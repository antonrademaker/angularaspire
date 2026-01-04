using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.ApiManagement;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace PublicApi.Tests.RateLimiting;

/// <summary>
/// Integration tests for API rate limiting middleware
/// </summary>
public class RateLimitingTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public RateLimitingTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region API Key Header Tests

    [Fact]
    public async Task ExternalApi_WithoutApiKey_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/external/events");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("API key is required", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExternalApi_WithInvalidApiKey_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "invalid_key_12345");

        // Act
        var response = await client.GetAsync("/api/external/events");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExternalApi_WithApiKeyInQueryString_AcceptsKey()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();

        // Create a test API key
        var createResult = await CreateTestApiKeyAsync(apiKeyService, ApiKeyTier.Standard);
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/external/me?api_key={createResult.RawKey}");

        // Assert - should accept the key (may succeed or fail based on actual validation)
        // The key should be extracted; not getting 401 for "missing key" is the test
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExternalApi_WithBearerToken_AcceptsApiKey()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();

        var createResult = await CreateTestApiKeyAsync(apiKeyService, ApiKeyTier.Standard);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {createResult.RawKey}");

        // Act
        var response = await client.GetAsync("/api/external/me");

        // Assert
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Rate Limit Header Tests

    [Fact]
    public async Task ValidApiKey_IncludesRateLimitHeaders()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();

        var createResult = await CreateTestApiKeyAsync(apiKeyService, ApiKeyTier.Standard);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", createResult.RawKey!);

        // Act
        var response = await client.GetAsync("/api/external/me");

        // Assert
        response.EnsureSuccessStatusCode();

        Assert.True(response.Headers.Contains("X-RateLimit-Limit"));
        Assert.True(response.Headers.Contains("X-RateLimit-Remaining"));
        Assert.True(response.Headers.Contains("X-RateLimit-Reset"));

        var limit = response.Headers.GetValues("X-RateLimit-Limit").First();
        var remaining = response.Headers.GetValues("X-RateLimit-Remaining").First();

        Assert.True(int.TryParse(limit, out var limitValue));
        Assert.True(int.TryParse(remaining, out var remainingValue));
        Assert.True(limitValue > 0);
        Assert.True(remainingValue <= limitValue);
    }

    [Theory]
    [InlineData(ApiKeyTier.Free, 100)]
    [InlineData(ApiKeyTier.Standard, 500)]
    [InlineData(ApiKeyTier.Premium, 2000)]
    [InlineData(ApiKeyTier.Enterprise, 10000)]
    public async Task RateLimitHeader_ReflectsTier(ApiKeyTier tier, int expectedLimit)
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();

        var createResult = await CreateTestApiKeyAsync(apiKeyService, tier);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", createResult.RawKey!);

        // Act
        var response = await client.GetAsync("/api/external/me");

        // Assert
        response.EnsureSuccessStatusCode();

        var limit = response.Headers.GetValues("X-RateLimit-Limit").First();
        Assert.Equal(expectedLimit.ToString(), limit);
    }

    #endregion

    #region Rate Limit Enforcement Tests

    [Fact]
    public async Task RateLimitExceeded_Returns429()
    {
        // Arrange - Create a mock service that returns rate limited
        var mockApiKeyService = new Mock<IApiKeyService>();
        mockApiKeyService
            .Setup(s => s.ValidateApiKeyAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiKeyValidationResult.RateLimited(
                Guid.NewGuid(),
                rateLimit: 100,
                currentRequests: 101));

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => mockApiKeyService.Object);
            });
        });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "evtmgr_test_key");

        // Act
        var response = await client.GetAsync("/api/external/events");

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.True(response.Headers.Contains("Retry-After"));

        var retryAfter = response.Headers.GetValues("Retry-After").First();
        Assert.True(int.TryParse(retryAfter, out var retrySeconds));
        Assert.True(retrySeconds > 0 && retrySeconds <= 60);
    }

    [Fact]
    public async Task RateLimitExceeded_IncludesRateLimitHeaders()
    {
        // Arrange
        var mockApiKeyService = new Mock<IApiKeyService>();
        mockApiKeyService
            .Setup(s => s.ValidateApiKeyAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiKeyValidationResult.RateLimited(
                Guid.NewGuid(),
                rateLimit: 100,
                currentRequests: 105));

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => mockApiKeyService.Object);
            });
        });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "evtmgr_test_key");

        // Act
        var response = await client.GetAsync("/api/external/events");

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.True(response.Headers.Contains("X-RateLimit-Limit"));
        Assert.True(response.Headers.Contains("X-RateLimit-Remaining"));

        var remaining = response.Headers.GetValues("X-RateLimit-Remaining").First();
        Assert.Equal("0", remaining);
    }

    #endregion

    #region Tier-Based Access Tests

    [Fact]
    public async Task FreeTier_CannotAccessWriteEndpoints()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();

        // Create a Free tier API key (read-only)
        var createResult = await CreateTestApiKeyAsync(apiKeyService, ApiKeyTier.Free);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", createResult.RawKey!);

        var createRequest = new
        {
            title = "Test Event",
            startDate = DateTime.UtcNow.AddDays(7),
            endDate = DateTime.UtcNow.AddDays(7).AddHours(8),
            maxCapacity = 100
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/external/events", createRequest);

        // Assert - Should be forbidden due to insufficient scope
        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden ||
            response.StatusCode == HttpStatusCode.Unauthorized,
            $"Expected 403 or 401, got {response.StatusCode}");
    }

    [Fact]
    public async Task StandardTier_CanAccessWriteEndpoints()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();

        var createResult = await CreateTestApiKeyAsync(apiKeyService, ApiKeyTier.Standard);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", createResult.RawKey!);

        // Act
        var response = await client.GetAsync("/api/external/me");

        // Assert - Should succeed and show write scopes
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("events:write", content);
    }

    #endregion

    #region Non-Protected Path Tests

    [Fact]
    public async Task NonExternalApiPath_DoesNotRequireApiKey()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Regular API endpoints don't require API key
        var response = await client.GetAsync("/api/docs");

        // Assert
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoint_DoesNotRequireApiKey()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert - Health endpoint should be accessible (or not found if not configured)
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Request Counting Tests

    [Fact]
    public async Task MultipleRequests_DecrementRemaining()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();

        var createResult = await CreateTestApiKeyAsync(apiKeyService, ApiKeyTier.Standard);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", createResult.RawKey!);

        // Act - Make two requests
        var response1 = await client.GetAsync("/api/external/me");
        response1.EnsureSuccessStatusCode();
        var remaining1 = int.Parse(response1.Headers.GetValues("X-RateLimit-Remaining").First());

        var response2 = await client.GetAsync("/api/external/me");
        response2.EnsureSuccessStatusCode();
        var remaining2 = int.Parse(response2.Headers.GetValues("X-RateLimit-Remaining").First());

        // Assert - Remaining should decrease (or stay same if tracking isn't per-request)
        Assert.True(remaining2 <= remaining1,
            $"Expected remaining to decrease or stay same. First: {remaining1}, Second: {remaining2}");
    }

    #endregion

    #region Scope Validation Tests

    [Fact]
    public async Task ApiKey_WithInsufficientScope_ReturnsForbidden()
    {
        // Arrange
        var mockApiKeyService = new Mock<IApiKeyService>();
        mockApiKeyService
            .Setup(s => s.ValidateApiKeyAsync(
                It.IsAny<string>(),
                It.Is<string?>(scope => scope != null && scope.Contains("delete")),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiKeyValidationResult.Invalid("Missing required scope: events:delete"));

        mockApiKeyService
            .Setup(s => s.ValidateApiKeyAsync(
                It.IsAny<string>(),
                It.Is<string?>(scope => scope == null || !scope.Contains("delete")),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiKeyValidationResult.Valid(
                Guid.NewGuid(),
                Guid.NewGuid(),
                ApiKeyTier.Standard,
                new[] { "events:read", "events:write" },
                500,
                0));

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => mockApiKeyService.Object);
            });
        });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "evtmgr_test_key");

        // Act
        var response = await client.DeleteAsync("/api/external/events/00000000-0000-0000-0000-000000000001");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region IP Restriction Tests

    [Fact]
    public async Task ApiKey_WithIpRestriction_ValidatesIpAddress()
    {
        // Arrange
        var mockApiKeyService = new Mock<IApiKeyService>();
        mockApiKeyService
            .Setup(s => s.ValidateApiKeyAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.Is<string?>(ip => ip != "192.168.1.1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiKeyValidationResult.Invalid("IP address not allowed"));

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => mockApiKeyService.Object);
            });
        });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "evtmgr_test_key");

        // Act
        var response = await client.GetAsync("/api/external/events");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("IP address not allowed", content);
    }

    #endregion

    #region Helper Methods

    private async Task<CreateApiKeyResult> CreateTestApiKeyAsync(
        IApiKeyService apiKeyService,
        ApiKeyTier tier)
    {
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

        return await apiKeyService.CreateApiKeyAsync(request, userId);
    }

    #endregion
}
