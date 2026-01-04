using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.SessionManagement;

namespace PrivateApi.SessionManagement;

/// <summary>
/// Controller for managing tracks in events (Private API for organizers/admins)
/// </summary>
[ApiController]
[Route("api/v1/admin/tracks")]
// TODO: Re-enable authorization when authentication is fully implemented
// [Authorize(Policy = "OrganizerOrAdmin")]
[AllowAnonymous] // Development only - remove for production
[Produces("application/json")]
[Tags("Track Management")]
public class TracksController : ControllerBase
{
    private readonly ITrackService _trackService;
    private readonly ILogger<TracksController> _logger;

    public TracksController(ITrackService trackService, ILogger<TracksController> logger)
    {
        _trackService = trackService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new track
    /// </summary>
    /// <param name="request">Track creation request</param>
    /// <returns>Created track details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TrackResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTrack([FromBody] CreateTrackRequest request)
    {
        try
        {
            var result = await _trackService.CreateTrackAsync(request);
            
            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("already exists") == true)
                {
                    return Conflict(new ApiError("TRACK_EXISTS", result.Error));
                }
                return BadRequest(new ApiError("INVALID_REQUEST", result.Error ?? "Failed to create track"));
            }

            _logger.LogInformation("Track created: {TrackId} - {Name} for Event {EventId}", 
                result.Value?.Id, result.Value?.Name, request.EventId);

            return CreatedAtAction(nameof(GetTrack), new { id = result.Value!.Id }, result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating track for event {EventId}", request.EventId);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while creating the track"));
        }
    }

    /// <summary>
    /// Get track by ID
    /// </summary>
    /// <param name="id">Track ID</param>
    /// <returns>Track details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TrackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTrack(Guid id)
    {
        try
        {
            var track = await _trackService.GetTrackAsync(id);
            
            if (track == null)
            {
                return NotFound(new ApiError("TRACK_NOT_FOUND", $"Track with ID {id} was not found"));
            }

            return Ok(track);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving track {TrackId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while retrieving the track"));
        }
    }

    /// <summary>
    /// Update an existing track
    /// </summary>
    /// <param name="id">Track ID</param>
    /// <param name="request">Track update request</param>
    /// <returns>Updated track details</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TrackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateTrack(Guid id, [FromBody] UpdateTrackRequest request)
    {
        try
        {
            var result = await _trackService.UpdateTrackAsync(id, request);
            
            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("not found") == true)
                {
                    return NotFound(new ApiError("TRACK_NOT_FOUND", result.Error));
                }
                if (result.Error?.Contains("already exists") == true)
                {
                    return Conflict(new ApiError("TRACK_EXISTS", result.Error));
                }
                return BadRequest(new ApiError("INVALID_REQUEST", result.Error ?? "Failed to update track"));
            }

            _logger.LogInformation("Track updated: {TrackId} - {Name}", result.Value?.Id, result.Value?.Name);

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating track {TrackId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while updating the track"));
        }
    }

    /// <summary>
    /// Delete a track
    /// </summary>
    /// <param name="id">Track ID</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTrack(Guid id)
    {
        try
        {
            var result = await _trackService.DeleteTrackAsync(id);
            
            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("not found") == true)
                {
                    return NotFound(new ApiError("TRACK_NOT_FOUND", result.Error));
                }
                return BadRequest(new ApiError("INVALID_REQUEST", result.Error ?? "Failed to delete track"));
            }

            _logger.LogInformation("Track deleted: {TrackId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting track {TrackId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while deleting the track"));
        }
    }

    /// <summary>
    /// Get tracks for an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="includeInactive">Whether to include inactive tracks</param>
    /// <returns>List of tracks</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TrackResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventTracks([FromQuery] Guid eventId, [FromQuery] bool includeInactive = false)
    {
        try
        {
            var tracks = await _trackService.GetEventTracksAsync(eventId, includeInactive);
            return Ok(tracks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tracks for event {EventId}", eventId);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while retrieving tracks"));
        }
    }

    /// <summary>
    /// Reorder tracks within an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="request">Track reordering request</param>
    /// <returns>Success result</returns>
    [HttpPost("reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReorderTracks([FromQuery] Guid eventId, [FromBody] ReorderTracksRequest request)
    {
        try
        {
            var result = await _trackService.ReorderTracksAsync(eventId, request.TrackOrders.Select(to => new TrackOrderItem 
            { 
                TrackId = to.TrackId, 
                DisplayOrder = to.DisplayOrder 
            }));
            
            if (!result.IsSuccess)
            {
                return BadRequest(new ApiError("INVALID_REQUEST", result.Error ?? "Failed to reorder tracks"));
            }

            _logger.LogInformation("Tracks reordered for event {EventId}", eventId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering tracks for event {EventId}", eventId);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while reordering tracks"));
        }
    }

    /// <summary>
    /// Activate or deactivate a track
    /// </summary>
    /// <param name="id">Track ID</param>
    /// <param name="request">Track status update request</param>
    /// <returns>Updated track details</returns>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(TrackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateTrackStatus(Guid id, [FromBody] UpdateTrackStatusRequest request)
    {
        try
        {
            var result = await _trackService.UpdateTrackStatusAsync(id, request.IsActive);
            
            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("not found") == true)
                {
                    return NotFound(new ApiError("TRACK_NOT_FOUND", result.Error));
                }
                return BadRequest(new ApiError("INVALID_REQUEST", result.Error ?? "Failed to update track status"));
            }

            _logger.LogInformation("Track status updated: {TrackId} - Active: {IsActive}", id, request.IsActive);

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating track status {TrackId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while updating track status"));
        }
    }
}

/// <summary>
/// Request to reorder tracks
/// </summary>
public class ReorderTracksRequest
{
    public List<TrackOrder> TrackOrders { get; set; } = [];
}

/// <summary>
/// Track order specification
/// </summary>
public class TrackOrder
{
    public Guid TrackId { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Request to update track status
/// </summary>
public class UpdateTrackStatusRequest
{
    public bool IsActive { get; set; }
}