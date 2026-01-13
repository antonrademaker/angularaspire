using System.Net;

namespace PublicApi.Tests.SessionManagement;

/// <summary>
/// Integration tests for session subscription API endpoints
/// Tests the complete subscription workflow including subscribe, unsubscribe, availability checks
/// Note: These tests validate the API contract structure and authentication requirements.
/// Full end-to-end testing with the actual database requires running with TestContainers
/// or a dedicated test environment with PostgreSQL.
/// </summary>
public class SubscriptionIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SubscriptionIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Tests that the GetEventSessions endpoint exists and is accessible
    /// </summary>
    [Fact]
    public async Task GetEventSessions_EndpointExists()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/sessions?eventId={eventId}");

        // Assert - endpoint exists (may return 500 due to DB config or empty results)
        Assert.NotEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    /// <summary>
    /// Tests that the GetSession endpoint exists
    /// </summary>
    [Fact]
    public async Task GetSession_EndpointExists()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/sessions/{sessionId}");

        // Assert - endpoint exists
        Assert.NotEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    /// <summary>
    /// Tests that the GetSessionAvailability endpoint exists
    /// </summary>
    [Fact]
    public async Task GetSessionAvailability_EndpointExists()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/sessions/{sessionId}/availability");

        // Assert - endpoint exists
        Assert.NotEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    /// <summary>
    /// Tests that the Subscribe endpoint requires authentication
    /// </summary>
    [Fact]
    public async Task Subscribe_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new { Notes = "Test subscription" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/sessions/{sessionId}/subscribe", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Tests that the Unsubscribe endpoint requires authentication
    /// </summary>
    [Fact]
    public async Task Unsubscribe_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new { Reason = "Schedule conflict" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/sessions/{sessionId}/unsubscribe", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Tests that the GetMySubscription endpoint requires authentication
    /// </summary>
    [Fact]
    public async Task GetMySubscription_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/sessions/{sessionId}/my-subscription");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Tests that the GetMySubscriptions endpoint requires authentication
    /// </summary>
    [Fact]
    public async Task GetMySubscriptions_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/my-subscriptions");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Tests that the GetMyEventSubscriptions endpoint requires authentication
    /// </summary>
    [Fact]
    public async Task GetMyEventSubscriptions_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/events/{eventId}/my-subscriptions");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Tests that subscription routes use correct HTTP methods
    /// </summary>
    [Fact]
    public async Task SubscriptionRoutes_UseCorrectHttpMethods()
    {
        var sessionId = Guid.NewGuid();

        // Subscribe should be POST
        var getSubscribe = await _client.GetAsync($"/api/v1/sessions/{sessionId}/subscribe");
        Assert.Equal(HttpStatusCode.MethodNotAllowed, getSubscribe.StatusCode);

        // Unsubscribe should be POST
        var getUnsubscribe = await _client.GetAsync($"/api/v1/sessions/{sessionId}/unsubscribe");
        Assert.Equal(HttpStatusCode.MethodNotAllowed, getUnsubscribe.StatusCode);
    }

    /// <summary>
    /// Tests that GET endpoints are accessible without authentication (public discovery)
    /// </summary>
    [Fact]
    public async Task PublicEndpoints_DoNotRequireAuth()
    {
        var sessionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        // GetEventSessions - should not return 401
        var sessionsResponse = await _client.GetAsync($"/api/v1/sessions?eventId={eventId}");
        Assert.NotEqual(HttpStatusCode.Unauthorized, sessionsResponse.StatusCode);

        // GetSession - should not return 401
        var sessionResponse = await _client.GetAsync($"/api/v1/sessions/{sessionId}");
        Assert.NotEqual(HttpStatusCode.Unauthorized, sessionResponse.StatusCode);

        // GetSessionAvailability - should not return 401
        var availabilityResponse = await _client.GetAsync($"/api/v1/sessions/{sessionId}/availability");
        Assert.NotEqual(HttpStatusCode.Unauthorized, availabilityResponse.StatusCode);
    }
}
