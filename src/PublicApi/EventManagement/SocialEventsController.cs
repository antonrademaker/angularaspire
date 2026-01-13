using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.EventManagement;

namespace PublicApi.EventManagement;

/// <summary>
/// Public API controller for social event discovery and RSVP operations
/// Provides endpoints for attendees to discover social events and manage RSVPs
/// </summary>
[ApiController]
[Route("api/events/{eventId}/social-events")]
[Tags("Social Events")]
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

    /// <summary>
    /// Get all published social events for an event
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<SocialEventResponse>>> GetSocialEvents(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        try
        {
            var socialEvents = await _socialEventService.GetSocialEventsForEventAsync(
                eventId,
                publishedOnly: true,
                cancellationToken);

            return Ok(socialEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting social events for event {EventId}", eventId);
            return StatusCode(500, new { message = "An error occurred while retrieving social events" });
        }
    }

    /// <summary>
    /// Get a social event by ID
    /// </summary>
    [HttpGet("{socialEventId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<SocialEventResponse>> GetSocialEvent(
        Guid eventId,
        Guid socialEventId,
        CancellationToken cancellationToken)
    {
        try
        {
            var socialEvent = await _socialEventService.GetSocialEventByIdAsync(
                socialEventId,
                cancellationToken);

            if (socialEvent == null)
            {
                return NotFound(new { message = "Social event not found" });
            }

            // Verify it belongs to the specified event
            if (socialEvent.EventId != eventId)
            {
                return NotFound(new { message = "Social event not found in this event" });
            }

            return Ok(socialEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting social event {SocialEventId}", socialEventId);
            return StatusCode(500, new { message = "An error occurred while retrieving the social event" });
        }
    }

    /// <summary>
    /// Get a social event by slug
    /// </summary>
    [HttpGet("by-slug/{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<SocialEventResponse>> GetSocialEventBySlug(
        Guid eventId,
        string slug,
        CancellationToken cancellationToken)
    {
        try
        {
            var socialEvent = await _socialEventService.GetSocialEventBySlugAsync(
                eventId,
                slug,
                cancellationToken);

            if (socialEvent == null)
            {
                return NotFound(new { message = "Social event not found" });
            }

            return Ok(socialEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting social event by slug {Slug} for event {EventId}", slug, eventId);
            return StatusCode(500, new { message = "An error occurred while retrieving the social event" });
        }
    }

    /// <summary>
    /// Check RSVP availability for a social event
    /// </summary>
    [HttpGet("{socialEventId:guid}/availability")]
    [AllowAnonymous]
    public async Task<ActionResult<RsvpAvailability>> CheckAvailability(
        Guid eventId,
        Guid socialEventId,
        [FromQuery] int requestedSpots = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var availability = await _socialEventService.CheckRsvpAvailabilityAsync(
                socialEventId,
                requestedSpots,
                cancellationToken);

            return Ok(availability);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking availability for social event {SocialEventId}", socialEventId);
            return StatusCode(500, new { message = "An error occurred while checking availability" });
        }
    }

    /// <summary>
    /// Create an RSVP for a social event (requires authentication)
    /// </summary>
    [HttpPost("{socialEventId:guid}/rsvp")]
    [Authorize(Policy = "Attendee")]
    public async Task<ActionResult<RsvpResult>> CreateRsvp(
        Guid eventId,
        Guid socialEventId,
        [FromBody] CreateRsvpRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "User ID not found in claims" });
            }

            // Verify social event belongs to the event
            var socialEvent = await _socialEventService.GetSocialEventByIdAsync(socialEventId, cancellationToken);
            if (socialEvent == null || socialEvent.EventId != eventId)
            {
                return NotFound(new { message = "Social event not found in this event" });
            }

            var result = await _socialEventService.CreateRsvpAsync(
                socialEventId,
                userId.Value,
                request,
                cancellationToken);

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage, errorCode = result.ErrorCode?.ToString() });
            }

            _logger.LogInformation(
                "User {UserId} RSVP'd to social event {SocialEventId}. Waitlisted: {IsWaitlisted}",
                userId.Value, socialEventId, result.IsWaitlisted);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating RSVP for social event {SocialEventId}", socialEventId);
            return StatusCode(500, new { message = "An error occurred while creating the RSVP" });
        }
    }

    /// <summary>
    /// Get the current user's RSVP for a social event
    /// </summary>
    [HttpGet("{socialEventId:guid}/my-rsvp")]
    [Authorize(Policy = "Attendee")]
    public async Task<ActionResult<SocialEventRsvpResponse>> GetMyRsvp(
        Guid eventId,
        Guid socialEventId,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "User ID not found in claims" });
            }

            var rsvp = await _socialEventService.GetUserRsvpAsync(
                socialEventId,
                userId.Value,
                cancellationToken);

            if (rsvp == null)
            {
                return NotFound(new { message = "No RSVP found for this social event" });
            }

            return Ok(rsvp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user RSVP for social event {SocialEventId}", socialEventId);
            return StatusCode(500, new { message = "An error occurred while retrieving the RSVP" });
        }
    }

    /// <summary>
    /// Get all of the current user's RSVPs for social events in this event
    /// </summary>
    [HttpGet("my-rsvps")]
    [Authorize(Policy = "Attendee")]
    public async Task<ActionResult<IEnumerable<SocialEventRsvpResponse>>> GetMyRsvps(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "User ID not found in claims" });
            }

            var rsvps = await _socialEventService.GetUserRsvpsForEventAsync(
                eventId,
                userId.Value,
                cancellationToken);

            return Ok(rsvps);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user RSVPs for event {EventId}", eventId);
            return StatusCode(500, new { message = "An error occurred while retrieving RSVPs" });
        }
    }

    /// <summary>
    /// Update the current user's RSVP
    /// </summary>
    [HttpPut("{socialEventId:guid}/rsvp/{rsvpId:guid}")]
    [Authorize(Policy = "Attendee")]
    public async Task<ActionResult<RsvpResult>> UpdateMyRsvp(
        Guid eventId,
        Guid socialEventId,
        Guid rsvpId,
        [FromBody] UpdateRsvpRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "User ID not found in claims" });
            }

            // Verify the RSVP belongs to the current user
            var existingRsvp = await _socialEventService.GetUserRsvpAsync(socialEventId, userId.Value, cancellationToken);
            if (existingRsvp == null || existingRsvp.Id != rsvpId)
            {
                return Forbid();
            }

            var result = await _socialEventService.UpdateRsvpAsync(
                rsvpId,
                request,
                cancellationToken);

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage, errorCode = result.ErrorCode?.ToString() });
            }

            _logger.LogInformation("User {UserId} updated RSVP {RsvpId}", userId.Value, rsvpId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating RSVP {RsvpId}", rsvpId);
            return StatusCode(500, new { message = "An error occurred while updating the RSVP" });
        }
    }

    /// <summary>
    /// Cancel the current user's RSVP
    /// </summary>
    [HttpDelete("{socialEventId:guid}/rsvp/{rsvpId:guid}")]
    [Authorize(Policy = "Attendee")]
    public async Task<ActionResult> CancelMyRsvp(
        Guid eventId,
        Guid socialEventId,
        Guid rsvpId,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "User ID not found in claims" });
            }

            // Verify the RSVP belongs to the current user
            var existingRsvp = await _socialEventService.GetUserRsvpAsync(socialEventId, userId.Value, cancellationToken);
            if (existingRsvp == null || existingRsvp.Id != rsvpId)
            {
                return Forbid();
            }

            var success = await _socialEventService.CancelRsvpAsync(rsvpId, cancellationToken);

            if (!success)
            {
                return NotFound(new { message = "RSVP not found" });
            }

            _logger.LogInformation("User {UserId} cancelled RSVP {RsvpId}", userId.Value, rsvpId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling RSVP {RsvpId}", rsvpId);
            return StatusCode(500, new { message = "An error occurred while cancelling the RSVP" });
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }
}
