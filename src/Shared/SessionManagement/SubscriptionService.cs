using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Common;

namespace Shared.SessionManagement;

/// <summary>
/// Service for managing session subscriptions with capacity control and waitlist management
/// </summary>
public class SubscriptionService : ISubscriptionService
{
    private readonly SessionDbContext _context;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(SessionDbContext context, ILogger<SubscriptionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ==================== Subscription Management ====================

    /// <summary>
    /// Subscribe a user to a session with capacity management
    /// </summary>
    public async Task<Result<SubscriptionResponse>> SubscribeAsync(SubscribeRequest request)
    {
        // Validate session exists and is subscribable
        Session? session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId);

        if (session == null)
        {
            return Result<SubscriptionResponse>.Failure("Session not found");
        }

        if (!session.IsPublished)
        {
            return Result<SubscriptionResponse>.Failure("Session is not published");
        }

        if (!session.RequiresSubscription)
        {
            return Result<SubscriptionResponse>.Failure("This session does not require subscription");
        }

        if (session.Status == SessionStatus.Cancelled)
        {
            return Result<SubscriptionResponse>.Failure("Cannot subscribe to a cancelled session");
        }

        if (session.Status == SessionStatus.Completed)
        {
            return Result<SubscriptionResponse>.Failure("Cannot subscribe to a completed session");
        }

        // Check if user already has a subscription
        Subscription? existingSubscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.SessionId == request.SessionId && s.UserId == request.UserId);

        if (existingSubscription != null)
        {
            if (existingSubscription.Status == SubscriptionStatus.Cancelled)
            {
                // Reactivate cancelled subscription
                return await ReactivateSubscriptionAsync(existingSubscription, session);
            }

            return Result<SubscriptionResponse>.Failure("User is already subscribed to this session");
        }

        // Check capacity and determine if should be waitlisted
        var confirmedCount = await _context.Subscriptions
            .CountAsync(s => s.SessionId == request.SessionId &&
                            s.Status == SubscriptionStatus.Confirmed);

        var isWaitlisted = session.MaxAttendees.HasValue && confirmedCount >= session.MaxAttendees.Value;

        // Get waitlist position if being waitlisted
        int? waitlistPosition = null;
        if (isWaitlisted)
        {
            var maxWaitlistPosition = await _context.Subscriptions
                .Where(s => s.SessionId == request.SessionId && s.IsWaitlisted)
                .MaxAsync(s => (int?)s.WaitlistPosition) ?? 0;
            waitlistPosition = maxWaitlistPosition + 1;
        }

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            SessionId = request.SessionId,
            UserId = request.UserId,
            Status = isWaitlisted ? SubscriptionStatus.Waitlisted : SubscriptionStatus.Confirmed,
            IsWaitlisted = isWaitlisted,
            WaitlistPosition = waitlistPosition,
            UserAgent = request.UserAgent,
            IpAddress = request.IpAddress,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);

        // Update session attendee count if confirmed
        if (!isWaitlisted)
        {
            session.CurrentAttendees++;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} subscribed to session {SessionId}. Waitlisted: {IsWaitlisted}",
            request.UserId, request.SessionId, isWaitlisted);

        return Result<SubscriptionResponse>.Success(MapToResponse(subscription, session.Title));
    }

    /// <summary>
    /// Unsubscribe a user from a session
    /// </summary>
    public async Task<Result> UnsubscribeAsync(Guid sessionId, Guid userId, string? reason = null)
    {
        Subscription? subscription = await _context.Subscriptions
            .Include(s => s.Session)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.UserId == userId);

        if (subscription == null)
        {
            return Result.Failure("Subscription not found");
        }

        if (subscription.Status == SubscriptionStatus.Cancelled)
        {
            return Result.Failure("Subscription is already cancelled");
        }

        var wasConfirmed = subscription.Status == SubscriptionStatus.Confirmed;

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledAt = DateTime.UtcNow;
        subscription.CancellationReason = reason;
        subscription.UpdatedAt = DateTime.UtcNow;

        // Update session attendee count if was confirmed
        if (wasConfirmed && subscription.Session != null)
        {
            subscription.Session.CurrentAttendees = Math.Max(0, subscription.Session.CurrentAttendees - 1);
        }

        await _context.SaveChangesAsync();

        // Process waitlist to fill the spot
        if (wasConfirmed)
        {
            await ProcessWaitlistAsync(sessionId, 1);
        }

        _logger.LogInformation(
            "User {UserId} unsubscribed from session {SessionId}. Reason: {Reason}",
            userId, sessionId, reason ?? "Not provided");

        return Result.Success();
    }

    /// <summary>
    /// Get subscription by ID
    /// </summary>
    public async Task<SubscriptionResponse?> GetSubscriptionAsync(Guid subscriptionId)
    {
        Subscription? subscription = await _context.Subscriptions
            .Include(s => s.Session)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId);

        return subscription == null ? null : MapToResponse(subscription, subscription.Session?.Title);
    }

    /// <summary>
    /// Get subscription for a specific user and session
    /// </summary>
    public async Task<SubscriptionResponse?> GetUserSessionSubscriptionAsync(Guid sessionId, Guid userId)
    {
        Subscription? subscription = await _context.Subscriptions
            .Include(s => s.Session)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.UserId == userId);

        return subscription == null ? null : MapToResponse(subscription, subscription.Session?.Title);
    }

    /// <summary>
    /// Get all subscriptions for a user
    /// </summary>
    public async Task<IEnumerable<SubscriptionResponse>> GetUserSubscriptionsAsync(Guid userId, Guid? eventId = null)
    {
        IQueryable<Subscription> query = _context.Subscriptions
            .Include(s => s.Session)
            .Where(s => s.UserId == userId);

        if (eventId.HasValue)
        {
            query = query.Where(s => s.Session != null && s.Session.EventId == eventId.Value);
        }

        List<Subscription> subscriptions = await query
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return subscriptions.Select(s => MapToResponse(s, s.Session?.Title));
    }

    /// <summary>
    /// Get all subscriptions for a session
    /// </summary>
    public async Task<IEnumerable<SubscriptionResponse>> GetSessionSubscriptionsAsync(Guid sessionId, bool includeWaitlisted = true, bool includeCancelled = false)
    {
        IQueryable<Subscription> query = _context.Subscriptions
            .Include(s => s.Session)
            .Where(s => s.SessionId == sessionId);

        if (!includeWaitlisted)
        {
            query = query.Where(s => !s.IsWaitlisted);
        }

        if (!includeCancelled)
        {
            query = query.Where(s => s.Status != SubscriptionStatus.Cancelled);
        }

        List<Subscription> subscriptions = await query
            .OrderBy(s => s.IsWaitlisted)
            .ThenBy(s => s.WaitlistPosition)
            .ThenBy(s => s.CreatedAt)
            .ToListAsync();

        return subscriptions.Select(s => MapToResponse(s, s.Session?.Title));
    }

    // ==================== Waitlist Management ====================

    /// <summary>
    /// Get waitlist for a session
    /// </summary>
    public async Task<IEnumerable<SubscriptionResponse>> GetWaitlistAsync(Guid sessionId)
    {
        List<Subscription> waitlist = await _context.Subscriptions
            .Include(s => s.Session)
            .Where(s => s.SessionId == sessionId &&
                       s.IsWaitlisted &&
                       s.Status == SubscriptionStatus.Waitlisted)
            .OrderBy(s => s.WaitlistPosition)
            .ToListAsync();

        return waitlist.Select(s => MapToResponse(s, s.Session?.Title));
    }

    /// <summary>
    /// Process waitlist to fill available spots
    /// </summary>
    public async Task<int> ProcessWaitlistAsync(Guid sessionId, int? count = null)
    {
        Session? session = await _context.Sessions.FindAsync(sessionId);
        if (session == null || !session.MaxAttendees.HasValue)
        {
            return 0;
        }

        var confirmedCount = await _context.Subscriptions
            .CountAsync(s => s.SessionId == sessionId && s.Status == SubscriptionStatus.Confirmed);

        var availableSpots = session.MaxAttendees.Value - confirmedCount;
        if (availableSpots <= 0)
        {
            return 0;
        }

        var spotsToFill = count.HasValue ? Math.Min(count.Value, availableSpots) : availableSpots;

        List<Subscription> waitlistedUsers = await _context.Subscriptions
            .Where(s => s.SessionId == sessionId &&
                       s.IsWaitlisted &&
                       s.Status == SubscriptionStatus.Waitlisted)
            .OrderBy(s => s.WaitlistPosition)
            .Take(spotsToFill)
            .ToListAsync();

        var promoted = 0;
        foreach (Subscription? subscription in waitlistedUsers)
        {
            subscription.Status = SubscriptionStatus.Confirmed;
            subscription.IsWaitlisted = false;
            subscription.WaitlistPosition = null;
            subscription.WaitlistConfirmedAt = DateTime.UtcNow;
            subscription.UpdatedAt = DateTime.UtcNow;
            session.CurrentAttendees++;
            promoted++;

            _logger.LogInformation(
                "User {UserId} promoted from waitlist for session {SessionId}",
                subscription.UserId, sessionId);
        }

        // Reorder remaining waitlist positions
        List<Subscription> remainingWaitlist = await _context.Subscriptions
            .Where(s => s.SessionId == sessionId &&
                       s.IsWaitlisted &&
                       s.Status == SubscriptionStatus.Waitlisted)
            .OrderBy(s => s.WaitlistPosition)
            .ToListAsync();

        for (int i = 0; i < remainingWaitlist.Count; i++)
        {
            remainingWaitlist[i].WaitlistPosition = i + 1;
        }

        await _context.SaveChangesAsync();

        return promoted;
    }

    /// <summary>
    /// Get waitlist position for a user
    /// </summary>
    public async Task<int?> GetWaitlistPositionAsync(Guid sessionId, Guid userId)
    {
        Subscription? subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId &&
                                     s.UserId == userId &&
                                     s.IsWaitlisted);

        return subscription?.WaitlistPosition;
    }

    // ==================== Attendance Management ====================

    /// <summary>
    /// Check in a user for a session
    /// </summary>
    public async Task<Result> CheckInAsync(Guid sessionId, Guid userId)
    {
        Subscription? subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.UserId == userId);

        if (subscription == null)
        {
            return Result.Failure("Subscription not found");
        }

        if (subscription.Status != SubscriptionStatus.Confirmed)
        {
            return Result.Failure("Only confirmed subscriptions can check in");
        }

        if (subscription.CheckedInAt.HasValue)
        {
            return Result.Failure("User has already checked in");
        }

        subscription.CheckedInAt = DateTime.UtcNow;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} checked in for session {SessionId}", userId, sessionId);

        return Result.Success();
    }

    /// <summary>
    /// Mark attendance for multiple users
    /// </summary>
    public async Task<Result> MarkAttendanceAsync(Guid sessionId, IEnumerable<AttendanceRecord> attendances)
    {
        Session? session = await _context.Sessions.FindAsync(sessionId);
        if (session == null)
        {
            return Result.Failure("Session not found");
        }

        var userIds = attendances.Select(a => a.UserId).ToList();
        Dictionary<Guid, Subscription> subscriptions = await _context.Subscriptions
            .Where(s => s.SessionId == sessionId && userIds.Contains(s.UserId))
            .ToDictionaryAsync(s => s.UserId);

        foreach (AttendanceRecord attendance in attendances)
        {
            if (subscriptions.TryGetValue(attendance.UserId, out Subscription? subscription))
            {
                subscription.Attended = attendance.Attended;
                subscription.Status = attendance.Attended ? SubscriptionStatus.Attended : SubscriptionStatus.NoShow;
                subscription.Notes = string.IsNullOrEmpty(attendance.Notes) ? subscription.Notes : attendance.Notes;
                subscription.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Marked attendance for {Count} users in session {SessionId}",
            attendances.Count(), sessionId);

        return Result.Success();
    }

    /// <summary>
    /// Get attendance statistics for a session
    /// </summary>
    public async Task<AttendanceStats> GetAttendanceStatsAsync(Guid sessionId)
    {
        List<Subscription> subscriptions = await _context.Subscriptions
            .Where(s => s.SessionId == sessionId)
            .ToListAsync();

        return new AttendanceStats
        {
            SessionId = sessionId,
            TotalSubscriptions = subscriptions.Count,
            ConfirmedCount = subscriptions.Count(s => s.Status == SubscriptionStatus.Confirmed),
            WaitlistedCount = subscriptions.Count(s => s.Status == SubscriptionStatus.Waitlisted),
            CancelledCount = subscriptions.Count(s => s.Status == SubscriptionStatus.Cancelled),
            CheckedInCount = subscriptions.Count(s => s.CheckedInAt.HasValue),
            AttendedCount = subscriptions.Count(s => s.Attended == true),
            NoShowCount = subscriptions.Count(s => s.Attended == false)
        };
    }

    // ==================== Capacity Management ====================

    /// <summary>
    /// Check session availability
    /// </summary>
    public async Task<SessionAvailability> CheckAvailabilityAsync(Guid sessionId)
    {
        Session? session = await _context.Sessions.FindAsync(sessionId);
        if (session == null)
        {
            return new SessionAvailability
            {
                SessionId = sessionId,
                IsAvailable = false,
                Message = "Session not found"
            };
        }

        var confirmedCount = await _context.Subscriptions
            .CountAsync(s => s.SessionId == sessionId && s.Status == SubscriptionStatus.Confirmed);

        var waitlistedCount = await _context.Subscriptions
            .CountAsync(s => s.SessionId == sessionId && s.Status == SubscriptionStatus.Waitlisted);

        var hasCapacity = !session.MaxAttendees.HasValue || confirmedCount < session.MaxAttendees.Value;
        var spotsRemaining = session.MaxAttendees.HasValue
            ? Math.Max(0, session.MaxAttendees.Value - confirmedCount)
            : (int?)null;

        return new SessionAvailability
        {
            SessionId = sessionId,
            IsAvailable = hasCapacity && session.IsPublished && session.RequiresSubscription,
            HasCapacity = hasCapacity,
            MaxAttendees = session.MaxAttendees,
            CurrentAttendees = confirmedCount,
            SpotsRemaining = spotsRemaining,
            WaitlistCount = waitlistedCount,
            Message = hasCapacity
                ? (spotsRemaining.HasValue ? $"{spotsRemaining} spots remaining" : "Unlimited capacity")
                : "Session is at capacity - join waitlist"
        };
    }

    /// <summary>
    /// Update session capacity
    /// </summary>
    public async Task<Result> UpdateCapacityAsync(Guid sessionId, int? maxAttendees)
    {
        Session? session = await _context.Sessions.FindAsync(sessionId);
        if (session == null)
        {
            return Result.Failure("Session not found");
        }

        var oldCapacity = session.MaxAttendees;
        session.MaxAttendees = maxAttendees;
        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // If capacity increased, process waitlist
        if (maxAttendees.HasValue && (!oldCapacity.HasValue || maxAttendees.Value > oldCapacity.Value))
        {
            await ProcessWaitlistAsync(sessionId);
        }

        _logger.LogInformation(
            "Session {SessionId} capacity updated from {OldCapacity} to {NewCapacity}",
            sessionId, oldCapacity, maxAttendees);

        return Result.Success();
    }

    // ==================== Bulk Operations ====================

    /// <summary>
    /// Get subscriptions for multiple sessions
    /// </summary>
    public async Task<Dictionary<Guid, IEnumerable<SubscriptionResponse>>> GetBulkSessionSubscriptionsAsync(IEnumerable<Guid> sessionIds)
    {
        List<Subscription> subscriptions = await _context.Subscriptions
            .Include(s => s.Session)
            .Where(s => sessionIds.Contains(s.SessionId) && s.Status != SubscriptionStatus.Cancelled)
            .ToListAsync();

        return subscriptions
            .GroupBy(s => s.SessionId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(s => MapToResponse(s, s.Session?.Title)));
    }

    /// <summary>
    /// Cancel all subscriptions for a session (when session is cancelled)
    /// </summary>
    public async Task<int> CancelAllSubscriptionsAsync(Guid sessionId, string reason)
    {
        List<Subscription> subscriptions = await _context.Subscriptions
            .Where(s => s.SessionId == sessionId &&
                       s.Status != SubscriptionStatus.Cancelled)
            .ToListAsync();

        foreach (Subscription? subscription in subscriptions)
        {
            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.CancelledAt = DateTime.UtcNow;
            subscription.CancellationReason = reason;
            subscription.UpdatedAt = DateTime.UtcNow;
        }

        Session? session = await _context.Sessions.FindAsync(sessionId);
        if (session != null)
        {
            session.CurrentAttendees = 0;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Cancelled {Count} subscriptions for session {SessionId}. Reason: {Reason}",
            subscriptions.Count, sessionId, reason);

        return subscriptions.Count;
    }

    // ==================== Private Helper Methods ====================

    private async Task<Result<SubscriptionResponse>> ReactivateSubscriptionAsync(Subscription subscription, Session session)
    {
        var confirmedCount = await _context.Subscriptions
            .CountAsync(s => s.SessionId == subscription.SessionId &&
                            s.Status == SubscriptionStatus.Confirmed);

        var isWaitlisted = session.MaxAttendees.HasValue && confirmedCount >= session.MaxAttendees.Value;

        if (isWaitlisted)
        {
            var maxWaitlistPosition = await _context.Subscriptions
                .Where(s => s.SessionId == subscription.SessionId && s.IsWaitlisted)
                .MaxAsync(s => (int?)s.WaitlistPosition) ?? 0;

            subscription.Status = SubscriptionStatus.Waitlisted;
            subscription.IsWaitlisted = true;
            subscription.WaitlistPosition = maxWaitlistPosition + 1;
        }
        else
        {
            subscription.Status = SubscriptionStatus.Confirmed;
            subscription.IsWaitlisted = false;
            subscription.WaitlistPosition = null;
            session.CurrentAttendees++;
        }

        subscription.CancelledAt = null;
        subscription.CancellationReason = null;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Reactivated subscription for user {UserId} to session {SessionId}. Waitlisted: {IsWaitlisted}",
            subscription.UserId, subscription.SessionId, isWaitlisted);

        return Result<SubscriptionResponse>.Success(MapToResponse(subscription, session.Title));
    }

    private static SubscriptionResponse MapToResponse(Subscription subscription, string? sessionTitle)
    {
        return new SubscriptionResponse
        {
            Id = subscription.Id,
            SessionId = subscription.SessionId,
            SessionTitle = sessionTitle ?? string.Empty,
            UserId = subscription.UserId,
            Status = subscription.Status,
            IsWaitlisted = subscription.IsWaitlisted,
            WaitlistPosition = subscription.WaitlistPosition,
            CreatedAt = subscription.CreatedAt,
            CancelledAt = subscription.CancelledAt,
            CancellationReason = subscription.CancellationReason,
            CheckedInAt = subscription.CheckedInAt,
            Attended = subscription.Attended,
            Notes = subscription.Notes
        };
    }
}

// ==================== DTOs ====================

/// <summary>
/// Interface for subscription service
/// </summary>
public interface ISubscriptionService
{
    Task<Result<SubscriptionResponse>> SubscribeAsync(SubscribeRequest request);
    Task<Result> UnsubscribeAsync(Guid sessionId, Guid userId, string? reason = null);
    Task<SubscriptionResponse?> GetSubscriptionAsync(Guid subscriptionId);
    Task<SubscriptionResponse?> GetUserSessionSubscriptionAsync(Guid sessionId, Guid userId);
    Task<IEnumerable<SubscriptionResponse>> GetUserSubscriptionsAsync(Guid userId, Guid? eventId = null);
    Task<IEnumerable<SubscriptionResponse>> GetSessionSubscriptionsAsync(Guid sessionId, bool includeWaitlisted = true, bool includeCancelled = false);
    Task<IEnumerable<SubscriptionResponse>> GetWaitlistAsync(Guid sessionId);
    Task<int> ProcessWaitlistAsync(Guid sessionId, int? count = null);
    Task<int?> GetWaitlistPositionAsync(Guid sessionId, Guid userId);
    Task<Result> CheckInAsync(Guid sessionId, Guid userId);
    Task<Result> MarkAttendanceAsync(Guid sessionId, IEnumerable<AttendanceRecord> attendances);
    Task<AttendanceStats> GetAttendanceStatsAsync(Guid sessionId);
    Task<SessionAvailability> CheckAvailabilityAsync(Guid sessionId);
    Task<Result> UpdateCapacityAsync(Guid sessionId, int? maxAttendees);
    Task<Dictionary<Guid, IEnumerable<SubscriptionResponse>>> GetBulkSessionSubscriptionsAsync(IEnumerable<Guid> sessionIds);
    Task<int> CancelAllSubscriptionsAsync(Guid sessionId, string reason);
}

/// <summary>
/// Request to subscribe to a session
/// </summary>
public class SubscribeRequest
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Attendance record for marking attendance
/// </summary>
public class AttendanceRecord
{
    public Guid UserId { get; set; }
    public bool Attended { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Attendance statistics for a session
/// </summary>
public class AttendanceStats
{
    public Guid SessionId { get; set; }
    public int TotalSubscriptions { get; set; }
    public int ConfirmedCount { get; set; }
    public int WaitlistedCount { get; set; }
    public int CancelledCount { get; set; }
    public int CheckedInCount { get; set; }
    public int AttendedCount { get; set; }
    public int NoShowCount { get; set; }
}

/// <summary>
/// Session availability information
/// </summary>
public class SessionAvailability
{
    public Guid SessionId { get; set; }
    public bool IsAvailable { get; set; }
    public bool HasCapacity { get; set; }
    public int? MaxAttendees { get; set; }
    public int CurrentAttendees { get; set; }
    public int? SpotsRemaining { get; set; }
    public int WaitlistCount { get; set; }
    public string? Message { get; set; }
}
