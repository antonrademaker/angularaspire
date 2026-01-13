using System.Net;
using System.Text.Json;
using Shared.ApiManagement;
using Shared.EventManagement;
using Shared.UserManagement;

namespace PublicApi.Tests.ApiIntegration;

/// <summary>
/// Integration tests for External API endpoints
/// </summary>
public class ExternalApiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ExternalApiTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region Events Endpoint Tests

    [Fact]
    public async Task ListEvents_ReturnsPagedResults()
    {
        // Arrange
        var client = await CreateAuthenticatedClient(ApiKeyTier.Standard);

        // Act
        var response = await client.GetAsync("/api/external/events?page=1&pageSize=10");

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ExternalApiResponse<ExternalPagedResult>>(content, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.CurrentPage >= 1);
        Assert.True(result.Data.PageSize > 0);
    }

    [Fact]
    public async Task ListEvents_WithStatusFilter_ReturnsFilteredResults()
    {
        // Arrange
        var client = await CreateAuthenticatedClient(ApiKeyTier.Standard);

        // Act
        var response = await client.GetAsync("/api/external/events?status=Published");

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("success", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetEvent_WithValidId_ReturnsEvent()
    {
        // Arrange
        var (client, eventId) = await CreateAuthenticatedClientWithTestEvent();

        // Act
        var response = await client.GetAsync($"/api/external/events/{eventId}");

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("success", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(eventId.ToString(), content);
    }

    [Fact]
    public async Task GetEvent_WithInvalidId_Returns404()
    {
        // Arrange
        var client = await CreateAuthenticatedClient(ApiKeyTier.Standard);
        var invalidId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/external/events/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithValidData_ReturnsCreatedEvent()
    {
        // Arrange
        var client = await CreateAuthenticatedClient(ApiKeyTier.Standard);
        var createRequest = new
        {
            title = $"External API Test Event {Guid.NewGuid():N}",
            description = "Event created via External API integration test",
            startDate = DateTime.UtcNow.AddDays(30),
            endDate = DateTime.UtcNow.AddDays(30).AddHours(8),
            location = "Test Venue",
            maxCapacity = 100,
            isVirtual = false
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/external/events", createRequest);

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("success", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(createRequest.title, content);
    }

    [Fact]
    public async Task CreateEvent_WithInvalidData_Returns400()
    {
        // Arrange
        var client = await CreateAuthenticatedClient(ApiKeyTier.Standard);
        var invalidRequest = new
        {
            // Missing required title
            startDate = DateTime.UtcNow.AddDays(30),
            endDate = DateTime.UtcNow.AddDays(29) // End before start
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/external/events", invalidRequest);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 400 or 422, got {response.StatusCode}");
    }

    [Fact]
    public async Task UpdateEvent_WithValidData_ReturnsUpdatedEvent()
    {
        // Arrange
        var (client, eventId) = await CreateAuthenticatedClientWithTestEvent();
        var updateRequest = new
        {
            title = "Updated External API Test Event",
            maxCapacity = 200
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/external/events/{eventId}", updateRequest);

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Updated External API Test Event", content);
    }

    [Fact]
    public async Task DeleteEvent_WithValidId_ReturnsSuccess()
    {
        // Arrange - Need Premium tier for delete
        var (client, eventId) = await CreateAuthenticatedClientWithTestEvent(ApiKeyTier.Premium);

        // Act
        var response = await client.DeleteAsync($"/api/external/events/{eventId}");

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify event is deleted
        var getResponse = await client.GetAsync($"/api/external/events/{eventId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WithFreeTier_Returns401()
    {
        // Arrange - Free tier cannot delete
        var (client, eventId) = await CreateAuthenticatedClientWithTestEvent(ApiKeyTier.Free, skipDelete: true);

        // Act
        var response = await client.DeleteAsync($"/api/external/events/{eventId}");

        // Assert - Should fail due to insufficient scope
        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden ||
            response.StatusCode == HttpStatusCode.Unauthorized,
            $"Expected 403 or 401, got {response.StatusCode}");
    }

    #endregion

    #region Social Events Endpoint Tests

    [Fact]
    public async Task ListSocialEvents_ReturnsResults()
    {
        // Arrange
        var (client, eventId) = await CreateAuthenticatedClientWithTestEvent();

        // Act
        var response = await client.GetAsync($"/api/external/events/{eventId}/social-events");

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("success", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListSocialEvents_WithInvalidEventId_Returns404()
    {
        // Arrange
        var client = await CreateAuthenticatedClient(ApiKeyTier.Standard);
        var invalidEventId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/external/events/{invalidEventId}/social-events");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region API Key Info Endpoint Tests

    [Fact]
    public async Task GetMe_ReturnsApiKeyInfo()
    {
        // Arrange
        var client = await CreateAuthenticatedClient(ApiKeyTier.Standard);

        // Act
        var response = await client.GetAsync("/api/external/me");

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("apiKeyId", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tier", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scopes", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rateLimit", content, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(ApiKeyTier.Free, "Free", 100)]
    [InlineData(ApiKeyTier.Standard, "Standard", 500)]
    [InlineData(ApiKeyTier.Premium, "Premium", 2000)]
    [InlineData(ApiKeyTier.Enterprise, "Enterprise", 10000)]
    public async Task GetMe_ReturnsCorrectTierInfo(ApiKeyTier tier, string tierName, int expectedRateLimit)
    {
        // Arrange
        var client = await CreateAuthenticatedClient(tier);

        // Act
        var response = await client.GetAsync("/api/external/me");

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains(tierName, content);
        Assert.Contains(expectedRateLimit.ToString(), content);
    }

    #endregion

    #region Response Format Tests

    [Fact]
    public async Task AllResponses_IncludeTimestamp()
    {
        // Arrange
        var client = await CreateAuthenticatedClient(ApiKeyTier.Standard);

        // Act
        var response = await client.GetAsync("/api/external/me");

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("timestamp", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ErrorResponses_IncludeErrorDetails()
    {
        // Arrange
        var client = await CreateAuthenticatedClient(ApiKeyTier.Standard);

        // Act
        var response = await client.GetAsync($"/api/external/events/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("success", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("false", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("error", content, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Pagination Tests

    [Fact]
    public async Task ListEvents_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        var client = await CreateAuthenticatedClient(ApiKeyTier.Standard);

        // Act
        var page1 = await client.GetAsync("/api/external/events?page=1&pageSize=5");
        var page2 = await client.GetAsync("/api/external/events?page=2&pageSize=5");

        // Assert
        page1.EnsureSuccessStatusCode();
        page2.EnsureSuccessStatusCode();

        var content1 = await page1.Content.ReadAsStringAsync();
        var content2 = await page2.Content.ReadAsStringAsync();

        Assert.Contains("\"currentPage\":1", content1);
        Assert.Contains("\"currentPage\":2", content2);
    }

    [Fact]
    public async Task ListEvents_WithLargePageSize_LimitsResults()
    {
        // Arrange
        var client = await CreateAuthenticatedClient(ApiKeyTier.Standard);

        // Act
        var response = await client.GetAsync("/api/external/events?pageSize=1000");

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        // Should limit page size to a reasonable maximum (typically 100)
        var result = JsonSerializer.Deserialize<ExternalApiResponse<ExternalPagedResult>>(content, _jsonOptions);
        Assert.True(result?.Data?.PageSize <= 100);
    }

    #endregion

    #region Content Negotiation Tests

    [Fact]
    public async Task Request_WithJsonAcceptHeader_ReturnsJson()
    {
        // Arrange
        var client = await CreateAuthenticatedClient(ApiKeyTier.Standard);
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        // Act
        var response = await client.GetAsync("/api/external/me");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    #endregion

    #region Helper Methods

    private async Task<HttpClient> CreateAuthenticatedClient(ApiKeyTier tier = ApiKeyTier.Standard)
    {
        using var scope = _factory.Services.CreateScope();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();

        var userId = Guid.NewGuid();
        var scopes = tier switch
        {
            ApiKeyTier.Free => "events:read,social_events:read",
            ApiKeyTier.Standard => "events:read,events:write,social_events:read,social_events:write",
            ApiKeyTier.Premium => "events:read,events:write,events:delete,social_events:read,social_events:write",
            ApiKeyTier.Enterprise => "events:read,events:write,events:delete,social_events:read,social_events:write,registrations:read,registrations:write",
            _ => "events:read"
        };

        var request = new CreateApiKeyRequest
        {
            Name = $"Test API Key - {tier}",
            Description = $"Integration test key for {tier} tier",
            Tier = tier,
            Scopes = scopes,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        var result = await apiKeyService.CreateApiKeyAsync(request, userId);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", result.RawKey!);

        return client;
    }

    private async Task<(HttpClient client, Guid eventId)> CreateAuthenticatedClientWithTestEvent(
        ApiKeyTier tier = ApiKeyTier.Standard,
        bool skipDelete = false)
    {
        using var scope = _factory.Services.CreateScope();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        // Create test user
        var user = new User
        {
            Email = $"test-{Guid.NewGuid():N}@example.com",
            FullName = "Test User"
        };
        var createdUser = await userService.CreateUserAsync(user);

        // Create test event
        var createEventRequest = new CreateEventRequest
        {
            Title = $"External API Test Event {Guid.NewGuid():N}",
            Description = "Event for external API testing",
            Slug = $"ext-api-test-{Guid.NewGuid():N}",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(31)
        };

        var createdEvent = await eventService.CreateEventAsync(createEventRequest, createdUser.Id);

        // Create API key
        var scopes = tier switch
        {
            ApiKeyTier.Free => "events:read,social_events:read",
            ApiKeyTier.Standard => "events:read,events:write,social_events:read,social_events:write",
            ApiKeyTier.Premium => "events:read,events:write,events:delete,social_events:read,social_events:write",
            ApiKeyTier.Enterprise => "events:read,events:write,events:delete,social_events:read,social_events:write,registrations:read,registrations:write",
            _ => "events:read"
        };

        var apiKeyRequest = new CreateApiKeyRequest
        {
            Name = $"Test API Key - {tier}",
            Description = $"Integration test key for {tier} tier",
            Tier = tier,
            Scopes = scopes,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        var result = await apiKeyService.CreateApiKeyAsync(apiKeyRequest, createdUser.Id);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", result.RawKey!);

        return (client, createdEvent.Id);
    }

    #endregion

    #region Response DTOs

    private record ExternalApiResponse<T>
    {
        public bool Success { get; init; }
        public T? Data { get; init; }
        public ExternalApiError? Error { get; init; }
        public DateTime Timestamp { get; init; }
    }

    private record ExternalApiError
    {
        public string? Code { get; init; }
        public string? Message { get; init; }
    }

    private record ExternalPagedResult
    {
        public int TotalCount { get; init; }
        public int CurrentPage { get; init; }
        public int TotalPages { get; init; }
        public int PageSize { get; init; }
        public bool HasPrevious { get; init; }
        public bool HasNext { get; init; }
    }

    #endregion
}
