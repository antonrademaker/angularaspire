using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.SessionManagement;

namespace PrivateApi.SessionManagement;

/// <summary>
/// Controller for managing speaker profiles (Private API for organizers/admins)
/// </summary>
[ApiController]
[Route("api/v1/admin/speakers")]
[Authorize(Policy = "OrganizerOrAdmin")]
[Produces("application/json")]
[Tags("Speaker Management")]
public class SpeakersController : ControllerBase
{
    private readonly ISpeakerService _speakerService;
    private readonly ILogger<SpeakersController> _logger;

    public SpeakersController(ISpeakerService speakerService, ILogger<SpeakersController> logger)
    {
        _speakerService = speakerService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new speaker profile
    /// </summary>
    /// <param name="request">Speaker profile creation request</param>
    /// <returns>Created speaker profile</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SpeakerProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSpeaker([FromBody] CreateSpeakerProfileRequest request)
    {
        try
        {
            var result = await _speakerService.CreateSpeakerProfileAsync(request);

            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("already exists") == true)
                {
                    return Conflict(new ApiError("SPEAKER_EXISTS", result.Error));
                }
                return BadRequest(new ApiError("INVALID_REQUEST", result.Error ?? "Failed to create speaker profile"));
            }

            _logger.LogInformation("Speaker profile created: {SpeakerId} - {DisplayName}",
                result.Value?.Id, result.Value?.DisplayName);

            return CreatedAtAction(nameof(GetSpeaker), new { id = result.Value!.Id }, result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating speaker profile");
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while creating the speaker profile"));
        }
    }

    /// <summary>
    /// Get speaker profile by ID
    /// </summary>
    /// <param name="id">Speaker profile ID</param>
    /// <returns>Speaker profile details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SpeakerProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSpeaker(Guid id)
    {
        try
        {
            var speaker = await _speakerService.GetSpeakerProfileAsync(id);

            if (speaker == null)
            {
                return NotFound(new ApiError("SPEAKER_NOT_FOUND", $"Speaker profile with ID {id} was not found"));
            }

            return Ok(speaker);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving speaker profile {SpeakerId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while retrieving the speaker profile"));
        }
    }

    /// <summary>
    /// Get speaker profile by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Speaker profile details</returns>
    [HttpGet("by-user/{userId:guid}")]
    [ProducesResponseType(typeof(SpeakerProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSpeakerByUserId(Guid userId)
    {
        try
        {
            var speaker = await _speakerService.GetSpeakerProfileByUserIdAsync(userId);

            if (speaker == null)
            {
                return NotFound(new ApiError("SPEAKER_NOT_FOUND", $"Speaker profile for user {userId} was not found"));
            }

            return Ok(speaker);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving speaker profile for user {UserId}", userId);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while retrieving the speaker profile"));
        }
    }

    /// <summary>
    /// Update an existing speaker profile
    /// </summary>
    /// <param name="id">Speaker profile ID</param>
    /// <param name="request">Speaker profile update request</param>
    /// <returns>Updated speaker profile</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SpeakerProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSpeaker(Guid id, [FromBody] UpdateSpeakerProfileRequest request)
    {
        try
        {
            var result = await _speakerService.UpdateSpeakerProfileAsync(id, request);

            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("not found") == true)
                {
                    return NotFound(new ApiError("SPEAKER_NOT_FOUND", result.Error));
                }
                return BadRequest(new ApiError("INVALID_REQUEST", result.Error ?? "Failed to update speaker profile"));
            }

            _logger.LogInformation("Speaker profile updated: {SpeakerId} - {DisplayName}",
                result.Value?.Id, result.Value?.DisplayName);

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating speaker profile {SpeakerId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while updating the speaker profile"));
        }
    }

    /// <summary>
    /// Delete a speaker profile
    /// </summary>
    /// <param name="id">Speaker profile ID</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSpeaker(Guid id)
    {
        try
        {
            var result = await _speakerService.DeleteSpeakerProfileAsync(id);

            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("not found") == true)
                {
                    return NotFound(new ApiError("SPEAKER_NOT_FOUND", result.Error));
                }
                if (result.Error?.Contains("assigned") == true)
                {
                    return BadRequest(new ApiError("SPEAKER_ASSIGNED", result.Error));
                }
                return BadRequest(new ApiError("INVALID_REQUEST", result.Error ?? "Failed to delete speaker profile"));
            }

            _logger.LogInformation("Speaker profile deleted: {SpeakerId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting speaker profile {SpeakerId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while deleting the speaker profile"));
        }
    }

    /// <summary>
    /// Search speaker profiles with filters
    /// </summary>
    /// <param name="request">Speaker search request</param>
    /// <returns>Paged list of speaker profiles</returns>
    [HttpPost("search")]
    [ProducesResponseType(typeof(PagedResult<SpeakerProfileResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchSpeakers([FromBody] SpeakerSearchRequest request)
    {
        try
        {
            var result = await _speakerService.SearchSpeakersAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching speaker profiles");
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while searching speaker profiles"));
        }
    }

    /// <summary>
    /// Get all speakers for an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <returns>List of speakers for the event</returns>
    [HttpGet("by-event/{eventId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<SpeakerProfileResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSpeakersByEvent(Guid eventId)
    {
        try
        {
            var speakers = await _speakerService.GetSpeakersByEventAsync(eventId);
            return Ok(speakers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving speakers for event {EventId}", eventId);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while retrieving speakers"));
        }
    }

    /// <summary>
    /// Assign a speaker to a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="request">Speaker assignment request</param>
    /// <returns>Updated session speakers</returns>
    [HttpPost("sessions/{sessionId:guid}/speakers")]
    [ProducesResponseType(typeof(IEnumerable<SessionSpeakerDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignSpeakerToSession(Guid sessionId, [FromBody] AssignSpeakerRequest request)
    {
        try
        {
            var result = await _speakerService.AssignSpeakerToSessionAsync(sessionId, request);

            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("not found") == true)
                {
                    return NotFound(new ApiError("NOT_FOUND", result.Error));
                }
                if (result.Error?.Contains("already assigned") == true)
                {
                    return Conflict(new ApiError("SPEAKER_ALREADY_ASSIGNED", result.Error));
                }
                if (result.Error?.Contains("conflict") == true)
                {
                    return Conflict(new ApiError("SCHEDULE_CONFLICT", result.Error));
                }
                return BadRequest(new ApiError("INVALID_REQUEST", result.Error ?? "Failed to assign speaker"));
            }

            _logger.LogInformation("Speaker {SpeakerId} assigned to session {SessionId} as {Role}",
                request.SpeakerId, sessionId, request.Role);

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning speaker {SpeakerId} to session {SessionId}", request.SpeakerId, sessionId);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while assigning the speaker"));
        }
    }

    /// <summary>
    /// Remove a speaker from a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="speakerId">Speaker profile ID</param>
    /// <returns>Updated session speakers</returns>
    [HttpDelete("sessions/{sessionId:guid}/speakers/{speakerId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSpeakerFromSession(Guid sessionId, Guid speakerId)
    {
        try
        {
            var result = await _speakerService.RemoveSpeakerFromSessionAsync(sessionId, speakerId);

            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("not found") == true)
                {
                    return NotFound(new ApiError("NOT_FOUND", result.Error));
                }
                return BadRequest(new ApiError("INVALID_REQUEST", result.Error ?? "Failed to remove speaker"));
            }

            _logger.LogInformation("Speaker {SpeakerId} removed from session {SessionId}", speakerId, sessionId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing speaker {SpeakerId} from session {SessionId}", speakerId, sessionId);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while removing the speaker"));
        }
    }

    /// <summary>
    /// Get speakers assigned to a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>List of speakers for the session</returns>
    [HttpGet("sessions/{sessionId:guid}/speakers")]
    [ProducesResponseType(typeof(IEnumerable<SessionSpeakerDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessionSpeakers(Guid sessionId)
    {
        try
        {
            var result = await _speakerService.GetSessionSpeakersAsync(sessionId);

            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("not found") == true)
                {
                    return NotFound(new ApiError("SESSION_NOT_FOUND", result.Error));
                }
                return BadRequest(new ApiError("INVALID_REQUEST", result.Error ?? "Failed to get session speakers"));
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving speakers for session {SessionId}", sessionId);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while retrieving session speakers"));
        }
    }

    /// <summary>
    /// Update a speaker's role or display order in a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="speakerId">Speaker profile ID</param>
    /// <param name="request">Speaker assignment update request</param>
    /// <returns>Updated session speaker</returns>
    [HttpPut("sessions/{sessionId:guid}/speakers/{speakerId:guid}")]
    [ProducesResponseType(typeof(SessionSpeakerDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSessionSpeaker(Guid sessionId, Guid speakerId, [FromBody] UpdateSessionSpeakerRequest request)
    {
        try
        {
            var result = await _speakerService.UpdateSessionSpeakerAsync(sessionId, speakerId, request);

            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("not found") == true)
                {
                    return NotFound(new ApiError("NOT_FOUND", result.Error));
                }
                return BadRequest(new ApiError("INVALID_REQUEST", result.Error ?? "Failed to update session speaker"));
            }

            _logger.LogInformation("Session speaker updated: Session {SessionId}, Speaker {SpeakerId}",
                sessionId, speakerId);

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session speaker {SpeakerId} for session {SessionId}", speakerId, sessionId);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while updating the session speaker"));
        }
    }

    /// <summary>
    /// Update speaker profile status
    /// </summary>
    /// <param name="id">Speaker profile ID</param>
    /// <param name="request">Status update request</param>
    /// <returns>Updated speaker profile</returns>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(SpeakerProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSpeakerStatus(Guid id, [FromBody] UpdateSpeakerStatusRequest request)
    {
        try
        {
            var result = await _speakerService.UpdateSpeakerStatusAsync(id, request.Status);

            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("not found") == true)
                {
                    return NotFound(new ApiError("SPEAKER_NOT_FOUND", result.Error));
                }
                return BadRequest(new ApiError("INVALID_REQUEST", result.Error ?? "Failed to update speaker status"));
            }

            _logger.LogInformation("Speaker profile status updated: {SpeakerId} to {Status}", id, request.Status);

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating speaker profile status {SpeakerId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while updating the speaker status"));
        }
    }
}
