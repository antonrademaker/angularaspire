using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.EventManagement;
using PrivateApi.SessionManagement;

namespace PrivateApi.EventManagement;

/// <summary>
/// Controller for managing social events (Private API for organizers/admins)
/// </summary>
[ApiController]
[Route("api/v1/admin/social-events")]
[Authorize(Policy = "OrganizerOrAdmin")]
[Produces("application/json")]
[Tags("Social Event Management")]
public class SocialEventsController : ControllerBase
{
    private readonly ISocialEventService _socialEventService;
    private readonly ILogger<SocialEventsController> _logger;

    public SocialEventsController(
        ISocialEventService socialEventService,
        ILogger<SocialEventsController> logger)
    {
        _socialEventService = socialEventService;
        _logger = logger;
    }

    #region Social Event CRUD

    /// <summary>
    /// Create a new social event
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SocialEventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSocialEvent(
        [FromBody] CreateSocialEventRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiError("UNAUTHORIZED", "User ID not found in claims"));
            }

            var socialEvent = await _socialEventService.CreateSocialEventAsync(
                request,
                userId.Value,
                cancellationToken);

            _logger.LogInformation(
                "Social event created: {SocialEventId} - {Title} for Event {EventId}",
                socialEvent.Id, socialEvent.Title, request.EventId);

            return CreatedAtAction(nameof(GetSocialEvent), new { id = socialEvent.Id }, socialEvent);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiError("INVALID_REQUEST", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating social event for event {EventId}", request.EventId);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while creating the social event"));
        }
    }

    /// <summary>
    /// Get a social event by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SocialEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSocialEvent(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var socialEvent = await _socialEventService.GetSocialEventByIdAsync(id, cancellationToken);

            if (socialEvent == null)
            {
                return NotFound(new ApiError("NOT_FOUND", "Social event not found"));
            }

            return Ok(socialEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting social event {SocialEventId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while retrieving the social event"));
        }
    }

    /// <summary>
    /// Get all social events for an event
    /// </summary>
    [HttpGet("by-event/{eventId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<SocialEventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSocialEventsByEvent(
        Guid eventId,
        [FromQuery] bool publishedOnly = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var socialEvents = await _socialEventService.GetSocialEventsForEventAsync(
                eventId,
                publishedOnly,
                cancellationToken);

            return Ok(socialEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting social events for event {EventId}", eventId);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while retrieving social events"));
        }
    }

    /// <summary>
    /// Search social events
    /// </summary>
    [HttpPost("search")]
    [ProducesResponseType(typeof(PagedResult<SocialEventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchSocialEvents(
        [FromBody] SocialEventSearchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _socialEventService.SearchSocialEventsAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching social events");
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while searching social events"));
        }
    }

    /// <summary>
    /// Update a social event
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SocialEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSocialEvent(
        Guid id,
        [FromBody] UpdateSocialEventRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var socialEvent = await _socialEventService.UpdateSocialEventAsync(id, request, cancellationToken);

            if (socialEvent == null)
            {
                return NotFound(new ApiError("NOT_FOUND", "Social event not found"));
            }

            _logger.LogInformation("Social event updated: {SocialEventId} - {Title}", id, socialEvent.Title);

            return Ok(socialEvent);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiError("INVALID_REQUEST", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating social event {SocialEventId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while updating the social event"));
        }
    }

    /// <summary>
    /// Delete a social event
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSocialEvent(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _socialEventService.DeleteSocialEventAsync(id, cancellationToken);

            if (!success)
            {
                return NotFound(new ApiError("NOT_FOUND", "Social event not found"));
            }

            _logger.LogInformation("Social event deleted: {SocialEventId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting social event {SocialEventId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while deleting the social event"));
        }
    }

    #endregion

    #region Social Event Status Management

    /// <summary>
    /// Publish a social event (make it visible to attendees)
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublishSocialEvent(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _socialEventService.PublishSocialEventAsync(id, cancellationToken);

            if (!success)
            {
                return NotFound(new ApiError("NOT_FOUND", "Social event not found"));
            }

            _logger.LogInformation("Social event published: {SocialEventId}", id);

            return Ok(new { message = "Social event published successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing social event {SocialEventId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while publishing the social event"));
        }
    }

    /// <summary>
    /// Cancel a social event
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSocialEvent(
        Guid id,
        [FromBody] CancelSocialEventRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _socialEventService.CancelSocialEventAsync(
                id,
                request?.Reason,
                cancellationToken);

            if (!success)
            {
                return NotFound(new ApiError("NOT_FOUND", "Social event not found"));
            }

            _logger.LogInformation("Social event cancelled: {SocialEventId}. Reason: {Reason}", id, request?.Reason ?? "Not specified");

            return Ok(new { message = "Social event cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling social event {SocialEventId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while cancelling the social event"));
        }
    }

    #endregion

    #region RSVP Management

    /// <summary>
    /// Get all RSVPs for a social event
    /// </summary>
    [HttpGet("{id:guid}/rsvps")]
    [ProducesResponseType(typeof(PagedResult<SocialEventRsvpResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRsvps(
        Guid id,
        [FromQuery] SocialEventRsvpStatus? status = null,
        [FromQuery] bool? isWaitlisted = null,
        [FromQuery] bool? isCheckedIn = null,
        [FromQuery] string? searchText = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "RegisteredAt",
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new RsvpSearchRequest
            {
                Status = status,
                IsWaitlisted = isWaitlisted,
                IsCheckedIn = isCheckedIn,
                SearchText = searchText,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDescending = sortDescending
            };

            var rsvps = await _socialEventService.GetRsvpsForSocialEventAsync(id, request, cancellationToken);

            return Ok(rsvps);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting RSVPs for social event {SocialEventId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while retrieving RSVPs"));
        }
    }

    /// <summary>
    /// Confirm an RSVP
    /// </summary>
    [HttpPost("rsvps/{rsvpId:guid}/confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmRsvp(Guid rsvpId, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _socialEventService.ConfirmRsvpAsync(rsvpId, cancellationToken);

            if (!success)
            {
                return NotFound(new ApiError("NOT_FOUND", "RSVP not found"));
            }

            _logger.LogInformation("RSVP confirmed: {RsvpId}", rsvpId);

            return Ok(new { message = "RSVP confirmed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming RSVP {RsvpId}", rsvpId);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while confirming the RSVP"));
        }
    }

    /// <summary>
    /// Mark an attendee as no-show
    /// </summary>
    [HttpPost("rsvps/{rsvpId:guid}/no-show")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsNoShow(Guid rsvpId, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _socialEventService.MarkAsNoShowAsync(rsvpId, cancellationToken);

            if (!success)
            {
                return NotFound(new ApiError("NOT_FOUND", "RSVP not found"));
            }

            _logger.LogInformation("RSVP marked as no-show: {RsvpId}", rsvpId);

            return Ok(new { message = "Attendee marked as no-show" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking RSVP {RsvpId} as no-show", rsvpId);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while marking as no-show"));
        }
    }

    /// <summary>
    /// Check in an attendee
    /// </summary>
    [HttpPost("rsvps/{rsvpId:guid}/check-in")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckInAttendee(Guid rsvpId, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _socialEventService.CheckInAttendeeAsync(rsvpId, cancellationToken);

            if (!success)
            {
                return NotFound(new ApiError("NOT_FOUND", "RSVP not found"));
            }

            _logger.LogInformation("Attendee checked in: {RsvpId}", rsvpId);

            return Ok(new { message = "Attendee checked in successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking in RSVP {RsvpId}", rsvpId);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while checking in"));
        }
    }

    /// <summary>
    /// Process waitlist (promote attendees from waitlist when spots become available)
    /// </summary>
    [HttpPost("{id:guid}/process-waitlist")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ProcessWaitlist(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var promotedCount = await _socialEventService.ProcessWaitlistAsync(id, cancellationToken);

            _logger.LogInformation("Processed waitlist for social event {SocialEventId}: {PromotedCount} attendees promoted", id, promotedCount);

            return Ok(new { message = $"Processed waitlist: {promotedCount} attendee(s) promoted", promotedCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing waitlist for social event {SocialEventId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while processing the waitlist"));
        }
    }

    #endregion

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }
}

/// <summary>
/// Request to cancel a social event
/// </summary>
public record CancelSocialEventRequest
{
    public string? Reason { get; init; }
}
