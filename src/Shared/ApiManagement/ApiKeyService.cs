using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Common;

namespace Shared.ApiManagement;

/// <summary>
/// Service implementation for API key management
/// </summary>
public class ApiKeyService : IApiKeyService
{
    private readonly ApiKeyDbContext _context;
    private readonly ILogger<ApiKeyService> _logger;
    
    // Rate limit window duration
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(1);

    public ApiKeyService(ApiKeyDbContext context, ILogger<ApiKeyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region API Key CRUD

    public async Task<CreateApiKeyResult> CreateApiKeyAsync(
        CreateApiKeyRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate the raw API key
            var (rawKey, keyHash, keyPrefix) = GenerateApiKey(request.Tier);

            var apiKey = new ApiKey
            {
                KeyHash = keyHash,
                KeyPrefix = keyPrefix,
                Name = request.Name,
                Description = request.Description,
                Tier = request.Tier,
                Status = ApiKeyStatus.Active,
                UserId = userId,
                OrganizationName = request.OrganizationName,
                Scopes = request.Scopes,
                AllowedIpAddresses = request.AllowedIpAddresses,
                AllowedOrigins = request.AllowedOrigins,
                ExpiresAt = request.ExpiresAt,
                WebhooksEnabled = request.WebhooksEnabled,
                WebhookUrl = request.WebhookUrl,
                WebhookSecret = request.WebhooksEnabled ? GenerateWebhookSecret() : null
            };

            _context.ApiKeys.Add(apiKey);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created API key {ApiKeyId} with tier {Tier} for user {UserId}",
                apiKey.Id, apiKey.Tier, userId);

            return CreateApiKeyResult.Succeeded(MapToResponse(apiKey), rawKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API key for user {UserId}", userId);
            return CreateApiKeyResult.Failed("Failed to create API key");
        }
    }

    public async Task<ApiKeyResponse?> GetApiKeyByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await _context.ApiKeys
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.Id == id, cancellationToken);

        return apiKey == null ? null : MapToResponse(apiKey);
    }

    public async Task<IEnumerable<ApiKeyResponse>> GetUserApiKeysAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var apiKeys = await _context.ApiKeys
            .Include(k => k.User)
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);

        return apiKeys.Select(MapToResponse);
    }

    public async Task<ApiKeyResponse?> UpdateApiKeyAsync(
        Guid id,
        UpdateApiKeyRequest request,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await _context.ApiKeys
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.Id == id, cancellationToken);

        if (apiKey == null)
        {
            return null;
        }

        if (request.Name != null)
            apiKey.Name = request.Name;
        if (request.Description != null)
            apiKey.Description = request.Description;
        if (request.Tier.HasValue)
            apiKey.Tier = request.Tier.Value;
        if (request.Status.HasValue)
            apiKey.Status = request.Status.Value;
        if (request.Scopes != null)
            apiKey.Scopes = request.Scopes;
        if (request.AllowedIpAddresses != null)
            apiKey.AllowedIpAddresses = request.AllowedIpAddresses;
        if (request.AllowedOrigins != null)
            apiKey.AllowedOrigins = request.AllowedOrigins;
        if (request.ExpiresAt.HasValue)
            apiKey.ExpiresAt = request.ExpiresAt.Value;
        if (request.WebhooksEnabled.HasValue)
        {
            apiKey.WebhooksEnabled = request.WebhooksEnabled.Value;
            if (request.WebhooksEnabled.Value && string.IsNullOrEmpty(apiKey.WebhookSecret))
            {
                apiKey.WebhookSecret = GenerateWebhookSecret();
            }
        }
        if (request.WebhookUrl != null)
            apiKey.WebhookUrl = request.WebhookUrl;

        apiKey.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated API key {ApiKeyId}", id);

        return MapToResponse(apiKey);
    }

    public async Task<CreateApiKeyResult?> RegenerateApiKeyAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await _context.ApiKeys
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.Id == id, cancellationToken);

        if (apiKey == null)
        {
            return null;
        }

        var (rawKey, keyHash, keyPrefix) = GenerateApiKey(apiKey.Tier);

        apiKey.KeyHash = keyHash;
        apiKey.KeyPrefix = keyPrefix;
        apiKey.UpdatedAt = DateTime.UtcNow;
        apiKey.TotalRequests = 0;
        apiKey.CurrentWindowRequests = 0;
        apiKey.CurrentWindowStart = null;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Regenerated API key {ApiKeyId}", id);

        return CreateApiKeyResult.Succeeded(MapToResponse(apiKey), rawKey);
    }

    public async Task<bool> RevokeApiKeyAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == id, cancellationToken);

        if (apiKey == null)
        {
            return false;
        }

        apiKey.Status = ApiKeyStatus.Revoked;
        apiKey.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Revoked API key {ApiKeyId}", id);

        return true;
    }

    public async Task<bool> DeleteApiKeyAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == id, cancellationToken);

        if (apiKey == null)
        {
            return false;
        }

        _context.ApiKeys.Remove(apiKey);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted API key {ApiKeyId}", id);

        return true;
    }

    #endregion

    #region API Key Validation

    public async Task<ApiKeyValidationResult> ValidateApiKeyAsync(
        string apiKey,
        string? requiredScope = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return ApiKeyValidationResult.Invalid("API key is required");
        }

        // Hash the provided key
        var keyHash = HashApiKey(apiKey);

        var key = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash, cancellationToken);

        if (key == null)
        {
            return ApiKeyValidationResult.Invalid("Invalid API key");
        }

        // Check status
        if (key.Status != ApiKeyStatus.Active)
        {
            return ApiKeyValidationResult.Invalid($"API key is {key.Status.ToString().ToLower()}");
        }

        // Check expiration
        if (key.ExpiresAt.HasValue && key.ExpiresAt.Value <= DateTime.UtcNow)
        {
            key.Status = ApiKeyStatus.Expired;
            await _context.SaveChangesAsync(cancellationToken);
            return ApiKeyValidationResult.Invalid("API key has expired");
        }

        // Check IP restrictions
        if (!string.IsNullOrWhiteSpace(key.AllowedIpAddresses) && !string.IsNullOrWhiteSpace(ipAddress))
        {
            var allowedIps = key.GetAllowedIpAddresses();
            if (allowedIps.Length > 0 && !IsIpAllowed(ipAddress, allowedIps))
            {
                return ApiKeyValidationResult.Invalid("IP address not allowed");
            }
        }

        // Check scope
        if (!string.IsNullOrWhiteSpace(requiredScope) && !key.HasScope(requiredScope))
        {
            return ApiKeyValidationResult.Invalid($"Missing required scope: {requiredScope}");
        }

        // Check rate limit
        var rateLimit = key.GetRateLimit();
        var now = DateTime.UtcNow;
        
        // Reset window if needed
        if (!key.CurrentWindowStart.HasValue || 
            now - key.CurrentWindowStart.Value > RateLimitWindow)
        {
            key.CurrentWindowStart = now;
            key.CurrentWindowRequests = 0;
        }

        // Check if rate limit exceeded
        if (key.CurrentWindowRequests >= rateLimit)
        {
            return ApiKeyValidationResult.RateLimited(key.Id, rateLimit, key.CurrentWindowRequests);
        }

        // Increment request count
        key.CurrentWindowRequests++;
        key.TotalRequests++;
        key.LastUsedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        return ApiKeyValidationResult.Valid(
            key.Id,
            key.UserId,
            key.Tier,
            key.GetScopes(),
            rateLimit,
            key.CurrentWindowRequests);
    }

    public async Task RecordUsageAsync(
        Guid apiKeyId,
        string endpoint,
        string httpMethod,
        int statusCode,
        string? ipAddress,
        string? userAgent,
        long responseTimeMs,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var usageLog = new ApiKeyUsageLog
        {
            ApiKeyId = apiKeyId,
            Endpoint = endpoint,
            HttpMethod = httpMethod,
            StatusCode = statusCode,
            IpAddress = ipAddress,
            UserAgent = userAgent?.Length > 500 ? userAgent[..500] : userAgent,
            ResponseTimeMs = responseTimeMs,
            ErrorMessage = errorMessage
        };

        _context.ApiKeyUsageLogs.Add(usageLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region API Key Search

    public async Task<PagedResult<ApiKeyResponse>> SearchApiKeysAsync(
        ApiKeySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ApiKeys
            .Include(k => k.User)
            .AsQueryable();

        if (request.UserId.HasValue)
        {
            query = query.Where(k => k.UserId == request.UserId.Value);
        }

        if (request.Tier.HasValue)
        {
            query = query.Where(k => k.Tier == request.Tier.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(k => k.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var searchLower = request.SearchText.ToLower();
            query = query.Where(k =>
                k.Name.ToLower().Contains(searchLower) ||
                (k.Description != null && k.Description.ToLower().Contains(searchLower)) ||
                (k.OrganizationName != null && k.OrganizationName.ToLower().Contains(searchLower)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(k => k.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ApiKeyResponse>(
            items.Select(MapToResponse),
            totalCount,
            request.Page,
            request.PageSize
        );
    }

    public async Task<ApiKeyUsageStats> GetUsageStatsAsync(
        Guid apiKeyId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == apiKeyId, cancellationToken);

        if (apiKey == null)
        {
            return new ApiKeyUsageStats { ApiKeyId = apiKeyId };
        }

        var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow;

        var logs = await _context.ApiKeyUsageLogs
            .Where(l => l.ApiKeyId == apiKeyId &&
                        l.Timestamp >= fromDate &&
                        l.Timestamp <= toDate)
            .ToListAsync(cancellationToken);

        return new ApiKeyUsageStats
        {
            ApiKeyId = apiKeyId,
            TotalRequests = apiKey.TotalRequests,
            SuccessfulRequests = logs.Count(l => l.StatusCode >= 200 && l.StatusCode < 400),
            FailedRequests = logs.Count(l => l.StatusCode >= 400),
            AvgResponseTimeMs = logs.Count > 0 ? logs.Average(l => l.ResponseTimeMs) : 0,
            LastUsedAt = apiKey.LastUsedAt,
            RequestsByEndpoint = logs
                .GroupBy(l => l.Endpoint)
                .ToDictionary(g => g.Key, g => g.Count()),
            RequestsByStatusCode = logs
                .GroupBy(l => l.StatusCode)
                .ToDictionary(g => g.Key, g => g.Count()),
            RequestsByHour = logs
                .GroupBy(l => l.Timestamp.ToString("yyyy-MM-dd HH:00"))
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    #endregion

    #region Private Helpers

    private static (string rawKey, string keyHash, string keyPrefix) GenerateApiKey(ApiKeyTier tier)
    {
        // Generate tier prefix
        var tierPrefix = tier switch
        {
            ApiKeyTier.Free => "free",
            ApiKeyTier.Standard => "std",
            ApiKeyTier.Premium => "prem",
            ApiKeyTier.Enterprise => "ent",
            _ => "free"
        };

        // Generate random bytes
        var randomBytes = new byte[32];
        RandomNumberGenerator.Fill(randomBytes);
        var randomPart = Convert.ToBase64String(randomBytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")[..32];

        // Full key: evtmgr_[tier]_[random]
        var rawKey = $"evtmgr_{tierPrefix}_{randomPart}";
        var keyPrefix = rawKey[..16] + "...";
        var keyHash = HashApiKey(rawKey);

        return (rawKey, keyHash, keyPrefix);
    }

    private static string HashApiKey(string apiKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(bytes);
    }

    private static string GenerateWebhookSecret()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static bool IsIpAllowed(string ipAddress, string[] allowedIps)
    {
        foreach (var allowed in allowedIps)
        {
            if (allowed == ipAddress || allowed == "*")
            {
                return true;
            }
            
            // Simple CIDR support (e.g., "192.168.1.0/24")
            if (allowed.Contains('/'))
            {
                var parts = allowed.Split('/');
                if (parts.Length == 2 && 
                    int.TryParse(parts[1], out var prefixLength) &&
                    IsIpInCidr(ipAddress, parts[0], prefixLength))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static bool IsIpInCidr(string ipAddress, string cidrBase, int prefixLength)
    {
        // Simple IPv4 CIDR check
        try
        {
            var ipParts = ipAddress.Split('.').Select(int.Parse).ToArray();
            var baseParts = cidrBase.Split('.').Select(int.Parse).ToArray();
            
            if (ipParts.Length != 4 || baseParts.Length != 4)
                return false;

            var ipBits = (ipParts[0] << 24) | (ipParts[1] << 16) | (ipParts[2] << 8) | ipParts[3];
            var baseBits = (baseParts[0] << 24) | (baseParts[1] << 16) | (baseParts[2] << 8) | baseParts[3];
            var mask = prefixLength == 0 ? 0 : ~((1 << (32 - prefixLength)) - 1);

            return (ipBits & mask) == (baseBits & mask);
        }
        catch
        {
            return false;
        }
    }

    private static ApiKeyResponse MapToResponse(ApiKey apiKey)
    {
        return new ApiKeyResponse
        {
            Id = apiKey.Id,
            Name = apiKey.Name,
            Description = apiKey.Description,
            KeyPrefix = apiKey.KeyPrefix,
            Tier = apiKey.Tier,
            Status = apiKey.Status,
            UserId = apiKey.UserId,
            UserEmail = apiKey.User?.Email,
            OrganizationName = apiKey.OrganizationName,
            Scopes = apiKey.GetScopes(),
            AllowedIpAddresses = apiKey.GetAllowedIpAddresses(),
            AllowedOrigins = apiKey.GetAllowedOrigins(),
            CreatedAt = apiKey.CreatedAt,
            UpdatedAt = apiKey.UpdatedAt,
            ExpiresAt = apiKey.ExpiresAt,
            LastUsedAt = apiKey.LastUsedAt,
            TotalRequests = apiKey.TotalRequests,
            RateLimit = apiKey.GetRateLimit(),
            WebhooksEnabled = apiKey.WebhooksEnabled,
            WebhookUrl = apiKey.WebhookUrl,
            IsValid = apiKey.IsValid()
        };
    }

    #endregion
}
