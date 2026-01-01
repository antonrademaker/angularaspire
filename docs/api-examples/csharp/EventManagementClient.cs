using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventManagement.Client;

/// <summary>
/// Client for the Event Management External API
/// </summary>
public interface IEventManagementClient : IDisposable
{
    Task<ApiResponse<PagedResult<EventDto>>> ListEventsAsync(
        int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default);
    Task<ApiResponse<EventDto>> GetEventAsync(Guid eventId, CancellationToken ct = default);
    Task<ApiResponse<EventDto>> CreateEventAsync(CreateEventRequest request, CancellationToken ct = default);
    Task<ApiResponse<EventDto>> UpdateEventAsync(Guid eventId, UpdateEventRequest request, CancellationToken ct = default);
    Task<ApiResponse<object>> DeleteEventAsync(Guid eventId, CancellationToken ct = default);
    Task<ApiResponse<List<SocialEventDto>>> ListSocialEventsAsync(Guid eventId, CancellationToken ct = default);
    Task<ApiResponse<ApiKeyInfoDto>> GetApiKeyInfoAsync(CancellationToken ct = default);
    RateLimitInfo GetRateLimitInfo();
    bool IsNearRateLimit(int threshold = 10);
}

public class EventManagementClient : IEventManagementClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly JsonSerializerOptions _jsonOptions;
    private RateLimitInfo _rateLimitInfo = new();

    public EventManagementClient(string baseUrl, string apiKey)
        : this(new HttpClient { BaseAddress = new Uri(baseUrl) }, apiKey)
    {
    }

    public EventManagementClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }

    public async Task<ApiResponse<PagedResult<EventDto>>> ListEventsAsync(
        int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default)
    {
        var query = $"api/external/events?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(status))
            query += $"&status={Uri.EscapeDataString(status)}";
        
        return await SendAsync<PagedResult<EventDto>>(HttpMethod.Get, query, ct: ct);
    }

    public async Task<ApiResponse<EventDto>> GetEventAsync(Guid eventId, CancellationToken ct = default)
    {
        return await SendAsync<EventDto>(HttpMethod.Get, $"api/external/events/{eventId}", ct: ct);
    }

    public async Task<ApiResponse<EventDto>> CreateEventAsync(CreateEventRequest request, CancellationToken ct = default)
    {
        return await SendAsync<EventDto>(HttpMethod.Post, "api/external/events", request, ct);
    }

    public async Task<ApiResponse<EventDto>> UpdateEventAsync(Guid eventId, UpdateEventRequest request, CancellationToken ct = default)
    {
        return await SendAsync<EventDto>(HttpMethod.Put, $"api/external/events/{eventId}", request, ct);
    }

    public async Task<ApiResponse<object>> DeleteEventAsync(Guid eventId, CancellationToken ct = default)
    {
        return await SendAsync<object>(HttpMethod.Delete, $"api/external/events/{eventId}", ct: ct);
    }

    public async Task<ApiResponse<List<SocialEventDto>>> ListSocialEventsAsync(Guid eventId, CancellationToken ct = default)
    {
        return await SendAsync<List<SocialEventDto>>(HttpMethod.Get, $"api/external/events/{eventId}/social-events", ct: ct);
    }

    public async Task<ApiResponse<ApiKeyInfoDto>> GetApiKeyInfoAsync(CancellationToken ct = default)
    {
        return await SendAsync<ApiKeyInfoDto>(HttpMethod.Get, "api/external/me", ct: ct);
    }

    public RateLimitInfo GetRateLimitInfo() => _rateLimitInfo;

    public bool IsNearRateLimit(int threshold = 10) => 
        _rateLimitInfo.Remaining.HasValue && _rateLimitInfo.Remaining.Value <= threshold;

    private async Task<ApiResponse<T>> SendAsync<T>(
        HttpMethod method, string path, object? content = null, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(method, path);
        
        if (content != null)
        {
            request.Content = JsonContent.Create(content, options: _jsonOptions);
        }

        using var response = await _httpClient.SendAsync(request, ct);
        
        UpdateRateLimitInfo(response);

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? 60;
            throw new RateLimitException("Rate limit exceeded", (int)retryAfter, _rateLimitInfo);
        }

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(_jsonOptions, ct);
        
        if (apiResponse == null)
        {
            throw new ApiException("PARSE_ERROR", "Failed to parse API response", (int)response.StatusCode);
        }

        if (!apiResponse.Success && apiResponse.Error != null)
        {
            throw new ApiException(
                apiResponse.Error.Code ?? "UNKNOWN_ERROR",
                apiResponse.Error.Message ?? "An unknown error occurred",
                (int)response.StatusCode);
        }

        return apiResponse;
    }

    private void UpdateRateLimitInfo(HttpResponseMessage response)
    {
        _rateLimitInfo = new RateLimitInfo();

        if (response.Headers.TryGetValues("X-RateLimit-Limit", out var limitValues) &&
            int.TryParse(limitValues.FirstOrDefault(), out var limit))
        {
            _rateLimitInfo.Limit = limit;
        }

        if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues) &&
            int.TryParse(remainingValues.FirstOrDefault(), out var remaining))
        {
            _rateLimitInfo.Remaining = remaining;
        }

        if (response.Headers.TryGetValues("X-RateLimit-Reset", out var resetValues) &&
            long.TryParse(resetValues.FirstOrDefault(), out var reset))
        {
            _rateLimitInfo.Reset = DateTimeOffset.FromUnixTimeSeconds(reset).UtcDateTime;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

#region DTOs

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public ApiError? Error { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ApiError
{
    public string? Code { get; set; }
    public string? Message { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}

public class EventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public bool IsVirtual { get; set; }
    public string? MeetingUrl { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int MaxCapacity { get; set; }
    public int CurrentRegistrations { get; set; }
    public Guid OrganizerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class SocialEventDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int MaxCapacity { get; set; }
    public int CurrentRsvps { get; set; }
    public bool RequiresRegistration { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ApiKeyInfoDto
{
    public Guid ApiKeyId { get; set; }
    public string Tier { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
    public int RateLimit { get; set; }
    public int CurrentUsage { get; set; }
    public int RemainingRequests { get; set; }
}

public class CreateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public bool IsVirtual { get; set; }
    public string? MeetingUrl { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MaxCapacity { get; set; }
}

public class UpdateEventRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public bool? IsVirtual { get; set; }
    public string? MeetingUrl { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? MaxCapacity { get; set; }
}

public class RateLimitInfo
{
    public int? Limit { get; set; }
    public int? Remaining { get; set; }
    public DateTime? Reset { get; set; }
}

#endregion

#region Exceptions

public class ApiException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }

    public ApiException(string code, string message, int statusCode) : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}

public class RateLimitException : ApiException
{
    public int RetryAfter { get; }
    public RateLimitInfo RateLimitInfo { get; }

    public RateLimitException(string message, int retryAfter, RateLimitInfo rateLimitInfo) 
        : base("RATE_LIMIT_EXCEEDED", message, 429)
    {
        RetryAfter = retryAfter;
        RateLimitInfo = rateLimitInfo;
    }
}

#endregion
