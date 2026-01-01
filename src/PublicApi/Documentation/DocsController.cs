using Microsoft.AspNetCore.Mvc;

namespace PublicApi.Documentation;

/// <summary>
/// Controller for external API documentation
/// Provides self-documenting API information
/// </summary>
[ApiController]
[Route("api/docs")]
[Produces("application/json")]
public class DocsController : ControllerBase
{
    private readonly ILogger<DocsController> _logger;
    private readonly IWebHostEnvironment _environment;

    public DocsController(ILogger<DocsController> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Get API overview and getting started guide
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiOverview), 200)]
    public IActionResult GetApiOverview()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        
        var overview = new ApiOverview
        {
            Name = "Event Management External API",
            Version = "1.0.0",
            Description = "REST API for external integrations with the Event Management System",
            BaseUrl = baseUrl,
            DocumentationUrl = $"{baseUrl}/api/docs",
            OpenApiUrl = $"{baseUrl}/openapi/v1.json",
            Authentication = new AuthenticationInfo
            {
                Type = "API Key",
                Description = "All external API endpoints require an API key for authentication",
                HeaderName = "X-Api-Key",
                AlternativeQueryParam = "api_key",
                HowToObtain = "Create an API key from the Admin portal or contact support"
            },
            RateLimits = GetRateLimits(),
            GettingStarted = new GettingStartedGuide
            {
                Steps = 
                [
                    new GettingStartedStep { Order = 1, Title = "Get an API Key", Description = "Create an API key from the admin portal. You'll need to select a tier based on your usage needs." },
                    new GettingStartedStep { Order = 2, Title = "Include your API key", Description = "Add the X-Api-Key header to all requests, or use the Bearer scheme in the Authorization header." },
                    new GettingStartedStep { Order = 3, Title = "Make your first request", Description = "Try GET /api/external/events to list all events and verify your authentication works." },
                    new GettingStartedStep { Order = 4, Title = "Check rate limits", Description = "Monitor the X-RateLimit-* headers in responses to track your API usage." }
                ],
                QuickExample = new QuickExample
                {
                    Description = "List all events",
                    Curl = "curl -X GET 'https://api.example.com/api/external/events' -H 'X-Api-Key: your_api_key_here'",
                    Response = @"{
  ""success"": true,
  ""data"": {
    ""items"": [...],
    ""totalCount"": 10,
    ""currentPage"": 1,
    ""pageSize"": 20
  },
  ""timestamp"": ""2025-01-15T12:00:00Z""
}"
                }
            },
            Endpoints = GetEndpointDocumentation(baseUrl)
        };

        return Ok(overview);
    }

    /// <summary>
    /// Get rate limiting documentation
    /// </summary>
    [HttpGet("rate-limits")]
    [ProducesResponseType(typeof(RateLimitDocumentation), 200)]
    public IActionResult GetRateLimitDocs()
    {
        var docs = new RateLimitDocumentation
        {
            Overview = "Rate limiting is applied to all external API endpoints based on your API key tier.",
            Window = "1 minute sliding window",
            Headers = new RateLimitHeaders
            {
                Limit = "X-RateLimit-Limit - Maximum requests allowed per window",
                Remaining = "X-RateLimit-Remaining - Requests remaining in current window",
                Reset = "X-RateLimit-Reset - Unix timestamp when the window resets",
                Policy = "X-RateLimit-Policy - Rate limit policy description"
            },
            Tiers = GetRateLimits(),
            ExceededResponse = new RateLimitExceededInfo
            {
                StatusCode = 429,
                StatusName = "Too Many Requests",
                Headers = new Dictionary<string, string>
                {
                    ["Retry-After"] = "Seconds to wait before retrying",
                    ["X-RateLimit-Limit"] = "Your limit",
                    ["X-RateLimit-Remaining"] = "0",
                    ["X-RateLimit-Reset"] = "Reset timestamp"
                },
                ExampleResponse = @"{
  ""error"": ""RATE_LIMIT_EXCEEDED"",
  ""message"": ""Rate limit exceeded. Limit: 100 requests per minute."",
  ""limit"": 100,
  ""current"": 101,
  ""retryAfter"": 45,
  ""timestamp"": ""2025-01-15T12:00:00Z""
}"
            },
            BestPractices = 
            [
                "Monitor the X-RateLimit-Remaining header and slow down requests when approaching the limit",
                "Implement exponential backoff when receiving 429 responses",
                "Cache responses where appropriate to reduce API calls",
                "Consider upgrading your tier if you consistently hit rate limits",
                "Use bulk endpoints where available to reduce the number of requests"
            ]
        };

        return Ok(docs);
    }

    /// <summary>
    /// Get authentication documentation
    /// </summary>
    [HttpGet("authentication")]
    [ProducesResponseType(typeof(AuthenticationDocumentation), 200)]
    public IActionResult GetAuthenticationDocs()
    {
        var docs = new AuthenticationDocumentation
        {
            Overview = "The External API uses API keys for authentication. Each API key is associated with a user account and has specific permissions based on its tier and scopes.",
            Methods = 
            [
                new AuthMethodDoc
                {
                    Name = "X-Api-Key Header (Recommended)",
                    Description = "Include your API key in the X-Api-Key header",
                    Example = "X-Api-Key: evtmgr_std_abc123def456..."
                },
                new AuthMethodDoc
                {
                    Name = "Bearer Token",
                    Description = "Use the Authorization header with Bearer scheme",
                    Example = "Authorization: Bearer evtmgr_std_abc123def456..."
                },
                new AuthMethodDoc
                {
                    Name = "Query Parameter",
                    Description = "Include the API key as a query parameter (not recommended for production)",
                    Example = "GET /api/external/events?api_key=evtmgr_std_abc123def456..."
                }
            ],
            ApiKeyFormat = new ApiKeyFormatDoc
            {
                Pattern = "evtmgr_[tier]_[random]",
                TierCodes = new Dictionary<string, string>
                {
                    ["free"] = "Free tier",
                    ["std"] = "Standard tier",
                    ["prm"] = "Premium tier",
                    ["ent"] = "Enterprise tier"
                },
                Example = "evtmgr_std_a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0"
            },
            Scopes = GetScopeDocumentation(),
            Errors = new AuthErrorsDoc
            {
                MissingKey = new AuthErrorDoc
                {
                    StatusCode = 401,
                    ErrorCode = "MISSING_API_KEY",
                    Message = "API key is required. Include it in the X-Api-Key header or api_key query parameter."
                },
                InvalidKey = new AuthErrorDoc
                {
                    StatusCode = 401,
                    ErrorCode = "INVALID_API_KEY",
                    Message = "Invalid API key"
                },
                InsufficientScope = new AuthErrorDoc
                {
                    StatusCode = 403,
                    ErrorCode = "INSUFFICIENT_SCOPE",
                    Message = "This operation requires the '[scope]' scope"
                },
                InsufficientTier = new AuthErrorDoc
                {
                    StatusCode = 403,
                    ErrorCode = "INSUFFICIENT_TIER",
                    Message = "This operation requires [tier] tier or above"
                }
            }
        };

        return Ok(docs);
    }

    /// <summary>
    /// Get endpoints documentation
    /// </summary>
    [HttpGet("endpoints")]
    [ProducesResponseType(typeof(EndpointsDocumentation), 200)]
    public IActionResult GetEndpointsDocs()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        
        var docs = new EndpointsDocumentation
        {
            BaseUrl = $"{baseUrl}/api/external",
            Version = "1.0",
            Endpoints = GetEndpointDocumentation(baseUrl)
        };

        return Ok(docs);
    }

    /// <summary>
    /// Get error codes documentation
    /// </summary>
    [HttpGet("errors")]
    [ProducesResponseType(typeof(ErrorCodesDocumentation), 200)]
    public IActionResult GetErrorsDocs()
    {
        var docs = new ErrorCodesDocumentation
        {
            Overview = "The API uses standard HTTP status codes and returns error details in a consistent JSON format.",
            ResponseFormat = @"{
  ""success"": false,
  ""error"": {
    ""code"": ""ERROR_CODE"",
    ""message"": ""Human-readable error message""
  },
  ""timestamp"": ""2025-01-15T12:00:00Z""
}",
            StatusCodes = 
            [
                new StatusCodeDoc { Code = 200, Name = "OK", Description = "Request succeeded" },
                new StatusCodeDoc { Code = 201, Name = "Created", Description = "Resource created successfully" },
                new StatusCodeDoc { Code = 204, Name = "No Content", Description = "Request succeeded with no response body" },
                new StatusCodeDoc { Code = 400, Name = "Bad Request", Description = "Invalid request parameters or body" },
                new StatusCodeDoc { Code = 401, Name = "Unauthorized", Description = "Missing or invalid API key" },
                new StatusCodeDoc { Code = 403, Name = "Forbidden", Description = "Insufficient permissions or tier" },
                new StatusCodeDoc { Code = 404, Name = "Not Found", Description = "Resource not found" },
                new StatusCodeDoc { Code = 429, Name = "Too Many Requests", Description = "Rate limit exceeded" },
                new StatusCodeDoc { Code = 500, Name = "Internal Server Error", Description = "Server error occurred" }
            ],
            ErrorCodes = 
            [
                new ErrorCodeDoc { Code = "MISSING_API_KEY", Description = "API key was not provided", HttpStatus = 401 },
                new ErrorCodeDoc { Code = "INVALID_API_KEY", Description = "API key is invalid, expired, or revoked", HttpStatus = 401 },
                new ErrorCodeDoc { Code = "RATE_LIMIT_EXCEEDED", Description = "Rate limit for your tier has been exceeded", HttpStatus = 429 },
                new ErrorCodeDoc { Code = "INSUFFICIENT_SCOPE", Description = "API key doesn't have required scope", HttpStatus = 403 },
                new ErrorCodeDoc { Code = "INSUFFICIENT_TIER", Description = "Operation requires higher tier API key", HttpStatus = 403 },
                new ErrorCodeDoc { Code = "EVENT_NOT_FOUND", Description = "Event with specified ID was not found", HttpStatus = 404 },
                new ErrorCodeDoc { Code = "SOCIAL_EVENT_NOT_FOUND", Description = "Social event with specified ID was not found", HttpStatus = 404 },
                new ErrorCodeDoc { Code = "CREATE_FAILED", Description = "Failed to create the resource", HttpStatus = 400 },
                new ErrorCodeDoc { Code = "UPDATE_FAILED", Description = "Failed to update the resource", HttpStatus = 400 },
                new ErrorCodeDoc { Code = "DELETE_FAILED", Description = "Failed to delete the resource", HttpStatus = 400 }
            ]
        };

        return Ok(docs);
    }

    #region Private Methods

    private static List<TierRateLimit> GetRateLimits()
    {
        return
        [
            new TierRateLimit { Tier = "Free", RequestsPerMinute = 100, Description = "Basic access with read-only permissions" },
            new TierRateLimit { Tier = "Standard", RequestsPerMinute = 500, Description = "Read/write access for most operations" },
            new TierRateLimit { Tier = "Premium", RequestsPerMinute = 2000, Description = "Full API access including delete operations" },
            new TierRateLimit { Tier = "Enterprise", RequestsPerMinute = 10000, Description = "Highest rate limits with priority support" }
        ];
    }

    private static List<ScopeDoc> GetScopeDocumentation()
    {
        return
        [
            new ScopeDoc { Scope = "events:read", Description = "Read event information", MinTier = "Free" },
            new ScopeDoc { Scope = "events:write", Description = "Create and update events", MinTier = "Standard" },
            new ScopeDoc { Scope = "events:delete", Description = "Delete events", MinTier = "Premium" },
            new ScopeDoc { Scope = "registrations:read", Description = "Read registration information", MinTier = "Free" },
            new ScopeDoc { Scope = "registrations:write", Description = "Create and manage registrations", MinTier = "Standard" },
            new ScopeDoc { Scope = "sessions:read", Description = "Read session information", MinTier = "Free" },
            new ScopeDoc { Scope = "sessions:write", Description = "Create and manage sessions", MinTier = "Standard" },
            new ScopeDoc { Scope = "social_events:read", Description = "Read social event information", MinTier = "Free" },
            new ScopeDoc { Scope = "social_events:write", Description = "Create and manage social events", MinTier = "Standard" },
            new ScopeDoc { Scope = "*", Description = "Full access to all operations", MinTier = "Enterprise" }
        ];
    }

    private static List<EndpointDoc> GetEndpointDocumentation(string baseUrl)
    {
        return
        [
            new EndpointDoc
            {
                Method = "GET",
                Path = "/api/external/events",
                Description = "List all events with pagination",
                RequiredScope = "events:read",
                MinTier = "Free",
                Parameters = 
                [
                    new ParamDoc { Name = "page", Type = "integer", Location = "query", Description = "Page number (default: 1)" },
                    new ParamDoc { Name = "pageSize", Type = "integer", Location = "query", Description = "Items per page (default: 20, max: 100)" },
                    new ParamDoc { Name = "status", Type = "string", Location = "query", Description = "Filter by event status" },
                    new ParamDoc { Name = "fromDate", Type = "datetime", Location = "query", Description = "Filter events starting after this date" },
                    new ParamDoc { Name = "toDate", Type = "datetime", Location = "query", Description = "Filter events starting before this date" }
                ],
                ExampleRequest = $"GET {baseUrl}/api/external/events?page=1&pageSize=20&status=Published",
                ExampleResponse = @"{
  ""success"": true,
  ""data"": {
    ""items"": [
      {
        ""id"": ""3fa85f64-5717-4562-b3fc-2c963f66afa6"",
        ""title"": ""Tech Conference 2025"",
        ""description"": ""Annual technology conference"",
        ""startDate"": ""2025-03-15T09:00:00Z"",
        ""endDate"": ""2025-03-17T18:00:00Z"",
        ""location"": ""Convention Center"",
        ""maxCapacity"": 500,
        ""currentRegistrations"": 325,
        ""status"": ""Published""
      }
    ],
    ""totalCount"": 15,
    ""currentPage"": 1,
    ""pageSize"": 20,
    ""totalPages"": 1,
    ""hasNextPage"": false,
    ""hasPreviousPage"": false
  },
  ""timestamp"": ""2025-01-15T12:00:00Z""
}"
            },
            new EndpointDoc
            {
                Method = "GET",
                Path = "/api/external/events/{id}",
                Description = "Get event by ID",
                RequiredScope = "events:read",
                MinTier = "Free",
                Parameters = 
                [
                    new ParamDoc { Name = "id", Type = "guid", Location = "path", Description = "Event unique identifier", Required = true }
                ],
                ExampleRequest = $"GET {baseUrl}/api/external/events/3fa85f64-5717-4562-b3fc-2c963f66afa6"
            },
            new EndpointDoc
            {
                Method = "POST",
                Path = "/api/external/events",
                Description = "Create a new event",
                RequiredScope = "events:write",
                MinTier = "Standard",
                RequestBody = @"{
  ""title"": ""My Event"",
  ""description"": ""Event description"",
  ""startDate"": ""2025-06-01T09:00:00Z"",
  ""endDate"": ""2025-06-01T18:00:00Z"",
  ""location"": ""Venue Name"",
  ""maxCapacity"": 100,
  ""isVirtual"": false
}",
                ExampleRequest = $"POST {baseUrl}/api/external/events"
            },
            new EndpointDoc
            {
                Method = "PUT",
                Path = "/api/external/events/{id}",
                Description = "Update an existing event",
                RequiredScope = "events:write",
                MinTier = "Standard",
                Parameters = 
                [
                    new ParamDoc { Name = "id", Type = "guid", Location = "path", Description = "Event unique identifier", Required = true }
                ],
                ExampleRequest = $"PUT {baseUrl}/api/external/events/3fa85f64-5717-4562-b3fc-2c963f66afa6"
            },
            new EndpointDoc
            {
                Method = "DELETE",
                Path = "/api/external/events/{id}",
                Description = "Delete an event",
                RequiredScope = "events:delete",
                MinTier = "Premium",
                Parameters = 
                [
                    new ParamDoc { Name = "id", Type = "guid", Location = "path", Description = "Event unique identifier", Required = true }
                ],
                ExampleRequest = $"DELETE {baseUrl}/api/external/events/3fa85f64-5717-4562-b3fc-2c963f66afa6"
            },
            new EndpointDoc
            {
                Method = "GET",
                Path = "/api/external/events/{eventId}/social-events",
                Description = "List social events for a parent event",
                RequiredScope = "social_events:read",
                MinTier = "Free",
                Parameters = 
                [
                    new ParamDoc { Name = "eventId", Type = "guid", Location = "path", Description = "Parent event ID", Required = true }
                ],
                ExampleRequest = $"GET {baseUrl}/api/external/events/3fa85f64-5717-4562-b3fc-2c963f66afa6/social-events"
            },
            new EndpointDoc
            {
                Method = "GET",
                Path = "/api/external/social-events/{id}",
                Description = "Get social event by ID",
                RequiredScope = "social_events:read",
                MinTier = "Free",
                Parameters = 
                [
                    new ParamDoc { Name = "id", Type = "guid", Location = "path", Description = "Social event unique identifier", Required = true }
                ],
                ExampleRequest = $"GET {baseUrl}/api/external/social-events/3fa85f64-5717-4562-b3fc-2c963f66afa6"
            },
            new EndpointDoc
            {
                Method = "GET",
                Path = "/api/external/me",
                Description = "Get API key information and rate limit status",
                RequiredScope = "Any",
                MinTier = "Free",
                ExampleRequest = $"GET {baseUrl}/api/external/me",
                ExampleResponse = @"{
  ""success"": true,
  ""data"": {
    ""apiKeyId"": ""3fa85f64-5717-4562-b3fc-2c963f66afa6"",
    ""userId"": ""3fa85f64-5717-4562-b3fc-2c963f66afa6"",
    ""tier"": ""Standard"",
    ""scopes"": [""events:read"", ""events:write""],
    ""rateLimit"": 500,
    ""currentUsage"": 45,
    ""remainingRequests"": 455
  },
  ""timestamp"": ""2025-01-15T12:00:00Z""
}"
            }
        ];
    }

    #endregion
}

#region DTOs

public class ApiOverview
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string DocumentationUrl { get; set; } = string.Empty;
    public string OpenApiUrl { get; set; } = string.Empty;
    public AuthenticationInfo Authentication { get; set; } = new();
    public List<TierRateLimit> RateLimits { get; set; } = [];
    public GettingStartedGuide GettingStarted { get; set; } = new();
    public List<EndpointDoc> Endpoints { get; set; } = [];
}

public class AuthenticationInfo
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string HeaderName { get; set; } = string.Empty;
    public string AlternativeQueryParam { get; set; } = string.Empty;
    public string HowToObtain { get; set; } = string.Empty;
}

public class TierRateLimit
{
    public string Tier { get; set; } = string.Empty;
    public int RequestsPerMinute { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class GettingStartedGuide
{
    public List<GettingStartedStep> Steps { get; set; } = [];
    public QuickExample QuickExample { get; set; } = new();
}

public class GettingStartedStep
{
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class QuickExample
{
    public string Description { get; set; } = string.Empty;
    public string Curl { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
}

public class RateLimitDocumentation
{
    public string Overview { get; set; } = string.Empty;
    public string Window { get; set; } = string.Empty;
    public RateLimitHeaders Headers { get; set; } = new();
    public List<TierRateLimit> Tiers { get; set; } = [];
    public RateLimitExceededInfo ExceededResponse { get; set; } = new();
    public List<string> BestPractices { get; set; } = [];
}

public class RateLimitHeaders
{
    public string Limit { get; set; } = string.Empty;
    public string Remaining { get; set; } = string.Empty;
    public string Reset { get; set; } = string.Empty;
    public string Policy { get; set; } = string.Empty;
}

public class RateLimitExceededInfo
{
    public int StatusCode { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = [];
    public string ExampleResponse { get; set; } = string.Empty;
}

public class AuthenticationDocumentation
{
    public string Overview { get; set; } = string.Empty;
    public List<AuthMethodDoc> Methods { get; set; } = [];
    public ApiKeyFormatDoc ApiKeyFormat { get; set; } = new();
    public List<ScopeDoc> Scopes { get; set; } = [];
    public AuthErrorsDoc Errors { get; set; } = new();
}

public class AuthMethodDoc
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
}

public class ApiKeyFormatDoc
{
    public string Pattern { get; set; } = string.Empty;
    public Dictionary<string, string> TierCodes { get; set; } = [];
    public string Example { get; set; } = string.Empty;
}

public class ScopeDoc
{
    public string Scope { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MinTier { get; set; } = string.Empty;
}

public class AuthErrorsDoc
{
    public AuthErrorDoc MissingKey { get; set; } = new();
    public AuthErrorDoc InvalidKey { get; set; } = new();
    public AuthErrorDoc InsufficientScope { get; set; } = new();
    public AuthErrorDoc InsufficientTier { get; set; } = new();
}

public class AuthErrorDoc
{
    public int StatusCode { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class EndpointsDocumentation
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public List<EndpointDoc> Endpoints { get; set; } = [];
}

public class EndpointDoc
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RequiredScope { get; set; } = string.Empty;
    public string MinTier { get; set; } = string.Empty;
    public List<ParamDoc>? Parameters { get; set; }
    public string? RequestBody { get; set; }
    public string ExampleRequest { get; set; } = string.Empty;
    public string? ExampleResponse { get; set; }
}

public class ParamDoc
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; }
}

public class ErrorCodesDocumentation
{
    public string Overview { get; set; } = string.Empty;
    public string ResponseFormat { get; set; } = string.Empty;
    public List<StatusCodeDoc> StatusCodes { get; set; } = [];
    public List<ErrorCodeDoc> ErrorCodes { get; set; } = [];
}

public class StatusCodeDoc
{
    public int Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ErrorCodeDoc
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int HttpStatus { get; set; }
}

#endregion
