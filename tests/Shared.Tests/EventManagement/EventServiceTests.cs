using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Common;
using Shared.EventManagement;
using Xunit;

namespace Shared.Tests.EventManagement;

public class EventServiceTests : IDisposable
{
    private readonly EventDbContext _context;
    private readonly Mock<ILogger<EventService>> _mockLogger;
    private readonly EventService _eventService;

    public EventServiceTests()
    {
        var options = new DbContextOptionsBuilder<EventDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EventDbContext(options);
        _mockLogger = new Mock<ILogger<EventService>>();

        // Configure DatabaseOptions to indicate InMemory mode
        var databaseOptions = Options.Create(new DatabaseOptions { UseInMemoryDatabase = true });
        _eventService = new EventService(_context, _mockLogger.Object, databaseOptions);
    }

    [Fact]
    public async Task CreateEventAsync_WithValidData_ShouldCreateEvent()
    {
        // Arrange
        var createRequest = new CreateEventRequest
        {
            Title = "Test Conference",
            Description = "A test conference description",
            Slug = "test-conference-2024",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(31)
        };

        var userId = Guid.NewGuid();

        // Act
        var result = await _eventService.CreateEventAsync(createRequest, userId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Test Conference", result.Title);
        Assert.Equal("test-conference-2024", result.Slug);
        Assert.Equal(userId, result.CreatedBy);
    }

    [Fact]
    public async Task GetEventByIdAsync_WithExistingEvent_ShouldReturnEvent()
    {
        // Arrange
        var createRequest = new CreateEventRequest
        {
            Title = "Get Event Test",
            Description = "Test event for retrieval",
            Slug = "get-event-test",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(31)
        };

        var userId = Guid.NewGuid();
        var createdEvent = await _eventService.CreateEventAsync(createRequest, userId);

        // Act
        var result = await _eventService.GetEventByIdAsync(createdEvent.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdEvent.Id, result.Id);
        Assert.Equal("Get Event Test", result.Title);
        Assert.Equal("get-event-test", result.Slug);
    }

    [Fact]
    public async Task GetEventBySlugAsync_WithExistingSlug_ShouldReturnEvent()
    {
        // Arrange
        var createRequest = new CreateEventRequest
        {
            Title = "Slug Event Test",
            Description = "Test event for slug retrieval",
            Slug = "slug-event-test",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(31)
        };

        var userId = Guid.NewGuid();
        await _eventService.CreateEventAsync(createRequest, userId);

        // Act
        var result = await _eventService.GetEventBySlugAsync("slug-event-test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Slug Event Test", result.Title);
        Assert.Equal("slug-event-test", result.Slug);
    }

    [Fact]
    public async Task UpdateEventAsync_WithValidData_ShouldUpdateEvent()
    {
        // Arrange
        var createRequest = new CreateEventRequest
        {
            Title = "Original Event",
            Description = "Original description",
            Slug = "original-event",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(31)
        };

        var userId = Guid.NewGuid();
        var createdEvent = await _eventService.CreateEventAsync(createRequest, userId);

        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Event",
            Description = "Updated description"
        };

        // Act
        var result = await _eventService.UpdateEventAsync(createdEvent.Id, updateRequest, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Event", result.Title);
        Assert.Equal("Updated description", result.Description);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task DeleteEventAsync_WithExistingEvent_ShouldDeleteEvent()
    {
        // Arrange
        var createRequest = new CreateEventRequest
        {
            Title = "Event to Delete",
            Description = "This event will be deleted",
            Slug = "event-to-delete",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(31)
        };

        var userId = Guid.NewGuid();
        var createdEvent = await _eventService.CreateEventAsync(createRequest, userId);

        // Act
        var result = await _eventService.DeleteEventAsync(createdEvent.Id, userId);

        // Assert
        Assert.True(result);

        var deletedEvent = await _eventService.GetEventByIdAsync(createdEvent.Id);
        Assert.Null(deletedEvent);
    }

    [Fact]
    public async Task SearchEventsAsync_WithSearchText_ShouldReturnMatchingEvents()
    {
        // Arrange
        var events = new[]
        {
            new CreateEventRequest
            {
                Title = "Angular Conference 2024",
                Description = "Angular development conference",
                Slug = "angular-conf-2024",
                StartDate = DateTime.UtcNow.AddDays(30),
                EndDate = DateTime.UtcNow.AddDays(31)
            },
            new CreateEventRequest
            {
                Title = "React Summit 2024",
                Description = "React development summit",
                Slug = "react-summit-2024",
                StartDate = DateTime.UtcNow.AddDays(40),
                EndDate = DateTime.UtcNow.AddDays(41)
            }
        };

        var userId = Guid.NewGuid();
        foreach (var eventRequest in events)
        {
            await _eventService.CreateEventAsync(eventRequest, userId);
        }

        var searchRequest = new EventSearchRequest
        {
            SearchText = "Angular",
            PageSize = 10,
            Page = 1
        };

        // Act
        var result = await _eventService.SearchEventsAsync(searchRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Events);
        Assert.Equal("Angular Conference 2024", result.Events.First().Title);
    }

    /*
    [Fact]
    public async Task IsRegistrationAvailableAsync_WithAvailableCapacity_ShouldReturnTrue()
    {
        // Arrange
        var createRequest = new CreateEventRequest
        {
            Title = "Registration Available Event",
            Description = "Event with available registration",
            Slug = "registration-available",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(31),
            MaxAttendees = 100,
            RegistrationOpenDate = DateTime.UtcNow.AddDays(-1),
            RegistrationCloseDate = DateTime.UtcNow.AddDays(29),
            Visibility = EventVisibility.Public
        };

        var userId = Guid.NewGuid();
        var createdEvent = await _eventService.CreateEventAsync(createRequest, userId);

        // Publish the event (required for registration to be available)
        await _eventService.ChangeEventStatusAsync(createdEvent.Id, EventStatus.Published, userId);

        // Act
        var result = await _eventService.IsRegistrationAvailableAsync(createdEvent.Id);

        // Assert
        Assert.True(result);
    }
    */

    public void Dispose()
    {
        _context?.Dispose();
    }
}