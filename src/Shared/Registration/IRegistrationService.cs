using System.ServiceModel;

namespace Shared.Registration;

/// <summary>
/// Registration request data transfer object
/// </summary>
public class RegistrationRequest
{
    /// <summary>
    /// Event ID to register for
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// User ID performing the registration
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Registration priority level
    /// </summary>
    public RegistrationPriority Priority { get; set; } = RegistrationPriority.Normal;

    /// <summary>
    /// Custom registration data (dietary restrictions, accessibility needs, etc.)
    /// </summary>
    public Dictionary<string, object>? RegistrationData { get; set; }

    /// <summary>
    /// Registration source (web, mobile, admin, etc.)
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// IP address where registration originated
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the registering client
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Optional notes about the registration
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Registration result with status and queue information
/// </summary>
public class RegistrationResult
{
    /// <summary>
    /// Whether the registration was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The created or existing registration
    /// </summary>
    public Registration? Registration { get; set; }

    /// <summary>
    /// Result message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Whether the registration was placed in queue
    /// </summary>
    public bool IsQueued { get; set; }

    /// <summary>
    /// Current position in queue (if queued)
    /// </summary>
    public int? QueuePosition { get; set; }

    /// <summary>
    /// Estimated wait time in minutes (if queued)
    /// </summary>
    public int? EstimatedWaitTimeMinutes { get; set; }

    /// <summary>
    /// Whether confirmation email will be sent
    /// </summary>
    public bool RequiresConfirmation { get; set; }

    /// <summary>
    /// Confirmation token (if requires confirmation)
    /// </summary>
    public string? ConfirmationToken { get; set; }

    /// <summary>
    /// Error code for specific failure types
    /// </summary>
    public string? ErrorCode { get; set; }
}

/// <summary>
/// Queue status information for an event
/// </summary>
public class QueueStatus
{
    /// <summary>
    /// Event ID
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Total number of confirmed registrations
    /// </summary>
    public int ConfirmedCount { get; set; }

    /// <summary>
    /// Total number in queue
    /// </summary>
    public int QueuedCount { get; set; }

    /// <summary>
    /// Event capacity (null if unlimited)
    /// </summary>
    public int? MaxCapacity { get; set; }

    /// <summary>
    /// Available spots (null if unlimited capacity)
    /// </summary>
    public int? AvailableSpots { get; set; }

    /// <summary>
    /// Whether the event is at capacity
    /// </summary>
    public bool IsAtCapacity { get; set; }

    /// <summary>
    /// Current queue processing rate (registrations processed per minute)
    /// </summary>
    public double ProcessingRate { get; set; }

    /// <summary>
    /// Estimated time to clear current queue (in minutes)
    /// </summary>
    public int? EstimatedClearTimeMinutes { get; set; }
}

/// <summary>
/// Registration search criteria
/// </summary>
public class RegistrationSearchCriteria
{
    /// <summary>
    /// Event ID filter
    /// </summary>
    public int? EventId { get; set; }

    /// <summary>
    /// User ID filter
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Status filter
    /// </summary>
    public RegistrationStatus? Status { get; set; }

    /// <summary>
    /// Priority filter
    /// </summary>
    public RegistrationPriority? Priority { get; set; }

    /// <summary>
    /// Registration date range start
    /// </summary>
    public DateTime? RegisteredAfter { get; set; }

    /// <summary>
    /// Registration date range end
    /// </summary>
    public DateTime? RegisteredBefore { get; set; }

    /// <summary>
    /// Search in custom registration data
    /// </summary>
    public string? DataSearch { get; set; }

    /// <summary>
    /// Page number for pagination
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Items per page
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Sort field
    /// </summary>
    public string SortBy { get; set; } = "RegisteredAt";

    /// <summary>
    /// Sort descending
    /// </summary>
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Paginated registration search results
/// </summary>
public class RegistrationSearchResult
{
    /// <summary>
    /// Registration results
    /// </summary>
    public List<Registration> Registrations { get; set; } = new();

    /// <summary>
    /// Total count of matching registrations
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }
}

/// <summary>
/// Service interface for managing event registrations with queue processing support
/// Implements business logic for registration lifecycle, capacity management, and queue processing
/// </summary>
[ServiceContract]
public interface IRegistrationService
{
    /// <summary>
    /// Register a user for an event with queue support
    /// </summary>
    /// <param name="request">Registration request details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration result with queue information</returns>
    [OperationContract]
    Task<RegistrationResult> RegisterUserAsync(RegistrationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirm a pending registration using confirmation token
    /// </summary>
    /// <param name="confirmationToken">Confirmation token from email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration result</returns>
    [OperationContract]
    Task<RegistrationResult> ConfirmRegistrationAsync(string confirmationToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel an existing registration
    /// </summary>
    /// <param name="registrationId">Registration ID to cancel</param>
    /// <param name="userId">User ID requesting cancellation (for authorization)</param>
    /// <param name="reason">Optional cancellation reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if cancelled successfully</returns>
    [OperationContract]
    Task<bool> CancelRegistrationAsync(Guid registrationId, Guid userId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get registration by ID with full details
    /// </summary>
    /// <param name="registrationId">Registration ID</param>
    /// <param name="includeUser">Include user details</param>
    /// <param name="includeEvent">Include event details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration details or null if not found</returns>
    [OperationContract]
    Task<Registration?> GetRegistrationAsync(Guid registrationId, bool includeUser = true, bool includeEvent = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's registration for a specific event
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration or null if not found</returns>
    [OperationContract]
    Task<Registration?> GetUserRegistrationAsync(Guid userId, Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search registrations based on criteria
    /// </summary>
    /// <param name="criteria">Search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated search results</returns>
    [OperationContract]
    Task<RegistrationSearchResult> SearchRegistrationsAsync(RegistrationSearchCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all registrations for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="includeEvents">Include event details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User's registrations</returns>
    [OperationContract]
    Task<List<Registration>> GetUserRegistrationsAsync(Guid userId, RegistrationStatus? status = null, bool includeEvents = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all registrations for an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="includeUsers">Include user details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Event registrations</returns>
    [OperationContract]
    Task<List<Registration>> GetEventRegistrationsAsync(Guid eventId, RegistrationStatus? status = null, bool includeUsers = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current queue status for an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Queue status information</returns>
    [OperationContract]
    Task<QueueStatus> GetQueueStatusAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's position in queue for an event
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Queue position or null if not queued</returns>
    Task<int?> GetUserQueuePositionAsync(Guid userId, Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Process the queue for an event (move queued registrations to confirmed if capacity allows)
    /// This method should be called when registrations are cancelled or capacity is increased
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="maxProcessCount">Maximum number of queue entries to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of registrations moved from queue to confirmed</returns>
    Task<int> ProcessEventQueueAsync(Guid eventId, int? maxProcessCount = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update queue positions for an event after changes
    /// This ensures queue positions are sequential and accurate
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of queue positions updated</returns>
    Task<int> UpdateQueuePositionsAsync(int eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark registration as attended (for check-in purposes)
    /// </summary>
    /// <param name="registrationId">Registration ID</param>
    /// <param name="checkedInBy">User ID who performed check-in</param>
    /// <param name="checkedInAt">Check-in timestamp (defaults to now)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if marked successfully</returns>
    Task<bool> MarkAsAttendedAsync(Guid registrationId, Guid checkedInBy, DateTime? checkedInAt = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark registration as no-show
    /// </summary>
    /// <param name="registrationId">Registration ID</param>
    /// <param name="markedBy">User ID who marked as no-show</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if marked successfully</returns>
    Task<bool> MarkAsNoShowAsync(Guid registrationId, Guid markedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up expired registrations
    /// Automatically cancel registrations that have passed their expiration time
    /// </summary>
    /// <param name="batchSize">Number of registrations to process in each batch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of registrations cleaned up</returns>
    Task<int> CleanupExpiredRegistrationsAsync(int batchSize = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send reminder emails for upcoming events
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="reminderType">Type of reminder (24h, 1h, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of reminder emails queued for sending</returns>
    Task<int> SendEventRemindersAsync(int eventId, string reminderType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get registration analytics for an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration analytics data</returns>
    Task<RegistrationAnalytics> GetRegistrationAnalyticsAsync(int eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update registration data
    /// </summary>
    /// <param name="registrationId">Registration ID</param>
    /// <param name="userId">User ID (for authorization)</param>
    /// <param name="registrationData">Updated registration data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateRegistrationDataAsync(Guid registrationId, Guid userId, Dictionary<string, object> registrationData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate if user can register for event
    /// Checks prerequisites, capacity, timing, etc.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with details</returns>
    Task<RegistrationValidationResult> ValidateRegistrationAsync(Guid userId, Guid eventId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Registration analytics data
/// </summary>
public class RegistrationAnalytics
{
    /// <summary>
    /// Event ID
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// Total registrations by status
    /// </summary>
    public Dictionary<RegistrationStatus, int> RegistrationsByStatus { get; set; } = new();

    /// <summary>
    /// Registrations by priority level
    /// </summary>
    public Dictionary<RegistrationPriority, int> RegistrationsByPriority { get; set; } = new();

    /// <summary>
    /// Registration timeline (registrations per day)
    /// </summary>
    public Dictionary<DateTime, int> RegistrationTimeline { get; set; } = new();

    /// <summary>
    /// Average time from registration to confirmation (in minutes)
    /// </summary>
    public double? AverageConfirmationTimeMinutes { get; set; }

    /// <summary>
    /// Cancellation rate (percentage)
    /// </summary>
    public double CancellationRate { get; set; }

    /// <summary>
    /// No-show rate (percentage)
    /// </summary>
    public double NoShowRate { get; set; }

    /// <summary>
    /// Top registration sources
    /// </summary>
    public Dictionary<string, int> RegistrationSources { get; set; } = new();

    /// <summary>
    /// Current capacity utilization (percentage)
    /// </summary>
    public double CapacityUtilization { get; set; }

    /// <summary>
    /// Queue processing efficiency metrics
    /// </summary>
    public QueueAnalytics QueueAnalytics { get; set; } = new();
}

/// <summary>
/// Queue processing analytics
/// </summary>
public class QueueAnalytics
{
    /// <summary>
    /// Average queue wait time (in minutes)
    /// </summary>
    public double AverageWaitTimeMinutes { get; set; }

    /// <summary>
    /// Maximum queue length reached
    /// </summary>
    public int MaxQueueLength { get; set; }

    /// <summary>
    /// Current queue processing rate (per minute)
    /// </summary>
    public double ProcessingRate { get; set; }

    /// <summary>
    /// Queue abandonment rate (percentage of users who cancelled while queued)
    /// </summary>
    public double AbandonmentRate { get; set; }

    /// <summary>
    /// Success rate from queue to confirmation (percentage)
    /// </summary>
    public double QueueSuccessRate { get; set; }
}

/// <summary>
/// Registration validation result
/// </summary>
public class RegistrationValidationResult
{
    /// <summary>
    /// Whether registration is allowed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation error messages
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Whether user is already registered
    /// </summary>
    public bool AlreadyRegistered { get; set; }

    /// <summary>
    /// Whether event is at capacity
    /// </summary>
    public bool AtCapacity { get; set; }

    /// <summary>
    /// Whether registration period has closed
    /// </summary>
    public bool RegistrationClosed { get; set; }

    /// <summary>
    /// Whether event has been cancelled
    /// </summary>
    public bool EventCancelled { get; set; }

    /// <summary>
    /// Whether user meets prerequisites
    /// </summary>
    public bool MeetsPrerequisites { get; set; }

    /// <summary>
    /// Suggested action if registration is not valid
    /// </summary>
    public string? SuggestedAction { get; set; }
}
