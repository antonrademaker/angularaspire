using System.Net;
using System.Text.Json;
using Shared.ApiManagement;

namespace PublicApi.Middleware;

/// <summary>
/// Middleware for tiered API rate limiting based on API keys
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitingOptions _options;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        RateLimitingOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context, IApiKeyService apiKeyService)
    {
        // Check if this path requires API key authentication
        if (!RequiresApiKey(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Extract API key from request
        var apiKey = ExtractApiKey(context.Request);
        
        if (string.IsNullOrEmpty(apiKey))
        {
            await WriteErrorResponse(context, HttpStatusCode.Unauthorized, 
                "MISSING_API_KEY", 
                "API key is required. Include it in the X-Api-Key header or api_key query parameter.");
            return;
        }

        // Get client IP for validation
        var ipAddress = GetClientIpAddress(context);

        // Validate the API key
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var validationResult = await apiKeyService.ValidateApiKeyAsync(
            apiKey, 
            requiredScope: GetRequiredScope(context.Request),
            ipAddress: ipAddress);

        if (!validationResult.IsValid)
        {
            if (validationResult.RateLimitExceeded)
            {
                // Calculate retry-after (assume 1-minute window)
                var retryAfter = 60 - DateTime.UtcNow.Second;
                
                await WriteRateLimitResponse(context, validationResult, retryAfter);

                _logger.LogWarning(
                    "Rate limit exceeded for API key {ApiKeyId}. Current: {Current}/{Limit}",
                    validationResult.ApiKeyId,
                    validationResult.CurrentWindowRequests,
                    validationResult.RateLimit);
            }
            else
            {
                await WriteErrorResponse(context, HttpStatusCode.Unauthorized,
                    "INVALID_API_KEY",
                    validationResult.ErrorMessage ?? "Invalid API key");

                _logger.LogWarning(
                    "Invalid API key attempt from IP {IpAddress}: {Error}",
                    ipAddress,
                    validationResult.ErrorMessage);
            }
            return;
        }

        // Add rate limit headers to response
        AddRateLimitHeaders(context.Response, validationResult);

        // Store validation result in HttpContext for use by controllers
        context.Items["ApiKeyValidation"] = validationResult;
        context.Items["ApiKeyId"] = validationResult.ApiKeyId;
        context.Items["ApiKeyUserId"] = validationResult.UserId;
        context.Items["ApiKeyTier"] = validationResult.Tier;
        context.Items["ApiKeyScopes"] = validationResult.Scopes;

        // Execute the request
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            // Copy response back
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;

            // Record usage (fire and forget)
            _ = RecordUsageAsync(apiKeyService, validationResult, context, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Determines if the request path requires API key authentication
    /// </summary>
    private bool RequiresApiKey(PathString path)
    {
        // Check excluded paths first
        foreach (var excludedPath in _options.ExcludedPaths)
        {
            if (path.StartsWithSegments(excludedPath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Check if path is in the protected paths
        foreach (var protectedPath in _options.ProtectedPaths)
        {
            if (path.StartsWithSegments(protectedPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // By default, API paths require authentication
        return path.StartsWithSegments("/api/external", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extracts API key from request headers or query string
    /// </summary>
    private string? ExtractApiKey(HttpRequest request)
    {
        // Check header first (preferred)
        if (request.Headers.TryGetValue("X-Api-Key", out var headerValue))
        {
            return headerValue.FirstOrDefault();
        }

        // Check Authorization header with Bearer scheme
        if (request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var value = authHeader.FirstOrDefault();
            if (!string.IsNullOrEmpty(value) && value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = value["Bearer ".Length..].Trim();
                // Only use as API key if it looks like our format
                if (token.StartsWith("evtmgr_", StringComparison.OrdinalIgnoreCase))
                {
                    return token;
                }
            }
        }

        // Check query string as fallback (less secure)
        if (request.Query.TryGetValue("api_key", out var queryValue))
        {
            return queryValue.FirstOrDefault();
        }

        return null;
    }

    /// <summary>
    /// Gets the client IP address from the request
    /// </summary>
    private string? GetClientIpAddress(HttpContext context)
    {
        // Check X-Forwarded-For header (for reverse proxy scenarios)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var ip = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(ip))
            {
                return ip;
            }
        }

        // Check X-Real-IP header
        if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
        {
            var ip = realIp.FirstOrDefault();
            if (!string.IsNullOrEmpty(ip))
            {
                return ip;
            }
        }

        // Fall back to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Determines required scope based on request method and path
    /// </summary>
    private string? GetRequiredScope(HttpRequest request)
    {
        var path = request.Path.Value?.ToLowerInvariant() ?? "";
        var method = request.Method.ToUpperInvariant();

        // Determine resource from path
        string? resource = null;
        if (path.Contains("/events")) resource = "events";
        else if (path.Contains("/registrations")) resource = "registrations";
        else if (path.Contains("/sessions")) resource = "sessions";
        else if (path.Contains("/speakers")) resource = "speakers";
        else if (path.Contains("/social-events")) resource = "social_events";

        if (resource == null) return null;

        // Determine operation from method
        var operation = method switch
        {
            "GET" => "read",
            "POST" => "write",
            "PUT" => "write",
            "PATCH" => "write",
            "DELETE" => "delete",
            _ => "read"
        };

        return $"{resource}:{operation}";
    }

    /// <summary>
    /// Adds rate limit headers to response
    /// </summary>
    private void AddRateLimitHeaders(HttpResponse response, ApiKeyValidationResult validation)
    {
        var remaining = Math.Max(0, validation.RateLimit - validation.CurrentWindowRequests);
        var resetTime = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds();

        response.Headers["X-RateLimit-Limit"] = validation.RateLimit.ToString();
        response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
        response.Headers["X-RateLimit-Reset"] = resetTime.ToString();
        response.Headers["X-RateLimit-Policy"] = $"{validation.RateLimit};w=60";

        // Add tier information
        if (validation.Tier.HasValue)
        {
            response.Headers["X-Api-Tier"] = validation.Tier.Value.ToString();
        }
    }

    /// <summary>
    /// Writes an error response
    /// </summary>
    private async Task WriteErrorResponse(
        HttpContext context, 
        HttpStatusCode statusCode, 
        string errorCode, 
        string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var error = new RateLimitErrorResponse
        {
            Error = errorCode,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(error, _options.JsonOptions);
    }

    /// <summary>
    /// Writes a rate limit exceeded response
    /// </summary>
    private async Task WriteRateLimitResponse(
        HttpContext context, 
        ApiKeyValidationResult validation,
        int retryAfter)
    {
        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.ContentType = "application/json";
        
        // Add Retry-After header (seconds)
        context.Response.Headers["Retry-After"] = retryAfter.ToString();
        
        // Add rate limit headers
        context.Response.Headers["X-RateLimit-Limit"] = validation.RateLimit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = "0";
        context.Response.Headers["X-RateLimit-Reset"] = 
            DateTimeOffset.UtcNow.AddSeconds(retryAfter).ToUnixTimeSeconds().ToString();

        var error = new RateLimitErrorResponse
        {
            Error = "RATE_LIMIT_EXCEEDED",
            Message = $"Rate limit exceeded. Limit: {validation.RateLimit} requests per minute.",
            Limit = validation.RateLimit,
            Current = validation.CurrentWindowRequests,
            RetryAfter = retryAfter,
            Timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(error, _options.JsonOptions);
    }

    /// <summary>
    /// Records API usage asynchronously
    /// </summary>
    private async Task RecordUsageAsync(
        IApiKeyService apiKeyService,
        ApiKeyValidationResult validation,
        HttpContext context,
        long responseTimeMs)
    {
        try
        {
            if (validation.ApiKeyId.HasValue)
            {
                await apiKeyService.RecordUsageAsync(
                    validation.ApiKeyId.Value,
                    context.Request.Path.Value ?? "",
                    context.Request.Method,
                    context.Response.StatusCode,
                    GetClientIpAddress(context),
                    context.Request.Headers.UserAgent.ToString(),
                    responseTimeMs);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record API usage for key {ApiKeyId}", validation.ApiKeyId);
        }
    }
}

/// <summary>
/// Options for rate limiting middleware
/// </summary>
public class RateLimitingOptions
{
    /// <summary>
    /// Paths that require API key authentication
    /// </summary>
    public List<string> ProtectedPaths { get; set; } = ["/api/external"];

    /// <summary>
    /// Paths that are excluded from API key authentication
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = 
    [
        "/api/health",
        "/api/docs",
        "/openapi",
        "/swagger",
        "/hubs"
    ];

    /// <summary>
    /// JSON serialization options for error responses
    /// </summary>
    public JsonSerializerOptions JsonOptions { get; set; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}

/// <summary>
/// Rate limit error response model
/// </summary>
public class RateLimitErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? Limit { get; set; }
    public int? Current { get; set; }
    public int? RetryAfter { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Extension methods for rate limiting middleware
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    /// <summary>
    /// Adds rate limiting services to the service collection
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services,
        Action<RateLimitingOptions>? configure = null)
    {
        var options = new RateLimitingOptions();
        configure?.Invoke(options);
        
        services.AddSingleton(options);
        
        return services;
    }

    /// <summary>
    /// Adds the rate limiting middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseApiRateLimiting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitingMiddleware>();
    }
}

/// <summary>
/// HttpContext extension methods for accessing API key information
/// </summary>
public static class HttpContextApiKeyExtensions
{
    /// <summary>
    /// Gets the validated API key ID from the context
    /// </summary>
    public static Guid? GetApiKeyId(this HttpContext context)
    {
        return context.Items.TryGetValue("ApiKeyId", out var value) ? value as Guid? : null;
    }

    /// <summary>
    /// Gets the API key user ID from the context
    /// </summary>
    public static Guid? GetApiKeyUserId(this HttpContext context)
    {
        return context.Items.TryGetValue("ApiKeyUserId", out var value) ? value as Guid? : null;
    }

    /// <summary>
    /// Gets the API key tier from the context
    /// </summary>
    public static ApiKeyTier? GetApiKeyTier(this HttpContext context)
    {
        return context.Items.TryGetValue("ApiKeyTier", out var value) ? value as ApiKeyTier? : null;
    }

    /// <summary>
    /// Gets the API key scopes from the context
    /// </summary>
    public static string[] GetApiKeyScopes(this HttpContext context)
    {
        return context.Items.TryGetValue("ApiKeyScopes", out var value) 
            ? value as string[] ?? [] 
            : [];
    }

    /// <summary>
    /// Gets the full API key validation result from the context
    /// </summary>
    public static ApiKeyValidationResult? GetApiKeyValidation(this HttpContext context)
    {
        return context.Items.TryGetValue("ApiKeyValidation", out var value) 
            ? value as ApiKeyValidationResult 
            : null;
    }

    /// <summary>
    /// Checks if the API key has a specific scope
    /// </summary>
    public static bool HasApiKeyScope(this HttpContext context, string scope)
    {
        var scopes = context.GetApiKeyScopes();
        return scopes.Contains(scope, StringComparer.OrdinalIgnoreCase) ||
               scopes.Contains("*", StringComparer.OrdinalIgnoreCase);
    }
}
