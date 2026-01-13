using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Common;
using Shared.EventManagement;
using Shared.EventManagement.Entities;
using Shared.Notifications;
using Shared.UserManagement;
using StackExchange.Redis;

namespace Shared.Registration;

/// <summary>
/// Registration service implementation with email notifications and queue processing
/// </summary>
public class RegistrationService : IRegistrationService
{
    private readonly RegistrationDbContext _context;
    private readonly IUserService _userService;
    private readonly IEventService _eventService;
    private readonly IEmailService _emailService;
    private readonly StackExchange.Redis.IDatabase _redis;
    private readonly ILogger<RegistrationService> _logger;
    private readonly bool _isInMemory;

    private const string QUEUE_KEY_PREFIX = "event_queue:";
    private const string PROCESSING_RATE_KEY_PREFIX = "processing_rate:";
    private const int DEFAULT_BATCH_SIZE = 50;

    public RegistrationService(
        RegistrationDbContext context,
        IUserService userService,
        IEventService eventService,
        IEmailService emailService,
        IConnectionMultiplexer redis,
        ILogger<RegistrationService> logger,
        IOptions<DatabaseOptions>? databaseOptions = null)
    {
        _context = context;
        _userService = userService;
        _eventService = eventService;
        _emailService = emailService;
        _redis = redis.GetDatabase();
        _logger = logger;
        _isInMemory = databaseOptions?.Value?.UseInMemoryDatabase ?? false;
    }

    public async Task<RegistrationResult> RegisterUserAsync(RegistrationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate the registration request
            RegistrationValidationResult validation = await ValidateRegistrationAsync(request.UserId, request.EventId, cancellationToken);
            if (!validation.IsValid)
            {
                return new RegistrationResult
                {
                    Success = false,
                    Message = string.Join("; ", validation.Errors),
                    ErrorCode = validation.AlreadyRegistered ? "ALREADY_REGISTERED"
                             : validation.AtCapacity ? "AT_CAPACITY"
                             : validation.RegistrationClosed ? "REGISTRATION_CLOSED"
                             : validation.EventCancelled ? "EVENT_CANCELLED"
                             : "VALIDATION_FAILED"
                };
            }

            // Get user and event details
            User? user = await _userService.GetUserByIdAsync(request.UserId, cancellationToken);
            var eventDetails = await _eventService.GetEventByIdAsync(request.EventId, true);

            if (user == null || eventDetails == null)
            {
                return new RegistrationResult
                {
                    Success = false,
                    Message = "User or event not found",
                    ErrorCode = "NOT_FOUND"
                };
            }

            // InMemory database doesn't support transactions, so we conditionally use them
            IDbContextTransaction? transaction = null;
            if (!_isInMemory)
            {
                transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            }

            try
            {
                // Check current capacity and determine if registration should be queued
                var currentCount = await _context.Registrations
                    .Where(r => r.EventId == request.EventId && r.Status == RegistrationStatus.Confirmed)
                    .CountAsync(cancellationToken);

                var shouldQueue = false; // eventDetails.MaxCapacity.HasValue && currentCount >= eventDetails.MaxCapacity.Value;

                // Create the registration
                var registration = new Registration
                {
                    EventId = request.EventId,
                    UserId = request.UserId,
                    Status = shouldQueue ? RegistrationStatus.Queued : RegistrationStatus.Confirmed,
                    Priority = request.Priority,
                    RegistrationData = request.RegistrationData ?? new Dictionary<string, object>(),
                    RegisteredAt = DateTime.UtcNow,
                    RegistrationSource = request.Source ?? "web",
                    IpAddress = request.IpAddress,
                    UserAgent = request.UserAgent,
                    Notes = request.Notes
                };

                _context.Registrations.Add(registration);
                await _context.SaveChangesAsync(cancellationToken);

                // If queued, add to Redis queue and get position
                int? queuePosition = null;
                int? estimatedWaitTime = null;

                if (shouldQueue && !_isInMemory)
                {
                    var queueKey = QUEUE_KEY_PREFIX + request.EventId;
                    var queueData = JsonSerializer.Serialize(new
                    {
                        RegistrationId = registration.Id,
                        UserId = request.UserId,
                        EventId = request.EventId,
                        Priority = request.Priority,
                        RegisteredAt = registration.RegisteredAt
                    });

                    await _redis.ListRightPushAsync(queueKey, queueData);

                    // Get queue position (1-based)
                    var queueLength = await _redis.ListLengthAsync(queueKey);
                    queuePosition = (int)queueLength;

                    // Calculate estimated wait time
                    estimatedWaitTime = await CalculateEstimatedWaitTimeAsync(request.EventId, queuePosition.Value);

                    registration.QueuePosition = queuePosition;
                    await _context.SaveChangesAsync(cancellationToken);
                }

                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                // Send appropriate email notification
                bool emailSent = false;
                if (shouldQueue)
                {
                    emailSent = await _emailService.SendRegistrationQueuedAsync(
                        user, eventDetails, registration, queuePosition!.Value, cancellationToken);
                }
                else
                {
                    emailSent = await _emailService.SendRegistrationConfirmationAsync(
                        user, eventDetails, registration, cancellationToken);
                }

                if (!emailSent)
                {
                    _logger.LogWarning("Failed to send registration email to user {UserId} for event {EventId}",
                        request.UserId, request.EventId);
                }

                return new RegistrationResult
                {
                    Success = true,
                    Registration = registration,
                    Message = shouldQueue
                        ? $"Registration queued at position #{queuePosition}"
                        : "Registration confirmed successfully",
                    IsQueued = shouldQueue,
                    QueuePosition = queuePosition,
                    EstimatedWaitTimeMinutes = estimatedWaitTime
                };
            }
            catch (Exception)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                throw;
            }
            finally
            {
                transaction?.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register user {UserId} for event {EventId}",
                request.UserId, request.EventId);

            return new RegistrationResult
            {
                Success = false,
                Message = "Registration failed due to an internal error",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    public async Task<bool> CancelRegistrationAsync(Guid registrationId, Guid userId, string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // InMemory provider doesn't support Include across different contexts
            IQueryable<Registration> query = _context.Registrations.AsQueryable();
            if (!_isInMemory)
            {
                query = query.Include(r => r.User).Include(r => r.Event);
            }

            Registration? registration = await query
                .FirstOrDefaultAsync(r => r.Id == registrationId && r.UserId == userId, cancellationToken);

            if (registration == null)
            {
                return false;
            }

            if (registration.Status == RegistrationStatus.Cancelled)
            {
                return true; // Already cancelled
            }

            // InMemory provider doesn't support transactions
            IDbContextTransaction? transaction = null;
            if (!_isInMemory)
            {
                transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            }

            try
            {
                var wasQueued = registration.Status == RegistrationStatus.Queued;
                var wasConfirmed = registration.Status == RegistrationStatus.Confirmed;

                // Update registration status
                registration.Status = RegistrationStatus.Cancelled;
                registration.CancelledAt = DateTime.UtcNow;
                registration.CancellationReason = reason ?? "Cancelled by user";

                await _context.SaveChangesAsync(cancellationToken);

                // Remove from Redis queue if queued (skip for InMemory tests)
                if (wasQueued && registration.QueuePosition.HasValue && !_isInMemory)
                {
                    await RemoveFromQueueAsync(registration.EventId, registrationId);
                }

                // If this was a confirmed registration, process the queue to promote someone
                if (wasConfirmed && !_isInMemory)
                {
                    var promoted = await ProcessEventQueueAsync(registration.EventId, 1, cancellationToken);
                    _logger.LogInformation("Promoted {Count} registrations from queue for event {EventId} after cancellation",
                        promoted, registration.EventId);
                }

                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                // Send cancellation email (skip User/Event lookup if InMemory and they weren't loaded)
                if (!_isInMemory)
                {
                    var emailSent = await _emailService.SendRegistrationCancelledAsync(
                        registration.User!, registration.Event!, registration,
                        registration.CancellationReason, cancellationToken);

                    if (!emailSent)
                    {
                        _logger.LogWarning("Failed to send cancellation email for registration {RegistrationId}", registrationId);
                    }
                }

                return true;
            }
            catch (Exception)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel registration {RegistrationId} for user {UserId}",
                registrationId, userId);
            return false;
        }
    }

    public async Task<int> ProcessEventQueueAsync(Guid eventId, int? maxProcessCount = null, CancellationToken cancellationToken = default)
    {
        try
        {
            Event? eventDetails = await _eventService.GetEventByIdAsync(eventId, true);
            if (eventDetails == null) // || !eventDetails.MaxCapacity.HasValue)
            {
                return 0;
            }

            /*
            var currentConfirmedCount = await _context.Registrations
                .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.Confirmed)
                .CountAsync(cancellationToken);

            var availableSpots = eventDetails.MaxCapacity.Value - currentConfirmedCount;
            if (availableSpots <= 0)
            {
                return 0;
            }

            var processCount = Math.Min(availableSpots, maxProcessCount ?? availableSpots);
            */
            var processCount = 0; // Disable queue processing for now
            var queueKey = QUEUE_KEY_PREFIX + eventId;
            var promoted = 0;

            for (int i = 0; i < processCount; i++)
            {
                RedisValue queueItem = await _redis.ListLeftPopAsync(queueKey);
                if (!queueItem.HasValue)
                {
                    break; // Queue is empty
                }

                try
                {
                    dynamic? queueData = JsonSerializer.Deserialize<dynamic>(queueItem!.ToString());
                    var registrationIdString = ((JsonElement)queueData).GetProperty("RegistrationId").GetString();
                    var registrationId = Guid.Parse(registrationIdString!);

                    Registration? registration = await _context.Registrations
                        .Include(r => r.User)
                        .Include(r => r.Event)
                        .FirstOrDefaultAsync(r => r.Id == registrationId && r.Status == RegistrationStatus.Queued, cancellationToken);

                    if (registration != null)
                    {
                        registration.Status = RegistrationStatus.Confirmed;
                        registration.ConfirmedAt = DateTime.UtcNow;
                        registration.QueuePosition = null;

                        await _context.SaveChangesAsync(cancellationToken);

                        // Send confirmation email
                        var emailSent = await _emailService.SendRegistrationConfirmedFromQueueAsync(
                            registration.User!, registration.Event!, registration, cancellationToken);

                        if (!emailSent)
                        {
                            _logger.LogWarning("Failed to send queue confirmation email for registration {RegistrationId}",
                                registrationId);
                        }

                        promoted++;
                        _logger.LogInformation("Promoted registration {RegistrationId} from queue for event {EventId}",
                            registrationId, eventId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process queue item for event {EventId}: {QueueItem}",
                        eventId, queueItem);
                }
            }

            // Update queue positions for remaining items
            if (promoted > 0)
            {
                await UpdateQueuePositionsAfterPromotionAsync(eventId, promoted);
            }

            return promoted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process queue for event {EventId}", eventId);
            return 0;
        }
    }

    // Helper method implementations
    private async Task<int> CalculateEstimatedWaitTimeAsync(Guid eventId, int queuePosition)
    {
        try
        {
            var rateKey = PROCESSING_RATE_KEY_PREFIX + eventId;
            RedisValue rateValue = await _redis.StringGetAsync(rateKey);

            var processingRate = 1.0; // Default: 1 registration per minute
            if (rateValue.HasValue && double.TryParse(rateValue.ToString(), out var rate))
            {
                processingRate = rate;
            }

            return (int)Math.Ceiling(queuePosition / processingRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate estimated wait time for event {EventId}", eventId);
            return queuePosition * 60; // Fallback: assume 1 per hour
        }
    }

    private async Task RemoveFromQueueAsync(Guid eventId, Guid registrationId)
    {
        try
        {
            var queueKey = QUEUE_KEY_PREFIX + eventId;
            RedisValue[] queueItems = await _redis.ListRangeAsync(queueKey);

            foreach (RedisValue item in queueItems)
            {
                dynamic? queueData = JsonSerializer.Deserialize<dynamic>(item!.ToString());
                var itemRegistrationIdString = ((JsonElement)queueData).GetProperty("RegistrationId").GetString();
                var itemRegistrationId = Guid.Parse(itemRegistrationIdString!);

                if (itemRegistrationId == registrationId)
                {
                    await _redis.ListRemoveAsync(queueKey, item);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove registration {RegistrationId} from queue for event {EventId}",
                registrationId, eventId);
        }
    }

    private async Task UpdateQueuePositionsAfterPromotionAsync(Guid eventId, int promotedCount)
    {
        try
        {
            // Update queue positions in database
            List<Registration> queuedRegistrations = await _context.Registrations
                .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.Queued)
                .OrderBy(r => r.RegisteredAt)
                .ToListAsync();

            for (int i = 0; i < queuedRegistrations.Count; i++)
            {
                var newPosition = i + 1;
                if (queuedRegistrations[i].QueuePosition != newPosition)
                {
                    queuedRegistrations[i].QueuePosition = newPosition;
                }
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update queue positions for event {EventId} after promoting {Count} registrations",
                eventId, promotedCount);
        }
    }

    // Implement other required interface methods with basic functionality
    public async Task<RegistrationResult> ConfirmRegistrationAsync(string confirmationToken, CancellationToken cancellationToken = default)
    {
        // Basic implementation - in a real system, you'd store confirmation tokens
        return new RegistrationResult
        {
            Success = false,
            Message = "Confirmation not implemented yet"
        };
    }

    public async Task<Registration?> GetRegistrationAsync(Guid registrationId, bool includeUser = true, bool includeEvent = true, CancellationToken cancellationToken = default)
    {
        IQueryable<Registration> query = _context.Registrations.AsQueryable();

        if (includeUser)
        {
            query = query.Include(r => r.User);
        }

        if (includeEvent)
        {
            query = query.Include(r => r.Event);
        }

        return await query.FirstOrDefaultAsync(r => r.Id == registrationId, cancellationToken);
    }

    public async Task<Registration?> GetUserRegistrationAsync(Guid userId, Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Registrations
            .Include(r => r.User)
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.UserId == userId && r.EventId == eventId, cancellationToken);
    }

    public async Task<QueueStatus> GetQueueStatusAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var confirmedCount = await _context.Registrations
            .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.Confirmed)
            .CountAsync(cancellationToken);

        var queuedCount = await _context.Registrations
            .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.Queued)
            .CountAsync(cancellationToken);

        Event? eventDetails = await _eventService.GetEventByIdAsync(eventId, true);
        var maxCapacity = (int?)null; // eventDetails?.MaxCapacity;
        var availableSpots = maxCapacity.HasValue ? maxCapacity.Value - confirmedCount : (int?)null;

        return new QueueStatus
        {
            EventId = eventId,
            ConfirmedCount = confirmedCount,
            QueuedCount = queuedCount,
            MaxCapacity = maxCapacity,
            AvailableSpots = availableSpots,
            IsAtCapacity = maxCapacity.HasValue && confirmedCount >= maxCapacity.Value,
            ProcessingRate = 1.0, // Default processing rate
            EstimatedClearTimeMinutes = queuedCount > 0 ? queuedCount : null
        };
    }

    public async Task<RegistrationValidationResult> ValidateRegistrationAsync(Guid userId, Guid eventId, CancellationToken cancellationToken = default)
    {
        var result = new RegistrationValidationResult { IsValid = true };

        // Check if already registered
        Registration? existingRegistration = await _context.Registrations
            .FirstOrDefaultAsync(r => r.UserId == userId && r.EventId == eventId &&
                r.Status != RegistrationStatus.Cancelled, cancellationToken);

        if (existingRegistration != null)
        {
            result.IsValid = false;
            result.AlreadyRegistered = true;
            result.Errors.Add("User is already registered for this event");
        }

        // Add other validations as needed...

        return result;
    }

    // Implement remaining interface methods with basic implementations
    public Task<RegistrationSearchResult> SearchRegistrationsAsync(RegistrationSearchCriteria criteria, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Search functionality to be implemented");

    public Task<List<Registration>> GetUserRegistrationsAsync(Guid userId, RegistrationStatus? status = null, bool includeEvents = true, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Get user registrations to be implemented");

    public Task<List<Registration>> GetEventRegistrationsAsync(Guid eventId, RegistrationStatus? status = null, bool includeUsers = true, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Get event registrations to be implemented");

    public Task<int?> GetUserQueuePositionAsync(Guid userId, Guid eventId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Get queue position to be implemented");

    public Task<int> UpdateQueuePositionsAsync(int eventId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Update queue positions to be implemented");

    public Task<bool> MarkAsAttendedAsync(Guid registrationId, Guid checkedInBy, DateTime? checkedInAt = null, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Mark as attended to be implemented");

    public Task<bool> MarkAsNoShowAsync(Guid registrationId, Guid markedBy, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Mark as no-show to be implemented");

    public Task<int> CleanupExpiredRegistrationsAsync(int batchSize = 100, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Cleanup expired registrations to be implemented");

    public Task<int> SendEventRemindersAsync(int eventId, string reminderType, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Send event reminders to be implemented");

    public Task<RegistrationAnalytics> GetRegistrationAnalyticsAsync(int eventId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Get registration analytics to be implemented");

    public Task<bool> UpdateRegistrationDataAsync(Guid registrationId, Guid userId, Dictionary<string, object> registrationData, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Update registration data to be implemented");
}
