using System.ServiceModel;
using Shared.Common;

namespace Shared.SessionManagement;

/// <summary>
/// Service interface for session management operations
/// Provides methods for session CRUD, subscription management, and capacity control
/// </summary>
[ServiceContract]
public interface ISessionService
{
    // Session CRUD Operations

    /// <summary>
    /// Create a new session
    /// </summary>
    /// <param name="request">Session creation request</param>
    /// <returns>Created session details</returns>
    [OperationContract]
    Task<Result<SessionResponse>> CreateSessionAsync(CreateSessionRequest request);

    /// <summary>
    /// Get session by ID
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Session details or null if not found</returns>
    [OperationContract]
    Task<SessionResponse?> GetSessionAsync(Guid sessionId);

    /// <summary>
    /// Get session by slug within an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="slug">Session slug</param>
    /// <returns>Session details or null if not found</returns>
    [OperationContract]
    Task<SessionResponse?> GetSessionBySlugAsync(Guid eventId, string slug);

    /// <summary>
    /// Update an existing session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="request">Session update request</param>
    /// <returns>Updated session details</returns>
    [OperationContract]
    Task<Result<SessionResponse>> UpdateSessionAsync(Guid sessionId, UpdateSessionRequest request);

    /// <summary>
    /// Delete a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Success result</returns>
    [OperationContract]
    Task<Result> DeleteSessionAsync(Guid sessionId);

    /// <summary>
    /// Publish a session (make it visible to attendees)
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Success result</returns>
    [OperationContract]
    Task<Result> PublishSessionAsync(Guid sessionId);

    /// <summary>
    /// Unpublish a session (hide from attendees)
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Success result</returns>
    [OperationContract]
    Task<Result> UnpublishSessionAsync(Guid sessionId);

    // Session Search and Filtering

    /// <summary>
    /// Search sessions with filters
    /// </summary>
    /// <param name="request">Search request with filters</param>
    /// <returns>Paged list of sessions</returns>
    [OperationContract]
    Task<PagedResult<SessionResponse>> SearchSessionsAsync(SessionSearchRequest request);

    /// <summary>
    /// Get sessions for an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="trackId">Optional track ID to filter by</param>
    /// <param name="includeUnpublished">Whether to include unpublished sessions</param>
    /// <returns>List of sessions</returns>
    [OperationContract]
    Task<IEnumerable<SessionResponse>> GetEventSessionsAsync(Guid eventId, Guid? trackId = null, bool includeUnpublished = false);

    /// <summary>
    /// Get sessions by track
    /// </summary>
    /// <param name="trackId">Track ID</param>
    /// <param name="includeUnpublished">Whether to include unpublished sessions</param>
    /// <returns>List of sessions in the track</returns>
    [OperationContract]
    Task<IEnumerable<SessionResponse>> GetTrackSessionsAsync(Guid trackId, bool includeUnpublished = false);

    // Session Capacity and Conflict Management

    /// <summary>
    /// Check if a session has available capacity
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Availability information</returns>
    [OperationContract]
    Task<SessionAvailabilityResponse> CheckSessionAvailabilityAsync(Guid sessionId);

    /// <summary>
    /// Detect scheduling conflicts for a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="startTime">Proposed start time</param>
    /// <param name="endTime">Proposed end time</param>
    /// <param name="room">Optional room to check conflicts</param>
    /// <returns>List of conflicting sessions</returns>
    [OperationContract]
    Task<IEnumerable<SessionConflict>> DetectSessionConflictsAsync(Guid sessionId, DateTime startTime, DateTime endTime, string? room = null);

    // Subscription Management

    /// <summary>
    /// Subscribe a user to a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="userAgent">User agent for analytics</param>
    /// <param name="ipAddress">IP address for security</param>
    /// <returns>Subscription result</returns>
    [OperationContract]
    Task<Result<SubscriptionResponse>> SubscribeToSessionAsync(Guid sessionId, Guid userId, string? userAgent = null, string? ipAddress = null);

    /// <summary>
    /// Unsubscribe a user from a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="reason">Optional cancellation reason</param>
    /// <returns>Success result</returns>
    [OperationContract]
    Task<Result> UnsubscribeFromSessionAsync(Guid sessionId, Guid userId, string? reason = null);

    /// <summary>
    /// Get user's session subscriptions
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="eventId">Optional event ID to filter</param>
    /// <returns>List of user's subscriptions</returns>
    [OperationContract]
    Task<IEnumerable<SubscriptionResponse>> GetUserSubscriptionsAsync(Guid userId, Guid? eventId = null);

    /// <summary>
    /// Get session subscribers
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="includeWaitlisted">Whether to include waitlisted users</param>
    /// <returns>List of session subscribers</returns>
    [OperationContract]
    Task<IEnumerable<SubscriptionResponse>> GetSessionSubscriptionsAsync(Guid sessionId, bool includeWaitlisted = false);

    // Waitlist Management

    /// <summary>
    /// Move users from waitlist to confirmed when capacity becomes available
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="count">Number of users to promote (default: all possible)</param>
    /// <returns>Number of users promoted</returns>
    [OperationContract]
    Task<int> ProcessWaitlistAsync(Guid sessionId, int? count = null);

    /// <summary>
    /// Get waitlist for a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Ordered list of waitlisted users</returns>
    [OperationContract]
    Task<IEnumerable<SubscriptionResponse>> GetSessionWaitlistAsync(Guid sessionId);

    // Attendance Management

    /// <summary>
    /// Check in a user for a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Success result</returns>
    [OperationContract]
    Task<Result> CheckInUserAsync(Guid sessionId, Guid userId);

    /// <summary>
    /// Mark session attendance (post-event)
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="attendances">List of user attendances</param>
    /// <returns>Success result</returns>
    [OperationContract]
    Task<Result> MarkAttendanceAsync(Guid sessionId, IEnumerable<SessionAttendance> attendances);

    // Session Status Management

    /// <summary>
    /// Start a session (change status to InProgress)
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Success result</returns>
    [OperationContract]
    Task<Result> StartSessionAsync(Guid sessionId);

    /// <summary>
    /// Complete a session (change status to Completed)
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Success result</returns>
    [OperationContract]
    Task<Result> CompleteSessionAsync(Guid sessionId);

    /// <summary>
    /// Cancel a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="reason">Cancellation reason</param>
    /// <returns>Success result</returns>
    [OperationContract]
    Task<Result> CancelSessionAsync(Guid sessionId, string reason);
}

// DTOs and Request/Response Models

/// <summary>
/// Request to create a new session
/// </summary>
public class CreateSessionRequest
{
    public Guid EventId { get; set; }
    public Guid? TrackId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Abstract { get; set; }
    public string Slug { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public SessionType Type { get; set; } = SessionType.Talk;
    public SessionDifficulty DifficultyLevel { get; set; } = SessionDifficulty.Intermediate;
    public int? MaxAttendees { get; set; }
    public bool RequiresSubscription { get; set; } = false;
    public string? Room { get; set; }
    public string? Building { get; set; }
    public bool IsVirtual { get; set; } = false;
    public string? VirtualUrl { get; set; }
    public string? MaterialsUrl { get; set; }
    public bool AllowQuestions { get; set; } = true;
    public bool IsRecorded { get; set; } = false;
    public string Language { get; set; } = "en";
    public string? Prerequisites { get; set; }
    public string? LearningOutcomes { get; set; }
    public string? TargetAudience { get; set; }
    public List<string>? Tags { get; set; }
    public Dictionary<string, object>? CustomFields { get; set; }
}

/// <summary>
/// Request to update an existing session
/// </summary>
public class UpdateSessionRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Abstract { get; set; }
    public string? Slug { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public SessionType? Type { get; set; }
    public SessionDifficulty? DifficultyLevel { get; set; }
    public int? MaxAttendees { get; set; }
    public bool? RequiresSubscription { get; set; }
    public string? Room { get; set; }
    public string? Building { get; set; }
    public bool? IsVirtual { get; set; }
    public string? VirtualUrl { get; set; }
    public string? RecordingUrl { get; set; }
    public string? MaterialsUrl { get; set; }
    public bool? AllowQuestions { get; set; }
    public bool? IsRecorded { get; set; }
    public string? Language { get; set; }
    public string? Prerequisites { get; set; }
    public string? LearningOutcomes { get; set; }
    public string? TargetAudience { get; set; }
    public List<string>? Tags { get; set; }
    public Dictionary<string, object>? CustomFields { get; set; }
}

/// <summary>
/// Session search request with filters
/// </summary>
public class SessionSearchRequest : PagedRequest
{
    public Guid? EventId { get; set; }
    public Guid? TrackId { get; set; }
    public string? SearchText { get; set; }
    public List<SessionType>? Types { get; set; }
    public List<SessionDifficulty>? DifficultyLevels { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string>? Tags { get; set; }
    public bool? HasCapacity { get; set; }
    public bool IncludeUnpublished { get; set; } = false;
    public string? Room { get; set; }
    public bool? IsVirtual { get; set; }
}

/// <summary>
/// Session response DTO
/// </summary>
public class SessionResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid? TrackId { get; set; }
    public string? TrackName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Abstract { get; set; }
    public string Slug { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public SessionType Type { get; set; }
    public SessionDifficulty DifficultyLevel { get; set; }
    public int? MaxAttendees { get; set; }
    public int CurrentAttendees { get; set; }
    public bool RequiresSubscription { get; set; }
    public string? Room { get; set; }
    public string? Building { get; set; }
    public bool IsVirtual { get; set; }
    public string? VirtualUrl { get; set; }
    public string? RecordingUrl { get; set; }
    public string? MaterialsUrl { get; set; }
    public SessionStatus Status { get; set; }
    public bool IsPublished { get; set; }
    public bool AllowQuestions { get; set; }
    public bool IsRecorded { get; set; }
    public string Language { get; set; } = string.Empty;
    public string? Prerequisites { get; set; }
    public string? LearningOutcomes { get; set; }
    public string? TargetAudience { get; set; }
    public List<string>? Tags { get; set; }
    public Dictionary<string, object>? CustomFields { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<SessionSpeakerResponse>? Speakers { get; set; }
}

/// <summary>
/// Session availability response
/// </summary>
public class SessionAvailabilityResponse
{
    public bool IsAvailable { get; set; }
    public int? RemainingCapacity { get; set; }
    public int WaitlistCount { get; set; }
    public string? UnavailabilityReason { get; set; }
}

/// <summary>
/// Session conflict information
/// </summary>
public class SessionConflict
{
    public Guid ConflictingSessionId { get; set; }
    public string ConflictingSessionTitle { get; set; } = string.Empty;
    public DateTime ConflictingStartTime { get; set; }
    public DateTime ConflictingEndTime { get; set; }
    public string ConflictType { get; set; } = string.Empty; // "Time", "Room", "Speaker"
    public string? Room { get; set; }
}

/// <summary>
/// Subscription response DTO
/// </summary>
public class SubscriptionResponse
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string SessionTitle { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public bool? Attended { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public bool IsWaitlisted { get; set; }
    public int? WaitlistPosition { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Session attendance record
/// </summary>
public class SessionAttendance
{
    public Guid UserId { get; set; }
    public bool Attended { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Session speaker response DTO (for display purposes)
/// </summary>
public class SessionSpeakerResponse
{
    public Guid SpeakerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public SpeakerRole Role { get; set; }
    public int DisplayOrder { get; set; }
}
