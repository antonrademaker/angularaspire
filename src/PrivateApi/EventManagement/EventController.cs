using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.EventManagement;
using Shared.EventManagement.Entities;

namespace PrivateApi.EventManagement;

/// <summary>
/// Controller for managing events (Admin/Organizer)
/// </summary>
[ApiController]
[Route("api/events")]
[Authorize] // Requires authentication
public class EventController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly ILogger<EventController> _logger;

    public EventController(IEventService eventService, ILogger<EventController> logger)
    {
        _eventService = eventService;
        _logger = logger;
    }

    /// <summary>
    /// Search for events with filtering and pagination
    /// </summary>
    /// <param name="request">Search criteria</param>
    /// <returns>Paginated list of events</returns>
    [HttpGet]
    public async Task<ActionResult<EventSearchResult>> GetEvents([FromQuery] EventSearchRequest request)
    {
        var result = await _eventService.SearchEventsAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Get a single event by ID
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <returns>Event details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<Event>> GetEvent(Guid id)
    {
        var evt = await _eventService.GetEventByIdAsync(id);
        if (evt == null)
        {
            return NotFound();
        }
        return Ok(evt);
    }

    /// <summary>
    /// Create a new event
    /// </summary>
    /// <param name="request">Event creation data</param>
    /// <returns>Created event</returns>
    [HttpPost]
    public async Task<ActionResult<Event>> CreateEvent([FromBody] CreateEventRequest request)
    {
        // TODO: Get actual user ID from claims
        var userId = Guid.NewGuid(); 

        try
        {
            var evt = await _eventService.CreateEventAsync(request, userId);
            return CreatedAtAction(nameof(GetEvent), new { id = evt.Id }, evt);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update an existing event
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <param name="request">Updated event data</param>
    /// <returns>Updated event</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<Event>> UpdateEvent(Guid id, [FromBody] UpdateEventRequest request)
    {
        // TODO: Get actual user ID from claims
        var userId = Guid.NewGuid();

        var evt = await _eventService.UpdateEventAsync(id, request, userId);
        if (evt == null)
        {
            return NotFound();
        }
        return Ok(evt);
    }

    /// <summary>
    /// Delete an event
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(Guid id)
    {
        // TODO: Get actual user ID from claims
        var userId = Guid.NewGuid();

        var result = await _eventService.DeleteEventAsync(id, userId);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }
}
