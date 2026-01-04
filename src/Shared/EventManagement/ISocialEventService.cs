using System.ComponentModel.DataAnnotations;
using Shared.Common;

namespace Shared.EventManagement;

/// <summary>
/// Business service interface for social event management operations
/// Handles social events, RSVPs, capacity management, and waitlists
/// </summary>
public interface ISocialEventService
{
    // Social Event Discovery Operations

    /// <summary>
    /// Get all social events for a parent event
    /// </summary>
    Task<IEnumerable<SocialEventResponse>> GetSocialEventsForEventAsync(
        Guid eventId,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a single social event by ID
    /// </summary>
    Task<SocialEventResponse?> GetSocialEventByIdAsync(
        Guid socialEventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a single social event by slug within an event
    /// </summary>
    Task<SocialEventResponse?> GetSocialEventBySlugAsync(
        Guid eventId,
        string slug,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search social events with filtering options
    /// </summary>
    Task<PagedResult<SocialEventResponse>> SearchSocialEventsAsync(
        SocialEventSearchRequest request,
        CancellationToken cancellationToken = default);

    // Social Event Management Operations (Admin/Organizer)

    /// <summary>
    /// Create a new social event
    /// </summary>
    Task<SocialEventResponse> CreateSocialEventAsync(
        CreateSocialEventRequest request,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing social event
    /// </summary>
    Task<SocialEventResponse?> UpdateSocialEventAsync(
        Guid socialEventId,
        UpdateSocialEventRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a social event
    /// </summary>
    Task<bool> DeleteSocialEventAsync(
        Guid socialEventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish a social event (make it visible to attendees)
    /// </summary>
    Task<bool> PublishSocialEventAsync(
        Guid socialEventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a social event
    /// </summary>
    Task<bool> CancelSocialEventAsync(
        Guid socialEventId,
        string? cancellationReason = null,
        CancellationToken cancellationToken = default);

    // RSVP Operations

    /// <summary>
    /// Create an RSVP for a social event
    /// </summary>
    Task<RsvpResult> CreateRsvpAsync(
        Guid socialEventId,
        Guid userId,
        CreateRsvpRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing RSVP
    /// </summary>
    Task<RsvpResult> UpdateRsvpAsync(
        Guid rsvpId,
        UpdateRsvpRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel an RSVP
    /// </summary>
    Task<bool> CancelRsvpAsync(
        Guid rsvpId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's RSVP for a specific social event
    /// </summary>
    Task<SocialEventRsvpResponse?> GetUserRsvpAsync(
        Guid socialEventId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all RSVPs for a user across all social events in an event
    /// </summary>
    Task<IEnumerable<SocialEventRsvpResponse>> GetUserRsvpsForEventAsync(
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all RSVPs for a specific social event (Admin/Organizer)
    /// </summary>
    Task<PagedResult<SocialEventRsvpResponse>> GetRsvpsForSocialEventAsync(
        Guid socialEventId,
        RsvpSearchRequest request,
        CancellationToken cancellationToken = default);

    // Capacity and Waitlist Operations

    /// <summary>
    /// Check if RSVP is available for a social event
    /// </summary>
    Task<RsvpAvailability> CheckRsvpAvailabilityAsync(
        Guid socialEventId,
        int requestedSpots = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process waitlist when a spot becomes available
    /// </summary>
    Task<int> ProcessWaitlistAsync(
        Guid socialEventId,
        CancellationToken cancellationToken = default);

    // Admin Operations

    /// <summary>
    /// Confirm an RSVP (Admin/Organizer)
    /// </summary>
    Task<bool> ConfirmRsvpAsync(
        Guid rsvpId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark attendee as no-show (Admin/Organizer)
    /// </summary>
    Task<bool> MarkAsNoShowAsync(
        Guid rsvpId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check in an attendee (Admin/Organizer)
    /// </summary>
    Task<bool> CheckInAttendeeAsync(
        Guid rsvpId,
        CancellationToken cancellationToken = default);
}

#region DTOs

/// <summary>
/// Response DTO for social event data
/// </summary>
public record SocialEventResponse(
    Guid Id,
    Guid EventId,
    string Title,
    string Slug,
    string Description,
    SocialEventType Type,
    DateTime StartTime,
    DateTime EndTime,
    int DurationMinutes,
    string? Location,
    string? Room,
    int? MaxCapacity,
    int CurrentRsvpCount,
    int WaitlistCount,
    int AvailableSpots,
    bool RsvpRequired,
    DateTime? RsvpDeadline,
    bool GuestsAllowed,
    int MaxGuestsPerAttendee,
    string? DressCode,
    decimal CostPerPerson,
    string Currency,
    string? DietaryInfo,
    string? Notes,
    string? ImageUrl,
    SocialEventStatus Status,
    int DisplayOrder,
    bool IsPublished,
    IEnumerable<string>? Tags,
    DateTime CreatedAt
);

/// <summary>
/// Response DTO for RSVP data
/// </summary>
public record SocialEventRsvpResponse(
    Guid Id,
    Guid SocialEventId,
    string SocialEventTitle,
    Guid UserId,
    string? UserFullName,
    string? UserEmail,
    SocialEventRsvpStatus Status,
    int GuestCount,
    string? GuestNames,
    string? DietaryRequirements,
    string? Notes,
    bool IsWaitlisted,
    int? WaitlistPosition,
    DateTime RegisteredAt,
    DateTime? ConfirmedAt,
    DateTime? CheckedInAt,
    bool IsCheckedIn,
    decimal? AmountPaid,
    PaymentStatus PaymentStatus
);

/// <summary>
/// Request to create a social event
/// </summary>
public record CreateSocialEventRequest
{
    [Required]
    public Guid EventId { get; init; }

    [Required]
    [MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? Slug { get; init; }

    [Required]
    [MaxLength(2000)]
    public string Description { get; init; } = string.Empty;

    public SocialEventType Type { get; init; } = SocialEventType.Networking;

    [Required]
    public DateTime StartTime { get; init; }

    [Required]
    public DateTime EndTime { get; init; }

    [MaxLength(200)]
    public string? Location { get; init; }

    [MaxLength(100)]
    public string? Room { get; init; }

    public int? MaxCapacity { get; init; }

    public bool RsvpRequired { get; init; } = true;

    public DateTime? RsvpDeadline { get; init; }

    public bool GuestsAllowed { get; init; } = false;

    public int MaxGuestsPerAttendee { get; init; } = 0;

    [MaxLength(200)]
    public string? DressCode { get; init; }

    public decimal CostPerPerson { get; init; } = 0;

    [MaxLength(3)]
    public string Currency { get; init; } = "USD";

    [MaxLength(1000)]
    public string? DietaryInfo { get; init; }

    public string? Notes { get; init; }

    [MaxLength(500)]
    public string? ImageUrl { get; init; }

    public int DisplayOrder { get; init; } = 0;

    public IEnumerable<string>? Tags { get; init; }
}

/// <summary>
/// Request to update a social event
/// </summary>
public record UpdateSocialEventRequest
{
    [MaxLength(200)]
    public string? Title { get; init; }

    [MaxLength(100)]
    public string? Slug { get; init; }

    [MaxLength(2000)]
    public string? Description { get; init; }

    public SocialEventType? Type { get; init; }

    public DateTime? StartTime { get; init; }

    public DateTime? EndTime { get; init; }

    [MaxLength(200)]
    public string? Location { get; init; }

    [MaxLength(100)]
    public string? Room { get; init; }

    public int? MaxCapacity { get; init; }

    public bool? RsvpRequired { get; init; }

    public DateTime? RsvpDeadline { get; init; }

    public bool? GuestsAllowed { get; init; }

    public int? MaxGuestsPerAttendee { get; init; }

    [MaxLength(200)]
    public string? DressCode { get; init; }

    public decimal? CostPerPerson { get; init; }

    [MaxLength(3)]
    public string? Currency { get; init; }

    [MaxLength(1000)]
    public string? DietaryInfo { get; init; }

    public string? Notes { get; init; }

    [MaxLength(500)]
    public string? ImageUrl { get; init; }

    public SocialEventStatus? Status { get; init; }

    public int? DisplayOrder { get; init; }

    public bool? IsPublished { get; init; }

    public IEnumerable<string>? Tags { get; init; }
}

/// <summary>
/// Request to search social events
/// </summary>
public record SocialEventSearchRequest
{
    public Guid? EventId { get; init; }

    public SocialEventType? Type { get; init; }

    public SocialEventStatus? Status { get; init; }

    public DateTime? FromDate { get; init; }

    public DateTime? ToDate { get; init; }

    public bool? HasAvailableSpots { get; init; }

    public bool PublishedOnly { get; init; } = true;

    public string? SearchText { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string SortBy { get; init; } = "StartTime";

    public bool SortDescending { get; init; } = false;
}

/// <summary>
/// Request to create an RSVP
/// </summary>
public record CreateRsvpRequest
{
    public int GuestCount { get; init; } = 0;

    [MaxLength(500)]
    public string? GuestNames { get; init; }

    [MaxLength(500)]
    public string? DietaryRequirements { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }
}

/// <summary>
/// Request to update an RSVP
/// </summary>
public record UpdateRsvpRequest
{
    public SocialEventRsvpStatus? Status { get; init; }

    public int? GuestCount { get; init; }

    [MaxLength(500)]
    public string? GuestNames { get; init; }

    [MaxLength(500)]
    public string? DietaryRequirements { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }
}

/// <summary>
/// Request to search RSVPs
/// </summary>
public record RsvpSearchRequest
{
    public SocialEventRsvpStatus? Status { get; init; }

    public bool? IsWaitlisted { get; init; }

    public bool? IsCheckedIn { get; init; }

    public string? SearchText { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string SortBy { get; init; } = "RegisteredAt";

    public bool SortDescending { get; init; } = false;
}

/// <summary>
/// Result of an RSVP operation
/// </summary>
public record RsvpResult
{
    public bool Success { get; init; }
    public SocialEventRsvpResponse? Rsvp { get; init; }
    public bool IsWaitlisted { get; init; }
    public int? WaitlistPosition { get; init; }
    public string? ErrorMessage { get; init; }
    public RsvpErrorCode? ErrorCode { get; init; }

    public static RsvpResult Succeeded(SocialEventRsvpResponse rsvp, bool isWaitlisted = false, int? waitlistPosition = null)
        => new()
        {
            Success = true,
            Rsvp = rsvp,
            IsWaitlisted = isWaitlisted,
            WaitlistPosition = waitlistPosition
        };

    public static RsvpResult Failed(string errorMessage, RsvpErrorCode errorCode)
        => new()
        {
            Success = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
}

/// <summary>
/// RSVP error codes
/// </summary>
public enum RsvpErrorCode
{
    SocialEventNotFound,
    SocialEventCancelled,
    SocialEventNotPublished,
    RsvpDeadlinePassed,
    NoSpotsAvailable,
    AlreadyRsvped,
    RsvpNotFound,
    GuestLimitExceeded,
    InvalidStatus,
    ValidationError
}

/// <summary>
/// RSVP availability check result
/// </summary>
public record RsvpAvailability
{
    public bool IsAvailable { get; init; }
    public int AvailableSpots { get; init; }
    public int WaitlistSize { get; init; }
    public bool WaitlistEnabled { get; init; }
    public DateTime? RsvpDeadline { get; init; }
    public bool IsDeadlinePassed { get; init; }
    public string? Message { get; init; }
}

#endregion
