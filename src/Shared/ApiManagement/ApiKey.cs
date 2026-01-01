using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.UserManagement;

namespace Shared.ApiManagement;

/// <summary>
/// API key tiers with different rate limits and capabilities
/// </summary>
public enum ApiKeyTier
{
    /// <summary>
    /// Free tier - Basic access with limited rate limits
    /// 100 requests/minute, read-only access
    /// </summary>
    Free = 0,

    /// <summary>
    /// Standard tier - Moderate rate limits with read/write access
    /// 500 requests/minute, CRUD operations
    /// </summary>
    Standard = 1,

    /// <summary>
    /// Premium tier - Higher rate limits with full access
    /// 2000 requests/minute, full API access
    /// </summary>
    Premium = 2,

    /// <summary>
    /// Enterprise tier - Highest rate limits with priority support
    /// 10000 requests/minute, full API access, webhooks
    /// </summary>
    Enterprise = 3
}

/// <summary>
/// API key status
/// </summary>
public enum ApiKeyStatus
{
    Active = 0,
    Suspended = 1,
    Revoked = 2,
    Expired = 3
}

/// <summary>
/// API key entity for external system integrations
/// </summary>
public class ApiKey
{
    /// <summary>
    /// Unique identifier for the API key record
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The API key value (hashed for storage, only shown once at creation)
    /// Format: evtmgr_[tier]_[random_base64]
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>
    /// Key prefix for identification (e.g., "evtmgr_std_abc123")
    /// The first 16 characters of the key shown to user
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name for the API key
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the API key's purpose
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// The tier determining rate limits and capabilities
    /// </summary>
    public ApiKeyTier Tier { get; set; } = ApiKeyTier.Free;

    /// <summary>
    /// Current status of the API key
    /// </summary>
    public ApiKeyStatus Status { get; set; } = ApiKeyStatus.Active;

    /// <summary>
    /// User who owns this API key
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// Organization this key belongs to (optional)
    /// </summary>
    [MaxLength(100)]
    public string? OrganizationName { get; set; }

    /// <summary>
    /// Comma-separated list of allowed scopes
    /// e.g., "events:read,registrations:write,sessions:read"
    /// </summary>
    [MaxLength(500)]
    public string? Scopes { get; set; }

    /// <summary>
    /// Comma-separated list of allowed IP addresses (CIDR notation supported)
    /// Empty means all IPs allowed
    /// </summary>
    [MaxLength(1000)]
    public string? AllowedIpAddresses { get; set; }

    /// <summary>
    /// Comma-separated list of allowed origins for CORS
    /// </summary>
    [MaxLength(1000)]
    public string? AllowedOrigins { get; set; }

    /// <summary>
    /// When the API key was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the API key was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// When the API key expires (null = never expires)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// When the API key was last used
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Total number of times this key has been used
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Number of requests in the current rate limit window
    /// </summary>
    public int CurrentWindowRequests { get; set; }

    /// <summary>
    /// Start of the current rate limit window
    /// </summary>
    public DateTime? CurrentWindowStart { get; set; }

    /// <summary>
    /// Whether webhooks are enabled for this key
    /// </summary>
    public bool WebhooksEnabled { get; set; }

    /// <summary>
    /// Webhook endpoint URL if enabled
    /// </summary>
    [MaxLength(500)]
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Secret for webhook signature verification
    /// </summary>
    [MaxLength(100)]
    public string? WebhookSecret { get; set; }

    /// <summary>
    /// Custom metadata as JSON
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }

    #region Helper Methods

    /// <summary>
    /// Get the list of scopes as an array
    /// </summary>
    public string[] GetScopes()
    {
        return string.IsNullOrWhiteSpace(Scopes)
            ? []
            : Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// Check if this key has a specific scope
    /// </summary>
    public bool HasScope(string scope)
    {
        var scopes = GetScopes();
        return scopes.Contains(scope) || scopes.Contains("*");
    }

    /// <summary>
    /// Get the list of allowed IP addresses
    /// </summary>
    public string[] GetAllowedIpAddresses()
    {
        return string.IsNullOrWhiteSpace(AllowedIpAddresses)
            ? []
            : AllowedIpAddresses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// Get the list of allowed origins
    /// </summary>
    public string[] GetAllowedOrigins()
    {
        return string.IsNullOrWhiteSpace(AllowedOrigins)
            ? []
            : AllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// Check if the key is currently valid
    /// </summary>
    public bool IsValid()
    {
        return Status == ApiKeyStatus.Active &&
               (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);
    }

    /// <summary>
    /// Get rate limit for this key's tier (requests per minute)
    /// </summary>
    public int GetRateLimit()
    {
        return Tier switch
        {
            ApiKeyTier.Free => 100,
            ApiKeyTier.Standard => 500,
            ApiKeyTier.Premium => 2000,
            ApiKeyTier.Enterprise => 10000,
            _ => 100
        };
    }

    #endregion
}

/// <summary>
/// API key usage log entry
/// </summary>
public class ApiKeyUsageLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApiKeyId { get; set; }
    
    [ForeignKey(nameof(ApiKeyId))]
    public ApiKey? ApiKey { get; set; }
    
    [MaxLength(100)]
    public string Endpoint { get; set; } = string.Empty;
    
    [MaxLength(10)]
    public string HttpMethod { get; set; } = string.Empty;
    
    public int StatusCode { get; set; }
    
    [MaxLength(50)]
    public string? IpAddress { get; set; }
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    public long ResponseTimeMs { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
}
