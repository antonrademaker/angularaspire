using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Shared.EventManagement.Entities;

namespace Shared.EventManagement;

/// <summary>
/// Business service interface for event management operations
/// Follows IDesign principles with clear business operations
/// </summary>
public interface IEventService
{
    // Event Discovery Operations

    /// <summary>
    /// Search for events by various criteria with pagination
    /// </summary>
    /// <param name="searchRequest">Search and filter criteria</param>
    /// <returns>Paginated list of events matching criteria</returns>
    Task<EventSearchResult> SearchEventsAsync(EventSearchRequest searchRequest);

    /// <summary>
    /// Get a single event by ID with full details
    /// </summary>
    /// <param name="eventId">Event unique identifier</param>
    /// <param name="includeDetails">Whether to include detailed information</param>
    /// <returns>Event details or null if not found</returns>
    Task<Event?> GetEventByIdAsync(Guid eventId, bool includeDetails = true);

    /// <summary>
    /// Get a single event by slug for SEO-friendly URLs
    /// </summary>
    /// <param name="slug">Event slug</param>
    /// <param name="includeDetails">Whether to include detailed information</param>
    /// <returns>Event details or null if not found</returns>
    Task<Event?> GetEventBySlugAsync(string slug, bool includeDetails = true);

    /// <summary>
    /// Get upcoming events for homepage/discovery
    /// </summary>
    /// <param name="limit">Maximum number of events to return</param>
    /// <returns>List of upcoming events</returns>
    Task<IEnumerable<Event>> GetUpcomingEventsAsync(int limit = 10);

    /*
    /// <summary>
    /// Get events by tags for category browsing
    /// </summary>
    /// <param name="tags">Array of tags to match</param>
    /// <param name="limit">Maximum number of events to return</param>
    /// <returns>Events matching any of the provided tags</returns>
    Task<IEnumerable<Event>> GetEventsByTagsAsync(string[] tags, int limit = 20);
    */

    // Event Management Operations (Admin/Organizer)

    /// <summary>
    /// Create a new event (Admin/Organizer only)
    /// </summary>
    /// <param name="eventData">Event creation data</param>
    /// <param name="createdByUserId">ID of the user creating the event</param>
    /// <returns>Created event with generated ID</returns>
    Task<Event> CreateEventAsync(CreateEventRequest eventData, Guid createdByUserId);

    /// <summary>
    /// Update an existing event (Admin/Organizer only)
    /// </summary>
    /// <param name="eventId">Event ID to update</param>
    /// <param name="eventData">Updated event data</param>
    /// <param name="updatedByUserId">ID of the user updating the event</param>
    /// <returns>Updated event or null if not found</returns>
    Task<Event?> UpdateEventAsync(Guid eventId, UpdateEventRequest eventData, Guid updatedByUserId);

    /// <summary>
    /// Delete an event (Admin only)
    /// </summary>
    /// <param name="eventId">Event ID to delete</param>
    /// <param name="deletedByUserId">ID of the user deleting the event</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    Task<bool> DeleteEventAsync(Guid eventId, Guid deletedByUserId);

    /// <summary>
    /// Change event status (e.g., publish, cancel, postpone)
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="newStatus">New status</param>
    /// <param name="updatedByUserId">ID of the user changing the status</param>
    /// <returns>Updated event or null if not found</returns>
    Task<Event?> ChangeEventStatusAsync(Guid eventId, EventStatus newStatus, Guid updatedByUserId);

    // Registration Management

    /*
    /// <summary>
    /// Check if an event has available registration slots
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <returns>True if registration is available, false otherwise</returns>
    Task<bool> IsRegistrationAvailableAsync(Guid eventId);

    /// <summary>
    /// Update attendee count after registration/cancellation
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="increment">Number to add (positive) or subtract (negative)</param>
    /// <returns>Updated attendee count</returns>
    Task<int> UpdateAttendeeCountAsync(Guid eventId, int increment);
    */

    // Reporting and Analytics

    /// <summary>
    /// Get events created by a specific organizer
    /// </summary>
    /// <param name="organizerUserId">Organizer user ID</param>
    /// <param name="includeStats">Whether to include registration statistics</param>
    /// <returns>List of events created by the organizer</returns>
    Task<IEnumerable<Event>> GetEventsByOrganizerAsync(Guid organizerUserId, bool includeStats = false);

    /*
    /// <summary>
    /// Get event registration statistics
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <returns>Registration statistics or null if event not found</returns>
    Task<EventRegistrationStats?> GetEventStatsAsync(Guid eventId);
    */
}

/// <summary>
/// Request model for event search operations
/// </summary>
public class EventSearchRequest
{
    /// <summary>
    /// Search text to match in title or description
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Filter by event status
    /// </summary>
    public EventStatus? Status { get; set; }

    /*
    /// <summary>
    /// Filter by event visibility
    /// </summary>
    public EventVisibility? Visibility { get; set; } = EventVisibility.Public;
    */

    /// <summary>
    /// Filter events starting after this date
    /// </summary>
    public DateTime? StartDateFrom { get; set; }

    /// <summary>
    /// Filter events starting before this date
    /// </summary>
    public DateTime? StartDateTo { get; set; }

    /*
    /// <summary>
    /// Filter by tags
    /// </summary>
    public string[]? Tags { get; set; }
    */

    /// <summary>
    /// Filter by organizer user ID
    /// </summary>
    public Guid? OrganizerId { get; set; }

    /*
    /// <summary>
    /// Whether to include virtual events
    /// </summary>
    public bool? IncludeVirtual { get; set; }

    /// <summary>
    /// Whether to include physical events
    /// </summary>
    public bool? IncludePhysical { get; set; }
    */

    /// <summary>
    /// Page number for pagination (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Sort field
    /// </summary>
    public string SortBy { get; set; } = "StartDate";

    /// <summary>
    /// Sort direction (true for ascending, false for descending)
    /// </summary>
    public bool SortAscending { get; set; } = true;
}

/// <summary>
/// Result model for event search operations
/// </summary>
public class EventSearchResult
{
    /// <summary>
    /// List of events matching the search criteria
    /// </summary>
    public IEnumerable<Event> Events { get; set; } = [];

    /// <summary>
    /// Total number of events matching the criteria (for pagination)
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages available
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there are more pages available
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// Whether there are previous pages available
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;
}

/// <summary>
/// Request model for creating new events
/// </summary>
public class CreateEventRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? DetailedDescription { get; set; }

    [Required]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public EventStatus Status { get; set; } = EventStatus.Draft;

    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }

    public Guid? SeriesId { get; set; }
}

/// <summary>
/// Request model for updating existing events
/// </summary>
public class UpdateEventRequest : CreateEventRequest
{
    // Inherits all fields from CreateEventRequest
    // Additional update-specific logic can be added here
}

/*
/// <summary>
/// Event registration statistics
/// </summary>
public class EventRegistrationStats
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public int MaxAttendees { get; set; }
    public int CurrentAttendees { get; set; }
    public int AvailableSlots => MaxAttendees - CurrentAttendees;
    public double RegistrationPercentage => MaxAttendees > 0 ? (double)CurrentAttendees / MaxAttendees * 100 : 0;
    public bool IsFullyBooked => CurrentAttendees >= MaxAttendees;
    public DateTime? LastRegistrationDate { get; set; }
}
*/
