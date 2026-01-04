using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Common;
using Shared.EventManagement;
using System.Text.Json;

namespace Shared.SessionManagement;

/// <summary>
/// Session service implementation with conflict detection and capacity management
/// </summary>
public class SessionService : ISessionService
{
    private readonly SessionDbContext _context;
    private readonly ILogger<SessionService> _logger;

    public SessionService(SessionDbContext context, ILogger<SessionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Session CRUD Operations

    public async Task<Result<SessionResponse>> CreateSessionAsync(CreateSessionRequest request)
    {
        try
        {
            // Validate event exists
            var eventExists = await _context.Set<Event>()
                .AnyAsync(e => e.Id == request.EventId);
            if (!eventExists)
            {
                return Result<SessionResponse>.Failure("Event not found");
            }

            // Validate track exists (if specified)
            if (request.TrackId.HasValue)
            {
                var trackExists = await _context.Tracks
                    .AnyAsync(t => t.Id == request.TrackId.Value && t.EventId == request.EventId);
                if (!trackExists)
                {
                    return Result<SessionResponse>.Failure("Track not found or does not belong to the specified event");
                }
            }

            // Check for duplicate slug
            var slugExists = await _context.Sessions
                .AnyAsync(s => s.EventId == request.EventId && s.Slug == request.Slug);
            if (slugExists)
            {
                return Result<SessionResponse>.Failure("A session with this slug already exists in the event");
            }

            // Check for time conflicts
            var conflicts = await DetectSessionConflictsAsync(Guid.Empty, request.StartTime, request.EndTime, request.Room);
            if (conflicts.Any())
            {
                var conflictMessages = conflicts.Select(c => $"{c.ConflictType}: {c.ConflictingSessionTitle}");
                return Result<SessionResponse>.Failure($"Schedule conflicts detected: {string.Join(", ", conflictMessages)}");
            }

            var session = new Session
            {
                EventId = request.EventId,
                TrackId = request.TrackId,
                Title = request.Title,
                Description = request.Description,
                Abstract = request.Abstract,
                Slug = request.Slug,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Type = request.Type,
                DifficultyLevel = request.DifficultyLevel,
                MaxAttendees = request.MaxAttendees,
                RequiresSubscription = request.RequiresSubscription,
                Room = request.Room,
                Building = request.Building,
                IsVirtual = request.IsVirtual,
                VirtualUrl = request.VirtualUrl,
                MaterialsUrl = request.MaterialsUrl,
                AllowQuestions = request.AllowQuestions,
                IsRecorded = request.IsRecorded,
                Language = request.Language,
                Prerequisites = request.Prerequisites,
                LearningOutcomes = request.LearningOutcomes,
                TargetAudience = request.TargetAudience,
                Tags = request.Tags?.Any() == true ? JsonSerializer.Serialize(request.Tags) : null,
                CustomFields = request.CustomFields?.Any() == true ? JsonSerializer.Serialize(request.CustomFields) : null,
                CreatedByUserId = Guid.NewGuid(), // TODO: Get from current user context
                CreatedAt = DateTime.UtcNow
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Session created: {SessionId} - {Title}", session.Id, session.Title);

            return Result<SessionResponse>.Success(await MapToSessionResponseAsync(session));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session: {Title}", request.Title);
            return Result<SessionResponse>.Failure("Failed to create session");
        }
    }

    public async Task<SessionResponse?> GetSessionAsync(Guid sessionId)
    {
        var session = await _context.Sessions
            .Include(s => s.Track)
            .Include(s => s.SessionSpeakers)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        return session != null ? await MapToSessionResponseAsync(session) : null;
    }

    public async Task<SessionResponse?> GetSessionBySlugAsync(Guid eventId, string slug)
    {
        var session = await _context.Sessions
            .Include(s => s.Track)
            .Include(s => s.SessionSpeakers)
            .FirstOrDefaultAsync(s => s.EventId == eventId && s.Slug == slug);

        return session != null ? await MapToSessionResponseAsync(session) : null;
    }

    public async Task<Result<SessionResponse>> UpdateSessionAsync(Guid sessionId, UpdateSessionRequest request)
    {
        try
        {
            var session = await _context.Sessions
                .Include(s => s.Track)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                return Result<SessionResponse>.Failure("Session not found");
            }

            // Update properties if provided
            if (!string.IsNullOrEmpty(request.Title))
                session.Title = request.Title;
            if (!string.IsNullOrEmpty(request.Description))
                session.Description = request.Description;
            if (request.Abstract != null)
                session.Abstract = request.Abstract;
            if (!string.IsNullOrEmpty(request.Slug))
            {
                // Check for duplicate slug
                var slugExists = await _context.Sessions
                    .AnyAsync(s => s.EventId == session.EventId && s.Slug == request.Slug && s.Id != sessionId);
                if (slugExists)
                {
                    return Result<SessionResponse>.Failure("A session with this slug already exists in the event");
                }
                session.Slug = request.Slug;
            }
            if (request.StartTime.HasValue)
                session.StartTime = request.StartTime.Value;
            if (request.EndTime.HasValue)
                session.EndTime = request.EndTime.Value;
            if (request.Type.HasValue)
                session.Type = request.Type.Value;
            if (request.DifficultyLevel.HasValue)
                session.DifficultyLevel = request.DifficultyLevel.Value;
            if (request.MaxAttendees.HasValue)
                session.MaxAttendees = request.MaxAttendees.Value;
            if (request.RequiresSubscription.HasValue)
                session.RequiresSubscription = request.RequiresSubscription.Value;
            if (request.Room != null)
                session.Room = request.Room;
            if (request.Building != null)
                session.Building = request.Building;
            if (request.IsVirtual.HasValue)
                session.IsVirtual = request.IsVirtual.Value;
            if (request.VirtualUrl != null)
                session.VirtualUrl = request.VirtualUrl;
            if (request.RecordingUrl != null)
                session.RecordingUrl = request.RecordingUrl;
            if (request.MaterialsUrl != null)
                session.MaterialsUrl = request.MaterialsUrl;
            if (request.AllowQuestions.HasValue)
                session.AllowQuestions = request.AllowQuestions.Value;
            if (request.IsRecorded.HasValue)
                session.IsRecorded = request.IsRecorded.Value;
            if (!string.IsNullOrEmpty(request.Language))
                session.Language = request.Language;
            if (request.Prerequisites != null)
                session.Prerequisites = request.Prerequisites;
            if (request.LearningOutcomes != null)
                session.LearningOutcomes = request.LearningOutcomes;
            if (request.TargetAudience != null)
                session.TargetAudience = request.TargetAudience;
            if (request.Tags != null)
                session.Tags = request.Tags.Any() ? JsonSerializer.Serialize(request.Tags) : null;
            if (request.CustomFields != null)
                session.CustomFields = request.CustomFields.Any() ? JsonSerializer.Serialize(request.CustomFields) : null;

            // Check for conflicts if time or room changed
            if (request.StartTime.HasValue || request.EndTime.HasValue || request.Room != null)
            {
                var conflicts = await DetectSessionConflictsAsync(sessionId, session.StartTime, session.EndTime, session.Room);
                if (conflicts.Any())
                {
                    var conflictMessages = conflicts.Select(c => $"{c.ConflictType}: {c.ConflictingSessionTitle}");
                    return Result<SessionResponse>.Failure($"Schedule conflicts detected: {string.Join(", ", conflictMessages)}");
                }
            }

            session.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Session updated: {SessionId} - {Title}", session.Id, session.Title);

            return Result<SessionResponse>.Success(await MapToSessionResponseAsync(session));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session: {SessionId}", sessionId);
            return Result<SessionResponse>.Failure("Failed to update session");
        }
    }

    public async Task<Result> DeleteSessionAsync(Guid sessionId)
    {
        try
        {
            var session = await _context.Sessions
                .Include(s => s.Subscriptions)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                return Result.Failure("Session not found");
            }

            // Check if session has subscriptions
            if (session.Subscriptions.Any(s => s.Status == SubscriptionStatus.Confirmed))
            {
                return Result.Failure("Cannot delete session with active subscriptions");
            }

            _context.Sessions.Remove(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Session deleted: {SessionId} - {Title}", session.Id, session.Title);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session: {SessionId}", sessionId);
            return Result.Failure("Failed to delete session");
        }
    }

    public async Task<Result> PublishSessionAsync(Guid sessionId)
    {
        try
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null)
            {
                return Result.Failure("Session not found");
            }

            session.IsPublished = true;
            session.Status = SessionStatus.Published;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Session published: {SessionId} - {Title}", session.Id, session.Title);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing session: {SessionId}", sessionId);
            return Result.Failure("Failed to publish session");
        }
    }

    public async Task<Result> UnpublishSessionAsync(Guid sessionId)
    {
        try
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null)
            {
                return Result.Failure("Session not found");
            }

            session.IsPublished = false;
            session.Status = SessionStatus.Draft;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Session unpublished: {SessionId} - {Title}", session.Id, session.Title);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unpublishing session: {SessionId}", sessionId);
            return Result.Failure("Failed to unpublish session");
        }
    }

    // Session Search and Filtering

    public async Task<PagedResult<SessionResponse>> SearchSessionsAsync(SessionSearchRequest request)
    {
        var query = _context.Sessions
            .Include(s => s.Track)
            .Include(s => s.SessionSpeakers)
            .AsQueryable();

        // Apply filters
        if (request.EventId.HasValue)
            query = query.Where(s => s.EventId == request.EventId.Value);

        if (request.TrackId.HasValue)
            query = query.Where(s => s.TrackId == request.TrackId.Value);

        if (!string.IsNullOrEmpty(request.SearchText))
        {
            var searchLower = request.SearchText.ToLower();
            query = query.Where(s =>
                s.Title.ToLower().Contains(searchLower) ||
                s.Description.ToLower().Contains(searchLower) ||
                (s.Abstract != null && s.Abstract.ToLower().Contains(searchLower)));
        }

        if (request.Types?.Any() == true)
            query = query.Where(s => request.Types.Contains(s.Type));

        if (request.DifficultyLevels?.Any() == true)
            query = query.Where(s => request.DifficultyLevels.Contains(s.DifficultyLevel));

        if (request.StartDate.HasValue)
            query = query.Where(s => s.StartTime >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(s => s.EndTime <= request.EndDate.Value);

        if (!string.IsNullOrEmpty(request.Room))
            query = query.Where(s => s.Room == request.Room);

        if (request.IsVirtual.HasValue)
            query = query.Where(s => s.IsVirtual == request.IsVirtual.Value);

        if (request.HasCapacity.HasValue && request.HasCapacity.Value)
            query = query.Where(s => s.MaxAttendees == null || s.CurrentAttendees < s.MaxAttendees);

        if (!request.IncludeUnpublished)
            query = query.Where(s => s.IsPublished);

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply paging and sorting
        query = query.OrderBy(s => s.StartTime)
                     .Skip(request.Skip)
                     .Take(request.PageSize);

        var sessions = await query.ToListAsync();
        var sessionResponses = new List<SessionResponse>();

        foreach (var session in sessions)
        {
            sessionResponses.Add(await MapToSessionResponseAsync(session));
        }

        return new PagedResult<SessionResponse>(sessionResponses, totalCount, request.Page, request.PageSize);
    }

    public async Task<IEnumerable<SessionResponse>> GetEventSessionsAsync(Guid eventId, Guid? trackId = null, bool includeUnpublished = false)
    {
        var query = _context.Sessions
            .Include(s => s.Track)
            .Include(s => s.SessionSpeakers)
            .Where(s => s.EventId == eventId);

        if (trackId.HasValue)
            query = query.Where(s => s.TrackId == trackId.Value);

        if (!includeUnpublished)
            query = query.Where(s => s.IsPublished);

        query = query.OrderBy(s => s.StartTime);

        var sessions = await query.ToListAsync();
        var responses = new List<SessionResponse>();

        foreach (var session in sessions)
        {
            responses.Add(await MapToSessionResponseAsync(session));
        }

        return responses;
    }

    public async Task<IEnumerable<SessionResponse>> GetTrackSessionsAsync(Guid trackId, bool includeUnpublished = false)
    {
        var query = _context.Sessions
            .Include(s => s.Track)
            .Include(s => s.SessionSpeakers)
            .Where(s => s.TrackId == trackId);

        if (!includeUnpublished)
            query = query.Where(s => s.IsPublished);

        query = query.OrderBy(s => s.StartTime);

        var sessions = await query.ToListAsync();
        var responses = new List<SessionResponse>();

        foreach (var session in sessions)
        {
            responses.Add(await MapToSessionResponseAsync(session));
        }

        return responses;
    }

    // Session Capacity and Conflict Management

    public async Task<SessionAvailabilityResponse> CheckSessionAvailabilityAsync(Guid sessionId)
    {
        var session = await _context.Sessions
            .Include(s => s.Subscriptions)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            return new SessionAvailabilityResponse
            {
                IsAvailable = false,
                UnavailabilityReason = "Session not found"
            };
        }

        var confirmedCount = session.Subscriptions.Count(s => s.Status == SubscriptionStatus.Confirmed);
        var waitlistCount = session.Subscriptions.Count(s => s.Status == SubscriptionStatus.Waitlisted);

        var isAvailable = session.MaxAttendees == null || confirmedCount < session.MaxAttendees;
        var remainingCapacity = session.MaxAttendees.HasValue ? session.MaxAttendees.Value - confirmedCount : (int?)null;

        return new SessionAvailabilityResponse
        {
            IsAvailable = isAvailable,
            RemainingCapacity = remainingCapacity,
            WaitlistCount = waitlistCount,
            UnavailabilityReason = !isAvailable ? "Session is at capacity" : null
        };
    }

    public async Task<IEnumerable<SessionConflict>> DetectSessionConflictsAsync(Guid sessionId, DateTime startTime, DateTime endTime, string? room = null)
    {
        var conflicts = new List<SessionConflict>();

        // Time-based conflicts (overlapping sessions in same room or virtual)
        var timeConflictQuery = _context.Sessions
            .Where(s => s.Id != sessionId && s.IsPublished)
            .Where(s => s.StartTime < endTime && s.EndTime > startTime);

        // Room conflicts
        if (!string.IsNullOrEmpty(room))
        {
            var roomConflicts = await timeConflictQuery
                .Where(s => s.Room == room && !s.IsVirtual)
                .ToListAsync();

            conflicts.AddRange(roomConflicts.Select(s => new SessionConflict
            {
                ConflictingSessionId = s.Id,
                ConflictingSessionTitle = s.Title,
                ConflictingStartTime = s.StartTime,
                ConflictingEndTime = s.EndTime,
                ConflictType = "Room",
                Room = s.Room
            }));
        }

        return conflicts;
    }

    // Subscription Management

    public async Task<Result<SubscriptionResponse>> SubscribeToSessionAsync(Guid sessionId, Guid userId, string? userAgent = null, string? ipAddress = null)
    {
        try
        {
            var session = await _context.Sessions
                .Include(s => s.Subscriptions)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                return Result<SubscriptionResponse>.Failure("Session not found");
            }

            // Check if user is already subscribed
            var existingSubscription = session.Subscriptions
                .FirstOrDefault(s => s.UserId == userId && s.Status != SubscriptionStatus.Cancelled);

            if (existingSubscription != null)
            {
                return Result<SubscriptionResponse>.Failure("User is already subscribed to this session");
            }

            // Check availability
            var availability = await CheckSessionAvailabilityAsync(sessionId);

            var subscription = new Subscription
            {
                SessionId = sessionId,
                UserId = userId,
                Status = availability.IsAvailable ? SubscriptionStatus.Confirmed : SubscriptionStatus.Waitlisted,
                IsWaitlisted = !availability.IsAvailable,
                UserAgent = userAgent,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            if (subscription.IsWaitlisted)
            {
                // Calculate waitlist position
                var waitlistPosition = session.Subscriptions.Count(s => s.Status == SubscriptionStatus.Waitlisted) + 1;
                subscription.WaitlistPosition = waitlistPosition;
            }
            else
            {
                // Update session attendee count
                session.CurrentAttendees++;
            }

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User subscribed to session: UserId={UserId}, SessionId={SessionId}, Status={Status}",
                userId, sessionId, subscription.Status);

            return Result<SubscriptionResponse>.Success(await MapToSubscriptionResponseAsync(subscription));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing user to session: UserId={UserId}, SessionId={SessionId}", userId, sessionId);
            return Result<SubscriptionResponse>.Failure("Failed to subscribe to session");
        }
    }

    public async Task<Result> UnsubscribeFromSessionAsync(Guid sessionId, Guid userId, string? reason = null)
    {
        try
        {
            var subscription = await _context.Subscriptions
                .Include(s => s.Session)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.UserId == userId && s.Status != SubscriptionStatus.Cancelled);

            if (subscription == null)
            {
                return Result.Failure("Subscription not found");
            }

            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.CancelledAt = DateTime.UtcNow;
            subscription.CancellationReason = reason;
            subscription.UpdatedAt = DateTime.UtcNow;

            // Update session attendee count if was confirmed
            if (subscription.Status == SubscriptionStatus.Confirmed && subscription.Session != null)
            {
                subscription.Session.CurrentAttendees = Math.Max(0, subscription.Session.CurrentAttendees - 1);
            }

            await _context.SaveChangesAsync();

            // Process waitlist to fill the vacant spot
            await ProcessWaitlistAsync(sessionId, 1);

            _logger.LogInformation("User unsubscribed from session: UserId={UserId}, SessionId={SessionId}", userId, sessionId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing user from session: UserId={UserId}, SessionId={SessionId}", userId, sessionId);
            return Result.Failure("Failed to unsubscribe from session");
        }
    }

    public async Task<IEnumerable<SubscriptionResponse>> GetUserSubscriptionsAsync(Guid userId, Guid? eventId = null)
    {
        var query = _context.Subscriptions
            .Include(s => s.Session)
            .Where(s => s.UserId == userId && s.Status != SubscriptionStatus.Cancelled);

        if (eventId.HasValue)
            query = query.Where(s => s.Session!.EventId == eventId.Value);

        var subscriptions = await query.ToListAsync();
        var responses = new List<SubscriptionResponse>();

        foreach (var subscription in subscriptions)
        {
            responses.Add(await MapToSubscriptionResponseAsync(subscription));
        }

        return responses;
    }

    public async Task<IEnumerable<SubscriptionResponse>> GetSessionSubscriptionsAsync(Guid sessionId, bool includeWaitlisted = false)
    {
        var query = _context.Subscriptions
            .Include(s => s.Session)
            .Where(s => s.SessionId == sessionId);

        if (!includeWaitlisted)
            query = query.Where(s => s.Status == SubscriptionStatus.Confirmed);

        var subscriptions = await query.OrderBy(s => s.CreatedAt).ToListAsync();
        var responses = new List<SubscriptionResponse>();

        foreach (var subscription in subscriptions)
        {
            responses.Add(await MapToSubscriptionResponseAsync(subscription));
        }

        return responses;
    }

    // Waitlist Management

    public async Task<int> ProcessWaitlistAsync(Guid sessionId, int? count = null)
    {
        try
        {
            var session = await _context.Sessions
                .Include(s => s.Subscriptions)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null || session.MaxAttendees == null)
                return 0;

            var confirmedCount = session.Subscriptions.Count(s => s.Status == SubscriptionStatus.Confirmed);
            var availableSpots = session.MaxAttendees.Value - confirmedCount;

            if (availableSpots <= 0)
                return 0;

            var spotsToFill = count.HasValue ? Math.Min(count.Value, availableSpots) : availableSpots;

            var waitlistedSubscriptions = session.Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Waitlisted)
                .OrderBy(s => s.WaitlistPosition)
                .Take(spotsToFill)
                .ToList();

            var promotedCount = 0;
            foreach (var subscription in waitlistedSubscriptions)
            {
                subscription.Status = SubscriptionStatus.Confirmed;
                subscription.IsWaitlisted = false;
                subscription.WaitlistPosition = null;
                subscription.WaitlistConfirmedAt = DateTime.UtcNow;
                subscription.UpdatedAt = DateTime.UtcNow;
                promotedCount++;
            }

            session.CurrentAttendees += promotedCount;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Processed waitlist for session: {SessionId}, promoted {Count} users", sessionId, promotedCount);

            return promotedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing waitlist for session: {SessionId}", sessionId);
            return 0;
        }
    }

    public async Task<IEnumerable<SubscriptionResponse>> GetSessionWaitlistAsync(Guid sessionId)
    {
        var subscriptions = await _context.Subscriptions
            .Include(s => s.Session)
            .Where(s => s.SessionId == sessionId && s.Status == SubscriptionStatus.Waitlisted)
            .OrderBy(s => s.WaitlistPosition)
            .ToListAsync();

        var responses = new List<SubscriptionResponse>();

        foreach (var subscription in subscriptions)
        {
            responses.Add(await MapToSubscriptionResponseAsync(subscription));
        }

        return responses;
    }

    // Attendance Management

    public async Task<Result> CheckInUserAsync(Guid sessionId, Guid userId)
    {
        try
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.UserId == userId && s.Status == SubscriptionStatus.Confirmed);

            if (subscription == null)
            {
                return Result.Failure("Confirmed subscription not found");
            }

            subscription.CheckedInAt = DateTime.UtcNow;
            subscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User checked in: UserId={UserId}, SessionId={SessionId}", userId, sessionId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking in user: UserId={UserId}, SessionId={SessionId}", userId, sessionId);
            return Result.Failure("Failed to check in user");
        }
    }

    public async Task<Result> MarkAttendanceAsync(Guid sessionId, IEnumerable<SessionAttendance> attendances)
    {
        try
        {
            var attendanceList = attendances.ToList();
            var userIds = attendanceList.Select(a => a.UserId).ToList();

            var subscriptions = await _context.Subscriptions
                .Where(s => s.SessionId == sessionId && userIds.Contains(s.UserId))
                .ToListAsync();

            foreach (var attendance in attendanceList)
            {
                var subscription = subscriptions.FirstOrDefault(s => s.UserId == attendance.UserId);
                if (subscription != null)
                {
                    subscription.Attended = attendance.Attended;
                    subscription.Status = attendance.Attended ? SubscriptionStatus.Attended : SubscriptionStatus.NoShow;
                    subscription.Notes = attendance.Notes;
                    subscription.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Marked attendance for session: {SessionId}, {Count} attendances processed",
                sessionId, attendanceList.Count);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking attendance for session: {SessionId}", sessionId);
            return Result.Failure("Failed to mark attendance");
        }
    }

    // Session Status Management

    public async Task<Result> StartSessionAsync(Guid sessionId)
    {
        try
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null)
            {
                return Result.Failure("Session not found");
            }

            session.Status = SessionStatus.InProgress;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Session started: {SessionId} - {Title}", session.Id, session.Title);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting session: {SessionId}", sessionId);
            return Result.Failure("Failed to start session");
        }
    }

    public async Task<Result> CompleteSessionAsync(Guid sessionId)
    {
        try
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null)
            {
                return Result.Failure("Session not found");
            }

            session.Status = SessionStatus.Completed;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Session completed: {SessionId} - {Title}", session.Id, session.Title);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing session: {SessionId}", sessionId);
            return Result.Failure("Failed to complete session");
        }
    }

    public async Task<Result> CancelSessionAsync(Guid sessionId, string reason)
    {
        try
        {
            var session = await _context.Sessions
                .Include(s => s.Subscriptions)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                return Result.Failure("Session not found");
            }

            session.Status = SessionStatus.Cancelled;
            session.UpdatedAt = DateTime.UtcNow;

            // Cancel all subscriptions
            foreach (var subscription in session.Subscriptions.Where(s => s.Status != SubscriptionStatus.Cancelled))
            {
                subscription.Status = SubscriptionStatus.Cancelled;
                subscription.CancelledAt = DateTime.UtcNow;
                subscription.CancellationReason = $"Session cancelled: {reason}";
                subscription.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Session cancelled: {SessionId} - {Title}, Reason: {Reason}",
                session.Id, session.Title, reason);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling session: {SessionId}", sessionId);
            return Result.Failure("Failed to cancel session");
        }
    }

    // Helper Methods

    private async Task<SessionResponse> MapToSessionResponseAsync(Session session)
    {
        // Deserialize JSON strings for response
        var tags = !string.IsNullOrEmpty(session.Tags) ? JsonSerializer.Deserialize<List<string>>(session.Tags) : null;
        var customFields = !string.IsNullOrEmpty(session.CustomFields) ? JsonSerializer.Deserialize<Dictionary<string, object>>(session.CustomFields) : null;

        return new SessionResponse
        {
            Id = session.Id,
            EventId = session.EventId,
            TrackId = session.TrackId,
            TrackName = session.Track?.Name,
            Title = session.Title,
            Description = session.Description,
            Abstract = session.Abstract,
            Slug = session.Slug,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            DurationMinutes = session.DurationMinutes,
            Type = session.Type,
            DifficultyLevel = session.DifficultyLevel,
            MaxAttendees = session.MaxAttendees,
            CurrentAttendees = session.CurrentAttendees,
            RequiresSubscription = session.RequiresSubscription,
            Room = session.Room,
            Building = session.Building,
            IsVirtual = session.IsVirtual,
            VirtualUrl = session.VirtualUrl,
            RecordingUrl = session.RecordingUrl,
            MaterialsUrl = session.MaterialsUrl,
            Status = session.Status,
            IsPublished = session.IsPublished,
            AllowQuestions = session.AllowQuestions,
            IsRecorded = session.IsRecorded,
            Language = session.Language,
            Prerequisites = session.Prerequisites,
            LearningOutcomes = session.LearningOutcomes,
            TargetAudience = session.TargetAudience,
            Tags = tags,
            CustomFields = customFields,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            Speakers = session.SessionSpeakers?.Select(ss => new SessionSpeakerResponse
            {
                SpeakerId = ss.SpeakerId,
                Role = ss.Role,
                DisplayOrder = ss.DisplayOrder,
                // Name, Title, Bio will be populated when SpeakerProfile is implemented
                Name = "Speaker Name", // Placeholder
                Title = "Speaker Title", // Placeholder
                Bio = "Speaker Bio" // Placeholder
            }).ToList()
        };
    }

    private async Task<SubscriptionResponse> MapToSubscriptionResponseAsync(Subscription subscription)
    {
        return new SubscriptionResponse
        {
            Id = subscription.Id,
            SessionId = subscription.SessionId,
            SessionTitle = subscription.Session?.Title ?? "Unknown Session",
            UserId = subscription.UserId,
            Status = subscription.Status,
            CreatedAt = subscription.CreatedAt,
            CancelledAt = subscription.CancelledAt,
            CancellationReason = subscription.CancellationReason,
            Attended = subscription.Attended,
            CheckedInAt = subscription.CheckedInAt,
            IsWaitlisted = subscription.IsWaitlisted,
            WaitlistPosition = subscription.WaitlistPosition,
            Notes = subscription.Notes,
            // UserName and UserEmail will be populated when User entity relationships are established
            UserName = "User Name", // Placeholder
            UserEmail = "user@example.com" // Placeholder
        };
    }
}