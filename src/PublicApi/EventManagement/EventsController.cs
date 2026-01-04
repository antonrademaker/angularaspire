using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.EventManagement;
using System.Security.Claims;

namespace PublicApi.EventManagement;

/// <summary>
/// Public API controller for event discovery and registration operations
/// Provides endpoints for attendees to discover and interact with events
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IEventService eventService, ILogger<EventsController> logger)
    {
        _eventService = eventService;
        _logger = logger;
    }

    /// <summary>
    /// Search for public events with filtering and pagination
    /// </summary>
    /// <param name="searchText">Text to search in event title and description</param>
    /// <param name="startDateFrom">Filter events starting after this date</param>
    /// <param name="startDateTo">Filter events starting before this date</param>
    /// <param name="tags">Comma-separated list of tags to filter by</param>
    /// <param name="includeVirtual">Include virtual events</param>
    /// <param name="includePhysical">Include physical events</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page (max 50)</param>
    /// <param name="sortBy">Field to sort by (Title, StartDate, CreatedAt)</param>
    /// <param name="sortAscending">Sort direction</param>
    /// <returns>Paginated list of public events</returns>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<EventSearchResult>> SearchEvents(
        [FromQuery] string? searchText = null,
        [FromQuery] DateTime? startDateFrom = null,
        [FromQuery] DateTime? startDateTo = null,
        [FromQuery] string? tags = null,
        [FromQuery] bool? includeVirtual = null,
        [FromQuery] bool? includePhysical = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "StartDate",
        [FromQuery] bool sortAscending = true)
    {
        try
        {
            // Validate pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Min(50, Math.Max(1, pageSize)); // Limit to 50 items per page

            var searchRequest = new EventSearchRequest
            {
                SearchText = searchText,
                Status = EventStatus.Published, // Only show published events in public API
                Visibility = EventVisibility.Public, // Only show public events
                StartDateFrom = startDateFrom,
                StartDateTo = startDateTo,
                Tags = !string.IsNullOrWhiteSpace(tags) ? tags.Split(',', StringSplitOptions.RemoveEmptyEntries) : null,
                IncludeVirtual = includeVirtual,
                IncludePhysical = includePhysical,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortAscending = sortAscending
            };

            var result = await _eventService.SearchEventsAsync(searchRequest);

            _logger.LogInformation("Event search returned {Count} events (page {Page})",
                result.Events.Count(), result.CurrentPage);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching events");
            return StatusCode(500, new { message = "An error occurred while searching events" });
        }
    }

    /// <summary>
    /// Get upcoming public events for homepage display
    /// </summary>
    /// <param name="limit">Maximum number of events to return (max 20)</param>
    /// <returns>List of upcoming public events</returns>
    [HttpGet("upcoming")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Event>>> GetUpcomingEvents([FromQuery] int limit = 10)
    {
        try
        {
            limit = Math.Min(20, Math.Max(1, limit)); // Limit to 20 events max

            var events = await _eventService.GetUpcomingEventsAsync(limit, EventVisibility.Public);

            _logger.LogInformation("Retrieved {Count} upcoming events", events.Count());

            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving upcoming events");
            return StatusCode(500, new { message = "An error occurred while retrieving upcoming events" });
        }
    }

    /// <summary>
    /// Get a single event by ID
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <returns>Event details or 404 if not found</returns>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<Event>> GetEventById(Guid id)
    {
        try
        {
            var eventItem = await _eventService.GetEventByIdAsync(id, includeDetails: true);

            if (eventItem == null)
            {
                _logger.LogWarning("Event not found: {EventId}", id);
                return NotFound(new { message = "Event not found" });
            }

            // Only return public events for anonymous users
            if (eventItem.Visibility != EventVisibility.Public)
            {
                // Check if user is authenticated and has access
                if (!User.Identity?.IsAuthenticated == true)
                {
                    return NotFound(new { message = "Event not found" });
                }

                // Additional access checks can be added here
                // For now, authenticated users can see unlisted events
            }

            // Only show published events in public API
            if (eventItem.Status != EventStatus.Published)
            {
                return NotFound(new { message = "Event not found" });
            }

            _logger.LogInformation("Retrieved event: {EventId}", id);

            return Ok(eventItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event {EventId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the event" });
        }
    }

    /// <summary>
    /// Get a single event by slug for SEO-friendly URLs
    /// </summary>
    /// <param name="slug">Event slug</param>
    /// <returns>Event details or 404 if not found</returns>
    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<Event>> GetEventBySlug(string slug)
    {
        try
        {
            var eventItem = await _eventService.GetEventBySlugAsync(slug, includeDetails: true);

            if (eventItem == null)
            {
                _logger.LogWarning("Event not found by slug: {Slug}", slug);
                return NotFound(new { message = "Event not found" });
            }

            // Apply same visibility and status checks as GetEventById
            if (eventItem.Visibility != EventVisibility.Public)
            {
                if (!User.Identity?.IsAuthenticated == true)
                {
                    return NotFound(new { message = "Event not found" });
                }
            }

            if (eventItem.Status != EventStatus.Published)
            {
                return NotFound(new { message = "Event not found" });
            }

            _logger.LogInformation("Retrieved event by slug: {Slug} -> {EventId}", slug, eventItem.Id);

            return Ok(eventItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event by slug {Slug}", slug);
            return StatusCode(500, new { message = "An error occurred while retrieving the event" });
        }
    }

    /// <summary>
    /// Get events by tags for category browsing
    /// </summary>
    /// <param name="tags">Comma-separated list of tags</param>
    /// <param name="limit">Maximum number of events to return (max 30)</param>
    /// <returns>Events matching any of the provided tags</returns>
    [HttpGet("by-tags")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Event>>> GetEventsByTags(
        [FromQuery] string tags,
        [FromQuery] int limit = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tags))
            {
                return BadRequest(new { message = "Tags parameter is required" });
            }

            limit = Math.Min(30, Math.Max(1, limit)); // Limit to 30 events max

            var tagArray = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(tag => tag.Trim())
                              .Where(tag => !string.IsNullOrWhiteSpace(tag))
                              .ToArray();

            if (tagArray.Length == 0)
            {
                return BadRequest(new { message = "At least one valid tag is required" });
            }

            var events = await _eventService.GetEventsByTagsAsync(tagArray, limit);

            _logger.LogInformation("Retrieved {Count} events for tags: {Tags}", events.Count(), string.Join(", ", tagArray));

            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events by tags: {Tags}", tags);
            return StatusCode(500, new { message = "An error occurred while retrieving events by tags" });
        }
    }

    /// <summary>
    /// Check if registration is available for an event
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <returns>Registration availability status</returns>
    [HttpGet("{id:guid}/registration-availability")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> CheckRegistrationAvailability(Guid id)
    {
        try
        {
            var eventItem = await _eventService.GetEventByIdAsync(id, includeDetails: false);
            if (eventItem == null)
            {
                return NotFound(new { message = "Event not found" });
            }

            var isAvailable = await _eventService.IsRegistrationAvailableAsync(id);
            var stats = await _eventService.GetEventStatsAsync(id);

            return Ok(new
            {
                eventId = id,
                registrationAvailable = isAvailable,
                currentAttendees = stats?.CurrentAttendees ?? 0,
                maxAttendees = stats?.MaxAttendees ?? 0,
                availableSlots = stats?.AvailableSlots ?? 0,
                isFullyBooked = stats?.IsFullyBooked ?? false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking registration availability for event {EventId}", id);
            return StatusCode(500, new { message = "An error occurred while checking registration availability" });
        }
    }

    /// <summary>
    /// Get basic event statistics
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <returns>Event registration statistics</returns>
    [HttpGet("{id:guid}/stats")]
    [AllowAnonymous]
    public async Task<ActionResult<EventRegistrationStats>> GetEventStats(Guid id)
    {
        try
        {
            var stats = await _eventService.GetEventStatsAsync(id);
            if (stats == null)
            {
                return NotFound(new { message = "Event not found" });
            }

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event stats for {EventId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving event statistics" });
        }
    }
}