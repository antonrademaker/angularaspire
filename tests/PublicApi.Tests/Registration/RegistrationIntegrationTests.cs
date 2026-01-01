using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shared.EventManagement;
using Shared.Registration;
using Shared.UserManagement;
using System.Net.Http.Json;
using Xunit;

namespace PublicApi.Tests.Registration;

public class RegistrationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public RegistrationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RegisterForEvent_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        // Create a test user
        var user = new User
        {
            Email = "test@example.com",
            FullName = "Test User"
        };
        var createdUser = await userService.CreateUserAsync(user);

        // Create a test event
        var createEventRequest = new CreateEventRequest
        {
            Title = "Integration Test Event",
            Description = "Event for integration testing",
            Slug = "integration-test-event",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(31),
            MaxAttendees = 100,
            RegistrationOpenDate = DateTime.UtcNow.AddDays(-1),
            RegistrationCloseDate = DateTime.UtcNow.AddDays(29),
            Visibility = EventVisibility.Public
        };

        var createdEvent = await eventService.CreateEventAsync(createEventRequest, createdUser.Id);

        var registrationRequest = new RegistrationRequest
        {
            EventId = createdEvent.Id,
            UserId = createdUser.Id,
            Priority = RegistrationPriority.Normal,
            RegistrationData = new Dictionary<string, object> { { "dietary", "vegetarian" } }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/registration", registrationRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<RegistrationResult>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Registration);
    }

    [Fact]
    public async Task GetRegistration_WithValidId_ShouldReturnRegistration()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var registrationService = scope.ServiceProvider.GetRequiredService<IRegistrationService>();

        // Create test user and event
        var user = new User
        {
            Email = "gettest@example.com",
            FullName = "Get Test User"
        };
        var createdUser = await userService.CreateUserAsync(user);

        var createEventRequest = new CreateEventRequest
        {
            Title = "Get Registration Test Event",
            Description = "Event for get registration testing",
            Slug = "get-registration-test-event",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(31),
            MaxAttendees = 100,
            Visibility = EventVisibility.Public
        };

        var createdEvent = await eventService.CreateEventAsync(createEventRequest, createdUser.Id);

        // Create registration
        var registrationRequest = new RegistrationRequest
        {
            EventId = createdEvent.Id,
            UserId = createdUser.Id,
            Priority = RegistrationPriority.Normal
        };

        var registrationResult = await registrationService.RegisterUserAsync(registrationRequest);

        // Act
        var response = await _client.GetAsync($"/api/registration/{registrationResult.Registration!.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var registration = await response.Content.ReadFromJsonAsync<Shared.Registration.Registration>();
        Assert.NotNull(registration);
        Assert.Equal(registrationResult.Registration.Id, registration.Id);
        Assert.Equal(createdUser.Id, registration.UserId);
        Assert.Equal(createdEvent.Id, registration.EventId);
    }

    [Fact]
    public async Task CancelRegistration_WithValidRequest_ShouldCancelSuccessfully()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var registrationService = scope.ServiceProvider.GetRequiredService<IRegistrationService>();

        // Create test user and event
        var user = new User
        {
            Email = "cancel@example.com",
            FullName = "Cancel User"
        };
        var createdUser = await userService.CreateUserAsync(user);

        var createEventRequest = new CreateEventRequest
        {
            Title = "Cancellation Test Event",
            Description = "Event for cancellation testing",
            Slug = "cancellation-test-event",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(31),
            MaxAttendees = 100,
            Visibility = EventVisibility.Public
        };

        var createdEvent = await eventService.CreateEventAsync(createEventRequest, createdUser.Id);

        // Create registration
        var registrationRequest = new RegistrationRequest
        {
            EventId = createdEvent.Id,
            UserId = createdUser.Id,
            Priority = RegistrationPriority.Normal
        };

        var registrationResult = await registrationService.RegisterUserAsync(registrationRequest);

        // Act
        var response = await _client.DeleteAsync($"/api/registration/{registrationResult.Registration!.Id}?userId={createdUser.Id}");

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify cancellation
        var cancelledRegistration = await registrationService.GetRegistrationAsync(registrationResult.Registration.Id);
        Assert.NotNull(cancelledRegistration);
        Assert.Equal(RegistrationStatus.Cancelled, cancelledRegistration.Status);
    }
}