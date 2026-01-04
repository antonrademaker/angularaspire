using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.SessionManagement;

namespace PrivateApi.SessionManagement;

/// <summary>
/// Controller for managing sessions in events (Private API for organizers/admins)
/// </summary>
[ApiController]
[Route("api/v1/admin/sessions")]
// TODO: Re-enable authorization when authentication is fully implemented
// [Authorize(Policy = "OrganizerOrAdmin")]
[AllowAnonymous] // Development only - remove for production
[Produces("application/json")]
[Tags("Session Management")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(ISessionService sessionService, ILogger<SessionsController> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new session
    /// </summary>
    /// <param name="request">Session creation request</param>
    /// <returns>Created session details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
    {
        try
        {
            var result = await _sessionService.CreateSessionAsync(request);
            
            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("conflicts") == true)
                {
                    return Conflict(new ApiError("SCHEDULE_CONFLICT", result.Error));
                }
                if (result.Error?.Contains("already exists") == true)
                {
                    return Conflict(new ApiError("SESSION_EXISTS", result.Error));
                }
                return BadRequest(new ApiError("INVALID_REQUEST", result.Error ?? "Failed to create session"));
            }

            _logger.LogInformation("Session created: {SessionId} - {Title} for Event {EventId}", 
                result.Value?.Id, result.Value?.Title, request.EventId);

            return CreatedAtAction(nameof(GetSession), new { id = result.Value!.Id }, result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session for event {EventId}", request.EventId);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while creating the session"));
        }
    }

    /// <summary>
    /// Get session by ID
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <returns>Session details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSession(Guid id)
    {
        try
        {
            var session = await _sessionService.GetSessionAsync(id);
            
            if (session == null)
            {
                return NotFound(new ApiError("SESSION_NOT_FOUND", $"Session with ID {id} was not found"));
            }

            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session {SessionId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while retrieving the session"));
        }
    }

    /// <summary>
    /// Update an existing session
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <param name="request">Session update request</param>
    /// <returns>Updated session details</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSession(Guid id, [FromBody] UpdateSessionRequest request)
    {
        try
        {
            var result = await _sessionService.UpdateSessionAsync(id, request);
            
            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("not found") == true)
                {
                    return NotFound(new ApiError("SESSION_NOT_FOUND", result.Error));
                }
                if (result.Error?.Contains("conflicts") == true)
                {
                    return Conflict(new ApiError("SCHEDULE_CONFLICT", result.Error));
                }
                if (result.Error?.Contains("already exists") == true)
                {
                    return Conflict(new ApiError("SESSION_EXISTS", result.Error));
                }
                return BadRequest(new ApiError("INVALID_REQUEST", result.Error ?? "Failed to update session"));
            }

            _logger.LogInformation("Session updated: {SessionId} - {Title}", result.Value?.Id, result.Value?.Title);

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session {SessionId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while updating the session"));
        }
    }

    /// <summary>
    /// Delete a session
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSession(Guid id)
    {
        try
        {
            var result = await _sessionService.DeleteSessionAsync(id);
            
            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("not found") == true)
                {
                    return NotFound(new ApiError("SESSION_NOT_FOUND", result.Error));
                }
                return BadRequest(new ApiError("INVALID_REQUEST", result.Error ?? "Failed to delete session"));
            }

            _logger.LogInformation("Session deleted: {SessionId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session {SessionId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while deleting the session"));
        }
    }

    /// <summary>
    /// Search sessions with filters
    /// </summary>
    /// <param name="request">Session search request</param>
    /// <returns>Paged list of sessions</returns>
    [HttpPost("search")]
    [ProducesResponseType(typeof(PagedResult<SessionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchSessions([FromBody] SessionSearchRequest request)
    {
        try
        {
            var result = await _sessionService.SearchSessionsAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching sessions");
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while searching sessions"));
        }
    }

    /// <summary>
    /// Get sessions for an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="trackId">Optional track ID to filter by</param>
    /// <param name="includeUnpublished">Whether to include unpublished sessions</param>
    /// <returns>List of sessions</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SessionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventSessions([FromQuery] Guid eventId, [FromQuery] Guid? trackId = null, [FromQuery] bool includeUnpublished = false)
    {
        try
        {
            var sessions = await _sessionService.GetEventSessionsAsync(eventId, trackId, includeUnpublished);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sessions for event {EventId}", eventId);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while retrieving sessions"));
        }
    }

    /// <summary>
    /// Get sessions for a specific track
    /// </summary>
    /// <param name="trackId">Track ID</param>
    /// <param name="includeUnpublished">Whether to include unpublished sessions</param>
    /// <returns>List of sessions in the track</returns>
    [HttpGet("by-track/{trackId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<SessionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrackSessions(Guid trackId, [FromQuery] bool includeUnpublished = false)
    {
        try
        {
            var sessions = await _sessionService.GetTrackSessionsAsync(trackId, includeUnpublished);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sessions for track {TrackId}", trackId);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while retrieving track sessions"));
        }
    }

    /// <summary>
    /// Publish a session (make it visible to attendees)
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PublishSession(Guid id)
    {
        try
        {
            var result = await _sessionService.PublishSessionAsync(id);
            
            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("not found") == true)
                {
                    return NotFound(new ApiError("SESSION_NOT_FOUND", result.Error));
                }
                return BadRequest(new ApiError("INVALID_REQUEST", result.Error ?? "Failed to publish session"));
            }

            _logger.LogInformation("Session published: {SessionId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing session {SessionId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while publishing the session"));
        }
    }

    /// <summary>
    /// Unpublish a session (hide from attendees)
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:guid}/unpublish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnpublishSession(Guid id)
    {
        try
        {
            var result = await _sessionService.UnpublishSessionAsync(id);
            
            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("not found") == true)
                {
                    return NotFound(new ApiError("SESSION_NOT_FOUND", result.Error));
                }
                return BadRequest(new ApiError("INVALID_REQUEST", result.Error ?? "Failed to unpublish session"));
            }

            _logger.LogInformation("Session unpublished: {SessionId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unpublishing session {SessionId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while unpublishing the session"));
        }
    }

    /// <summary>
    /// Check session availability (capacity and conflicts)
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <returns>Session availability information</returns>
    [HttpGet("{id:guid}/availability")]
    [ProducesResponseType(typeof(SessionAvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckSessionAvailability(Guid id)
    {
        try
        {
            var availability = await _sessionService.CheckSessionAvailabilityAsync(id);
            return Ok(availability);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking session availability {SessionId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while checking session availability"));
        }
    }

    /// <summary>
    /// Detect scheduling conflicts for a session
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <param name="request">Conflict detection request</param>
    /// <returns>List of conflicting sessions</returns>
    [HttpPost("{id:guid}/conflicts")]
    [ProducesResponseType(typeof(IEnumerable<SessionConflict>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DetectSessionConflicts(Guid id, [FromBody] ConflictDetectionRequest request)
    {
        try
        {
            var conflicts = await _sessionService.DetectSessionConflictsAsync(id, request.StartTime, request.EndTime, request.Room);
            return Ok(conflicts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting conflicts for session {SessionId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while detecting conflicts"));
        }
    }

    /// <summary>
    /// Get session subscriptions
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <param name="includeWaitlisted">Whether to include waitlisted users</param>
    /// <returns>List of session subscribers</returns>
    [HttpGet("{id:guid}/subscriptions")]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessionSubscriptions(Guid id, [FromQuery] bool includeWaitlisted = false)
    {
        try
        {
            var subscriptions = await _sessionService.GetSessionSubscriptionsAsync(id, includeWaitlisted);
            return Ok(subscriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscriptions for session {SessionId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while retrieving subscriptions"));
        }
    }

    /// <summary>
    /// Get session waitlist
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <returns>Ordered list of waitlisted users</returns>
    [HttpGet("{id:guid}/waitlist")]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessionWaitlist(Guid id)
    {
        try
        {
            var waitlist = await _sessionService.GetSessionWaitlistAsync(id);
            return Ok(waitlist);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving waitlist for session {SessionId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while retrieving waitlist"));
        }
    }

    /// <summary>
    /// Process session waitlist (promote waiting users to confirmed)
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <param name="request">Waitlist processing request</param>
    /// <returns>Number of users promoted</returns>
    [HttpPost("{id:guid}/waitlist/process")]
    [ProducesResponseType(typeof(WaitlistProcessResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessWaitlist(Guid id, [FromBody] ProcessWaitlistRequest request)
    {
        try
        {
            var promotedCount = await _sessionService.ProcessWaitlistAsync(id, request.Count);
            
            _logger.LogInformation("Processed waitlist for session {SessionId}: {Count} users promoted", 
                id, promotedCount);

            return Ok(new WaitlistProcessResult { PromotedCount = promotedCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing waitlist for session {SessionId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while processing waitlist"));
        }
    }

    /// <summary>
    /// Start a session (change status to InProgress)
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartSession(Guid id)
    {
        try
        {
            var result = await _sessionService.StartSessionAsync(id);
            
            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("not found") == true)
                {
                    return NotFound(new ApiError("SESSION_NOT_FOUND", result.Error));
                }
                return BadRequest(new ApiError("INVALID_REQUEST", result.Error ?? "Failed to start session"));
            }

            _logger.LogInformation("Session started: {SessionId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting session {SessionId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while starting the session"));
        }
    }

    /// <summary>
    /// Complete a session (change status to Completed)
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteSession(Guid id)
    {
        try
        {
            var result = await _sessionService.CompleteSessionAsync(id);
            
            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("not found") == true)
                {
                    return NotFound(new ApiError("SESSION_NOT_FOUND", result.Error));
                }
                return BadRequest(new ApiError("INVALID_REQUEST", result.Error ?? "Failed to complete session"));
            }

            _logger.LogInformation("Session completed: {SessionId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing session {SessionId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while completing the session"));
        }
    }

    /// <summary>
    /// Cancel a session
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <param name="request">Session cancellation request</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelSession(Guid id, [FromBody] CancelSessionRequest request)
    {
        try
        {
            var result = await _sessionService.CancelSessionAsync(id, request.Reason);
            
            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("not found") == true)
                {
                    return NotFound(new ApiError("SESSION_NOT_FOUND", result.Error));
                }
                return BadRequest(new ApiError("INVALID_REQUEST", result.Error ?? "Failed to cancel session"));
            }

            _logger.LogInformation("Session cancelled: {SessionId}, Reason: {Reason}", id, request.Reason);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling session {SessionId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while cancelling the session"));
        }
    }

    /// <summary>
    /// Mark attendance for session participants
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <param name="request">Attendance marking request</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:guid}/attendance")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MarkAttendance(Guid id, [FromBody] MarkAttendanceRequest request)
    {
        try
        {
            var result = await _sessionService.MarkAttendanceAsync(id, request.Attendances);
            
            if (!result.IsSuccess)
            {
                return BadRequest(new ApiError("INVALID_REQUEST", result.Error ?? "Failed to mark attendance"));
            }

            _logger.LogInformation("Attendance marked for session {SessionId}: {Count} attendances processed", 
                id, request.Attendances.Count);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking attendance for session {SessionId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while marking attendance"));
        }
    }
}

// Additional DTOs for SessionsController

/// <summary>
/// Request for conflict detection
/// </summary>
public class ConflictDetectionRequest
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Room { get; set; }
}

/// <summary>
/// Request to process waitlist
/// </summary>
public class ProcessWaitlistRequest
{
    public int? Count { get; set; } // null means process all possible
}

/// <summary>
/// Result of waitlist processing
/// </summary>
public class WaitlistProcessResult
{
    public int PromotedCount { get; set; }
}

/// <summary>
/// Request to cancel a session
/// </summary>
public class CancelSessionRequest
{
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request to mark attendance
/// </summary>
public class MarkAttendanceRequest
{
    public List<SessionAttendance> Attendances { get; set; } = [];
}