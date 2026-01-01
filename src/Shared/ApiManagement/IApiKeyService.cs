using System.ComponentModel.DataAnnotations;
using Shared.Common;

namespace Shared.ApiManagement;

/// <summary>
/// Service interface for API key management
/// </summary>
public interface IApiKeyService
{
    #region API Key CRUD

    /// <summary>
    /// Create a new API key
    /// </summary>
    Task<CreateApiKeyResult> CreateApiKeyAsync(
        CreateApiKeyRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get an API key by ID
    /// </summary>
    Task<ApiKeyResponse?> GetApiKeyByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all API keys for a user
    /// </summary>
    Task<IEnumerable<ApiKeyResponse>> GetUserApiKeysAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an API key
    /// </summary>
    Task<ApiKeyResponse?> UpdateApiKeyAsync(
        Guid id,
        UpdateApiKeyRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Regenerate an API key (creates new key value)
    /// </summary>
    Task<CreateApiKeyResult?> RegenerateApiKeyAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke an API key
    /// </summary>
    Task<bool> RevokeApiKeyAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an API key permanently
    /// </summary>
    Task<bool> DeleteApiKeyAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    #endregion

    #region API Key Validation

    /// <summary>
    /// Validate an API key and return its details
    /// </summary>
    Task<ApiKeyValidationResult> ValidateApiKeyAsync(
        string apiKey,
        string? requiredScope = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Record API key usage
    /// </summary>
    Task RecordUsageAsync(
        Guid apiKeyId,
        string endpoint,
        string httpMethod,
        int statusCode,
        string? ipAddress,
        string? userAgent,
        long responseTimeMs,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region API Key Search

    /// <summary>
    /// Search API keys with filters
    /// </summary>
    Task<PagedResult<ApiKeyResponse>> SearchApiKeysAsync(
        ApiKeySearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get API key usage statistics
    /// </summary>
    Task<ApiKeyUsageStats> GetUsageStatsAsync(
        Guid apiKeyId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    #endregion
}

#region Request/Response DTOs

/// <summary>
/// Request to create an API key
/// </summary>
public record CreateApiKeyRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; init; }

    public ApiKeyTier Tier { get; init; } = ApiKeyTier.Free;

    [MaxLength(100)]
    public string? OrganizationName { get; init; }

    /// <summary>
    /// Comma-separated scopes (e.g., "events:read,registrations:write")
    /// </summary>
    [MaxLength(500)]
    public string? Scopes { get; init; }

    /// <summary>
    /// Comma-separated allowed IP addresses
    /// </summary>
    [MaxLength(1000)]
    public string? AllowedIpAddresses { get; init; }

    /// <summary>
    /// Comma-separated allowed origins
    /// </summary>
    [MaxLength(1000)]
    public string? AllowedOrigins { get; init; }

    /// <summary>
    /// Expiration date (null = never expires)
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    public bool WebhooksEnabled { get; init; }

    [MaxLength(500)]
    public string? WebhookUrl { get; init; }
}

/// <summary>
/// Request to update an API key
/// </summary>
public record UpdateApiKeyRequest
{
    [MaxLength(100)]
    public string? Name { get; init; }

    [MaxLength(500)]
    public string? Description { get; init; }

    public ApiKeyTier? Tier { get; init; }

    public ApiKeyStatus? Status { get; init; }

    [MaxLength(500)]
    public string? Scopes { get; init; }

    [MaxLength(1000)]
    public string? AllowedIpAddresses { get; init; }

    [MaxLength(1000)]
    public string? AllowedOrigins { get; init; }

    public DateTime? ExpiresAt { get; init; }

    public bool? WebhooksEnabled { get; init; }

    [MaxLength(500)]
    public string? WebhookUrl { get; init; }
}

/// <summary>
/// Request to search API keys
/// </summary>
public record ApiKeySearchRequest
{
    public Guid? UserId { get; init; }
    public ApiKeyTier? Tier { get; init; }
    public ApiKeyStatus? Status { get; init; }
    public string? SearchText { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Result of creating an API key (includes the raw key value)
/// </summary>
public record CreateApiKeyResult
{
    public bool Success { get; init; }
    public ApiKeyResponse? ApiKey { get; init; }
    
    /// <summary>
    /// The raw API key value - only shown once at creation
    /// </summary>
    public string? RawKey { get; init; }
    
    public string? ErrorMessage { get; init; }

    public static CreateApiKeyResult Succeeded(ApiKeyResponse apiKey, string rawKey) =>
        new() { Success = true, ApiKey = apiKey, RawKey = rawKey };

    public static CreateApiKeyResult Failed(string error) =>
        new() { Success = false, ErrorMessage = error };
}

/// <summary>
/// API key response DTO
/// </summary>
public record ApiKeyResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string KeyPrefix { get; init; } = string.Empty;
    public ApiKeyTier Tier { get; init; }
    public ApiKeyStatus Status { get; init; }
    public Guid UserId { get; init; }
    public string? UserEmail { get; init; }
    public string? OrganizationName { get; init; }
    public string[] Scopes { get; init; } = [];
    public string[] AllowedIpAddresses { get; init; } = [];
    public string[] AllowedOrigins { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
    public long TotalRequests { get; init; }
    public int RateLimit { get; init; }
    public bool WebhooksEnabled { get; init; }
    public string? WebhookUrl { get; init; }
    public bool IsValid { get; init; }
}

/// <summary>
/// Result of validating an API key
/// </summary>
public record ApiKeyValidationResult
{
    public bool IsValid { get; init; }
    public Guid? ApiKeyId { get; init; }
    public Guid? UserId { get; init; }
    public ApiKeyTier? Tier { get; init; }
    public string[] Scopes { get; init; } = [];
    public int RateLimit { get; init; }
    public int CurrentWindowRequests { get; init; }
    public bool RateLimitExceeded { get; init; }
    public string? ErrorMessage { get; init; }

    public static ApiKeyValidationResult Valid(
        Guid apiKeyId,
        Guid userId,
        ApiKeyTier tier,
        string[] scopes,
        int rateLimit,
        int currentRequests) => new()
    {
        IsValid = true,
        ApiKeyId = apiKeyId,
        UserId = userId,
        Tier = tier,
        Scopes = scopes,
        RateLimit = rateLimit,
        CurrentWindowRequests = currentRequests
    };

    public static ApiKeyValidationResult Invalid(string error) => new()
    {
        IsValid = false,
        ErrorMessage = error
    };

    public static ApiKeyValidationResult RateLimited(
        Guid apiKeyId,
        int rateLimit,
        int currentRequests) => new()
    {
        IsValid = false,
        ApiKeyId = apiKeyId,
        RateLimit = rateLimit,
        CurrentWindowRequests = currentRequests,
        RateLimitExceeded = true,
        ErrorMessage = $"Rate limit exceeded. Limit: {rateLimit}/min, Current: {currentRequests}"
    };
}

/// <summary>
/// API key usage statistics
/// </summary>
public record ApiKeyUsageStats
{
    public Guid ApiKeyId { get; init; }
    public long TotalRequests { get; init; }
    public long SuccessfulRequests { get; init; }
    public long FailedRequests { get; init; }
    public double AvgResponseTimeMs { get; init; }
    public DateTime? LastUsedAt { get; init; }
    public Dictionary<string, int> RequestsByEndpoint { get; init; } = new();
    public Dictionary<int, int> RequestsByStatusCode { get; init; } = new();
    public Dictionary<string, int> RequestsByHour { get; init; } = new();
}

#endregion
