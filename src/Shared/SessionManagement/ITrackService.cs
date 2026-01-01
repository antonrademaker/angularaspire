using Shared.Common;

namespace Shared.SessionManagement;

/// <summary>
/// Service interface for track management operations
/// Provides methods for track CRUD, ordering, and status management
/// </summary>
public interface ITrackService
{
    // Track CRUD Operations

    /// <summary>
    /// Create a new track
    /// </summary>
    /// <param name="request">Track creation request</param>
    /// <returns>Created track details</returns>
    Task<Result<TrackResponse>> CreateTrackAsync(CreateTrackRequest request);

    /// <summary>
    /// Get track by ID
    /// </summary>
    /// <param name="trackId">Track ID</param>
    /// <returns>Track details or null if not found</returns>
    Task<TrackResponse?> GetTrackAsync(Guid trackId);

    /// <summary>
    /// Get track by slug within an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="slug">Track slug</param>
    /// <returns>Track details or null if not found</returns>
    Task<TrackResponse?> GetTrackBySlugAsync(Guid eventId, string slug);

    /// <summary>
    /// Update an existing track
    /// </summary>
    /// <param name="trackId">Track ID</param>
    /// <param name="request">Track update request</param>
    /// <returns>Updated track details</returns>
    Task<Result<TrackResponse>> UpdateTrackAsync(Guid trackId, UpdateTrackRequest request);

    /// <summary>
    /// Delete a track
    /// </summary>
    /// <param name="trackId">Track ID</param>
    /// <returns>Success result</returns>
    Task<Result> DeleteTrackAsync(Guid trackId);

    // Track Listing and Search

    /// <summary>
    /// Get tracks for an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="includeInactive">Whether to include inactive tracks</param>
    /// <returns>List of tracks ordered by DisplayOrder</returns>
    Task<IEnumerable<TrackResponse>> GetEventTracksAsync(Guid eventId, bool includeInactive = false);

    /// <summary>
    /// Search tracks with filters
    /// </summary>
    /// <param name="request">Search request with filters</param>
    /// <returns>Paged list of tracks</returns>
    Task<PagedResult<TrackResponse>> SearchTracksAsync(TrackSearchRequest request);

    // Track Organization

    /// <summary>
    /// Reorder tracks within an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="trackOrders">List of track IDs with their new display orders</param>
    /// <returns>Success result</returns>
    Task<Result> ReorderTracksAsync(Guid eventId, IEnumerable<TrackOrderItem> trackOrders);

    /// <summary>
    /// Update track status (active/inactive)
    /// </summary>
    /// <param name="trackId">Track ID</param>
    /// <param name="isActive">Whether the track should be active</param>
    /// <returns>Updated track details</returns>
    Task<Result<TrackResponse>> UpdateTrackStatusAsync(Guid trackId, bool isActive);

    // Track Statistics

    /// <summary>
    /// Get track statistics (session count, subscription count, etc.)
    /// </summary>
    /// <param name="trackId">Track ID</param>
    /// <returns>Track statistics</returns>
    Task<TrackStatistics> GetTrackStatisticsAsync(Guid trackId);

    /// <summary>
    /// Get event-wide track statistics
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <returns>Statistics for all tracks in the event</returns>
    Task<IEnumerable<TrackStatistics>> GetEventTrackStatisticsAsync(Guid eventId);
}

// DTOs and Request/Response Models

/// <summary>
/// Request to create a new track
/// </summary>
public class CreateTrackRequest
{
    public Guid EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public TrackAudienceLevel AudienceLevel { get; set; } = TrackAudienceLevel.Intermediate;
    public TrackCategory Category { get; set; } = TrackCategory.Technical;
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public int? MaxConcurrentSessions { get; set; }
    public List<string>? Tags { get; set; }
    public Dictionary<string, object>? CustomFields { get; set; }
}

/// <summary>
/// Request to update an existing track
/// </summary>
public class UpdateTrackRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public TrackAudienceLevel? AudienceLevel { get; set; }
    public TrackCategory? Category { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? IsActive { get; set; }
    public int? MaxConcurrentSessions { get; set; }
    public List<string>? Tags { get; set; }
    public Dictionary<string, object>? CustomFields { get; set; }
}

/// <summary>
/// Track search request with filters
/// </summary>
public class TrackSearchRequest : PagedRequest
{
    public Guid? EventId { get; set; }
    public string? SearchText { get; set; }
    public List<TrackAudienceLevel>? AudienceLevels { get; set; }
    public List<TrackCategory>? Categories { get; set; }
    public bool? IsActive { get; set; }
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Track response DTO
/// </summary>
public class TrackResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public TrackAudienceLevel AudienceLevel { get; set; }
    public TrackCategory Category { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public int? MaxConcurrentSessions { get; set; }
    public List<string>? Tags { get; set; }
    public Dictionary<string, object>? CustomFields { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    
    // Statistics (populated when requested)
    public int? SessionCount { get; set; }
    public int? ActiveSessionCount { get; set; }
    public int? TotalSubscriptions { get; set; }
}

/// <summary>
/// Track ordering item for reordering operations
/// </summary>
public class TrackOrderItem
{
    public Guid TrackId { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Track statistics
/// </summary>
public class TrackStatistics
{
    public Guid TrackId { get; set; }
    public string TrackName { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public int PublishedSessions { get; set; }
    public int DraftSessions { get; set; }
    public int CompletedSessions { get; set; }
    public int CancelledSessions { get; set; }
    public int TotalSubscriptions { get; set; }
    public int ConfirmedSubscriptions { get; set; }
    public int WaitlistedSubscriptions { get; set; }
    public double AverageSessionCapacityUtilization { get; set; }
    public DateTime? LastSessionDateTime { get; set; }
    public DateTime? NextSessionDateTime { get; set; }
}