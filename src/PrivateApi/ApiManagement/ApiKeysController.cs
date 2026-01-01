using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateApi.SessionManagement;
using Shared.ApiManagement;
using Shared.Common;
using System.Security.Claims;

namespace PrivateApi.ApiManagement;

/// <summary>
/// Controller for managing API keys (Private API for organizers/admins)
/// </summary>
[ApiController]
[Route("api/v1/admin/api-keys")]
[Authorize(Policy = "OrganizerOrAdmin")]
[Produces("application/json")]
[Tags("API Key Management")]
public class ApiKeysController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(
        IApiKeyService apiKeyService,
        ILogger<ApiKeysController> logger)
    {
        _apiKeyService = apiKeyService;
        _logger = logger;
    }

    #region API Key CRUD

    /// <summary>
    /// Create a new API key
    /// </summary>
    /// <remarks>
    /// Creates a new API key for the authenticated user.
    /// The raw key value is only returned once during creation - save it securely!
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(CreateApiKeyResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateApiKey(
        [FromBody] CreateApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiError("UNAUTHORIZED", "User ID not found in claims"));
            }

            var result = await _apiKeyService.CreateApiKeyAsync(
                request,
                userId.Value,
                cancellationToken);

            if (!result.Success)
            {
                return BadRequest(new ApiError("CREATE_FAILED", result.ErrorMessage ?? "Failed to create API key"));
            }

            _logger.LogInformation(
                "API key created: {ApiKeyId} - {Name} for user {UserId}",
                result.ApiKey?.Id, request.Name, userId);

            return CreatedAtAction(
                nameof(GetApiKey),
                new { id = result.ApiKey?.Id },
                result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API key");
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while creating the API key"));
        }
    }

    /// <summary>
    /// Get an API key by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApiKey(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var apiKey = await _apiKeyService.GetApiKeyByIdAsync(id, cancellationToken);

            if (apiKey == null)
            {
                return NotFound(new ApiError("NOT_FOUND", $"API key with ID {id} not found"));
            }

            // Users can only view their own API keys (unless admin)
            if (apiKey.UserId != userId && !IsAdmin())
            {
                return Forbid();
            }

            return Ok(apiKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API key {ApiKeyId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while retrieving the API key"));
        }
    }

    /// <summary>
    /// Get all API keys for the current user
    /// </summary>
    [HttpGet("my-keys")]
    [ProducesResponseType(typeof(IEnumerable<ApiKeyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyApiKeys(CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new ApiError("UNAUTHORIZED", "User ID not found in claims"));
            }

            var apiKeys = await _apiKeyService.GetUserApiKeysAsync(userId.Value, cancellationToken);
            return Ok(apiKeys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user API keys");
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while retrieving API keys"));
        }
    }

    /// <summary>
    /// Search API keys (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(PagedResult<ApiKeyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchApiKeys(
        [FromQuery] ApiKeySearchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _apiKeyService.SearchApiKeysAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching API keys");
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while searching API keys"));
        }
    }

    /// <summary>
    /// Update an API key
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateApiKey(
        Guid id,
        [FromBody] UpdateApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var existingKey = await _apiKeyService.GetApiKeyByIdAsync(id, cancellationToken);

            if (existingKey == null)
            {
                return NotFound(new ApiError("NOT_FOUND", $"API key with ID {id} not found"));
            }

            // Users can only update their own API keys (unless admin)
            if (existingKey.UserId != userId && !IsAdmin())
            {
                return Forbid();
            }

            // Non-admins cannot change tier
            if (request.Tier.HasValue && !IsAdmin())
            {
                return BadRequest(new ApiError("FORBIDDEN", "Only admins can change API key tier"));
            }

            var result = await _apiKeyService.UpdateApiKeyAsync(id, request, cancellationToken);

            _logger.LogInformation("Updated API key {ApiKeyId}", id);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating API key {ApiKeyId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while updating the API key"));
        }
    }

    /// <summary>
    /// Regenerate an API key
    /// </summary>
    /// <remarks>
    /// Generates a new key value while keeping all other settings.
    /// The old key will no longer work after regeneration.
    /// </remarks>
    [HttpPost("{id:guid}/regenerate")]
    [ProducesResponseType(typeof(CreateApiKeyResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegenerateApiKey(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var existingKey = await _apiKeyService.GetApiKeyByIdAsync(id, cancellationToken);

            if (existingKey == null)
            {
                return NotFound(new ApiError("NOT_FOUND", $"API key with ID {id} not found"));
            }

            // Users can only regenerate their own API keys (unless admin)
            if (existingKey.UserId != userId && !IsAdmin())
            {
                return Forbid();
            }

            var result = await _apiKeyService.RegenerateApiKeyAsync(id, cancellationToken);

            _logger.LogInformation("Regenerated API key {ApiKeyId}", id);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating API key {ApiKeyId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while regenerating the API key"));
        }
    }

    /// <summary>
    /// Revoke an API key
    /// </summary>
    /// <remarks>
    /// Revokes the API key, preventing any further use.
    /// The key record is preserved for audit purposes.
    /// </remarks>
    [HttpPost("{id:guid}/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeApiKey(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var existingKey = await _apiKeyService.GetApiKeyByIdAsync(id, cancellationToken);

            if (existingKey == null)
            {
                return NotFound(new ApiError("NOT_FOUND", $"API key with ID {id} not found"));
            }

            // Users can only revoke their own API keys (unless admin)
            if (existingKey.UserId != userId && !IsAdmin())
            {
                return Forbid();
            }

            var result = await _apiKeyService.RevokeApiKeyAsync(id, cancellationToken);

            if (!result)
            {
                return NotFound(new ApiError("NOT_FOUND", $"API key with ID {id} not found"));
            }

            _logger.LogInformation("Revoked API key {ApiKeyId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking API key {ApiKeyId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while revoking the API key"));
        }
    }

    /// <summary>
    /// Delete an API key permanently
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteApiKey(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var existingKey = await _apiKeyService.GetApiKeyByIdAsync(id, cancellationToken);

            if (existingKey == null)
            {
                return NotFound(new ApiError("NOT_FOUND", $"API key with ID {id} not found"));
            }

            // Users can only delete their own API keys (unless admin)
            if (existingKey.UserId != userId && !IsAdmin())
            {
                return Forbid();
            }

            var result = await _apiKeyService.DeleteApiKeyAsync(id, cancellationToken);

            if (!result)
            {
                return NotFound(new ApiError("NOT_FOUND", $"API key with ID {id} not found"));
            }

            _logger.LogInformation("Deleted API key {ApiKeyId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting API key {ApiKeyId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while deleting the API key"));
        }
    }

    #endregion

    #region API Key Usage

    /// <summary>
    /// Get usage statistics for an API key
    /// </summary>
    [HttpGet("{id:guid}/usage")]
    [ProducesResponseType(typeof(ApiKeyUsageStats), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApiKeyUsage(
        Guid id,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var existingKey = await _apiKeyService.GetApiKeyByIdAsync(id, cancellationToken);

            if (existingKey == null)
            {
                return NotFound(new ApiError("NOT_FOUND", $"API key with ID {id} not found"));
            }

            // Users can only view usage for their own API keys (unless admin)
            if (existingKey.UserId != userId && !IsAdmin())
            {
                return Forbid();
            }

            var stats = await _apiKeyService.GetUsageStatsAsync(id, from, to, cancellationToken);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API key usage for {ApiKeyId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while retrieving usage statistics"));
        }
    }

    #endregion

    #region Admin Operations

    /// <summary>
    /// Update API key tier (Admin only)
    /// </summary>
    [HttpPost("{id:guid}/tier")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateApiKeyTier(
        Guid id,
        [FromBody] UpdateTierRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _apiKeyService.UpdateApiKeyAsync(
                id,
                new UpdateApiKeyRequest { Tier = request.Tier },
                cancellationToken);

            if (result == null)
            {
                return NotFound(new ApiError("NOT_FOUND", $"API key with ID {id} not found"));
            }

            _logger.LogInformation(
                "Admin updated API key {ApiKeyId} tier to {Tier}",
                id, request.Tier);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating API key tier for {ApiKeyId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while updating the API key tier"));
        }
    }

    /// <summary>
    /// Suspend an API key (Admin only)
    /// </summary>
    [HttpPost("{id:guid}/suspend")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SuspendApiKey(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _apiKeyService.UpdateApiKeyAsync(
                id,
                new UpdateApiKeyRequest { Status = ApiKeyStatus.Suspended },
                cancellationToken);

            if (result == null)
            {
                return NotFound(new ApiError("NOT_FOUND", $"API key with ID {id} not found"));
            }

            _logger.LogInformation("Admin suspended API key {ApiKeyId}", id);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending API key {ApiKeyId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while suspending the API key"));
        }
    }

    /// <summary>
    /// Reactivate a suspended API key (Admin only)
    /// </summary>
    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReactivateApiKey(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var existingKey = await _apiKeyService.GetApiKeyByIdAsync(id, cancellationToken);

            if (existingKey == null)
            {
                return NotFound(new ApiError("NOT_FOUND", $"API key with ID {id} not found"));
            }

            if (existingKey.Status == ApiKeyStatus.Revoked)
            {
                return BadRequest(new ApiError("INVALID_STATE", "Cannot reactivate a revoked API key"));
            }

            var result = await _apiKeyService.UpdateApiKeyAsync(
                id,
                new UpdateApiKeyRequest { Status = ApiKeyStatus.Active },
                cancellationToken);

            _logger.LogInformation("Admin reactivated API key {ApiKeyId}", id);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating API key {ApiKeyId}", id);
            return StatusCode(500, new ApiError("INTERNAL_ERROR", "An error occurred while reactivating the API key"));
        }
    }

    #endregion

    #region Helper Methods

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private bool IsAdmin()
    {
        return User.IsInRole("Admin");
    }

    #endregion
}

/// <summary>
/// Request to update API key tier
/// </summary>
public record UpdateTierRequest
{
    public ApiKeyTier Tier { get; init; }
}
