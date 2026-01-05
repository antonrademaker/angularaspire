using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.EventManagement;
using Shared.EventManagement.Entities;
using Xunit;

namespace Shared.Tests.EventManagement;

public class SocialEventServiceTests : IDisposable
{
    private readonly EventDbContext _context;
    private readonly Mock<ILogger<SocialEventService>> _mockLogger;
    private readonly SocialEventService _service;
    private readonly Guid _testEventId;
    private readonly Guid _testUserId;

    public SocialEventServiceTests()
    {
        var options = new DbContextOptionsBuilder<EventDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EventDbContext(options);
        _mockLogger = new Mock<ILogger<SocialEventService>>();
        _service = new SocialEventService(_context, _mockLogger.Object);
        _testEventId = Guid.NewGuid();
        _testUserId = Guid.NewGuid();

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add a test event
        var testEvent = new Event
        {
            Id = _testEventId,
            Title = "Test Conference",
            Description = "A test conference for social events",
            Slug = "test-conference",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(32),
            CreatedBy = _testUserId
        };

        _context.Events.Add(testEvent);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    // Helper method to create test social events
    private async Task<SocialEventResponse> CreateTestSocialEventAsync(
        string title,
        string slug,
        SocialEventType type = SocialEventType.Networking,
        int? maxCapacity = null,
        bool publish = true)
    {
        var request = new CreateSocialEventRequest
        {
            EventId = _testEventId,
            Title = title,
            Slug = slug,
            Description = $"Test {type} event",
            Type = type,
            StartTime = DateTime.UtcNow.AddDays(30).AddHours(18),
            EndTime = DateTime.UtcNow.AddDays(30).AddHours(21),
            Location = "Main Hall",
            MaxCapacity = maxCapacity,
            RsvpRequired = maxCapacity.HasValue,
            GuestsAllowed = true,
            MaxGuestsPerAttendee = 2
        };

        var result = await _service.CreateSocialEventAsync(request, _testUserId);

        if (publish)
        {
            await _service.PublishSocialEventAsync(result.Id);
            return (await _service.GetSocialEventByIdAsync(result.Id))!;
        }

        return result;
    }

    #region Social Event CRUD Tests

    [Fact]
    public async Task CreateSocialEventAsync_WithValidData_ShouldCreateSocialEvent()
    {
        // Arrange
        var request = new CreateSocialEventRequest
        {
            EventId = _testEventId,
            Title = "Welcome Reception",
            Slug = "welcome-reception",
            Description = "Join us for a welcome cocktail reception",
            Type = SocialEventType.CocktailReception,
            StartTime = DateTime.UtcNow.AddDays(30).AddHours(18),
            EndTime = DateTime.UtcNow.AddDays(30).AddHours(20),
            Location = "Grand Ballroom",
            Room = "Lobby Area",
            MaxCapacity = 100,
            RsvpRequired = true,
            GuestsAllowed = true,
            MaxGuestsPerAttendee = 1,
            DressCode = "Business Casual",
            CostPerPerson = 0,
            Currency = "USD"
        };

        // Act
        var result = await _service.CreateSocialEventAsync(request, _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Welcome Reception", result.Title);
        Assert.Equal("welcome-reception", result.Slug);
        Assert.Equal(SocialEventType.CocktailReception, result.Type);
        Assert.Equal(100, result.MaxCapacity);
        Assert.True(result.RsvpRequired);
        Assert.True(result.GuestsAllowed);
        Assert.Equal(1, result.MaxGuestsPerAttendee);
        Assert.Equal("Business Casual", result.DressCode);
        Assert.Equal(SocialEventStatus.Draft, result.Status);
    }

    [Fact]
    public async Task CreateSocialEventAsync_WithInvalidEventId_ShouldThrowException()
    {
        // Arrange
        var request = new CreateSocialEventRequest
        {
            EventId = Guid.NewGuid(), // Non-existent event
            Title = "Test Social Event",
            Slug = "test-social-event",
            Description = "Test description",
            Type = SocialEventType.Networking,
            StartTime = DateTime.UtcNow.AddDays(30).AddHours(18),
            EndTime = DateTime.UtcNow.AddDays(30).AddHours(20),
            Location = "Test Location"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateSocialEventAsync(request, _testUserId));
    }

    [Fact]
    public async Task GetSocialEventByIdAsync_WithExistingEvent_ShouldReturnEvent()
    {
        // Arrange
        var created = await CreateTestSocialEventAsync("Dinner Event", "dinner-event", SocialEventType.Dinner);

        // Act
        var result = await _service.GetSocialEventByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Dinner Event", result.Title);
        Assert.Equal(SocialEventType.Dinner, result.Type);
    }

    [Fact]
    public async Task GetSocialEventByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetSocialEventByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSocialEventBySlugAsync_WithExistingSlug_ShouldReturnEvent()
    {
        // Arrange
        await CreateTestSocialEventAsync("Lunch Event", "lunch-event", SocialEventType.Lunch);

        // Act
        var result = await _service.GetSocialEventBySlugAsync(_testEventId, "lunch-event");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Lunch Event", result.Title);
        Assert.Equal("lunch-event", result.Slug);
    }

    [Fact]
    public async Task GetSocialEventsForEventAsync_ShouldReturnOnlyPublishedByDefault()
    {
        // Arrange
        await CreateTestSocialEventAsync("Published Event", "published-event", publish: true);
        await CreateTestSocialEventAsync("Draft Event", "draft-event", publish: false);

        // Act
        var result = await _service.GetSocialEventsForEventAsync(_testEventId, publishedOnly: true);

        // Assert
        Assert.Single(result);
        Assert.Equal("Published Event", result.First().Title);
    }

    [Fact]
    public async Task GetSocialEventsForEventAsync_WithPublishedOnlyFalse_ShouldReturnAll()
    {
        // Arrange
        await CreateTestSocialEventAsync("Published Event 2", "published-event-2", publish: true);
        await CreateTestSocialEventAsync("Draft Event 2", "draft-event-2", publish: false);

        // Act
        var result = await _service.GetSocialEventsForEventAsync(_testEventId, publishedOnly: false);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task UpdateSocialEventAsync_WithValidData_ShouldUpdateEvent()
    {
        // Arrange
        var created = await CreateTestSocialEventAsync("Original Title", "update-test", publish: false);

        var updateRequest = new UpdateSocialEventRequest
        {
            Title = "Updated Title",
            Description = "Updated description",
            MaxCapacity = 200,
            DressCode = "Formal"
        };

        // Act
        var result = await _service.UpdateSocialEventAsync(created.Id, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
        Assert.Equal("Updated description", result.Description);
        Assert.Equal(200, result.MaxCapacity);
        Assert.Equal("Formal", result.DressCode);
    }

    [Fact]
    public async Task UpdateSocialEventAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var updateRequest = new UpdateSocialEventRequest { Title = "Updated" };

        // Act
        var result = await _service.UpdateSocialEventAsync(Guid.NewGuid(), updateRequest);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteSocialEventAsync_WithExistingEvent_ShouldDeleteEvent()
    {
        // Arrange
        var created = await CreateTestSocialEventAsync("To Delete", "to-delete", publish: false);

        // Act
        var result = await _service.DeleteSocialEventAsync(created.Id);

        // Assert
        Assert.True(result);
        var deleted = await _service.GetSocialEventByIdAsync(created.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteSocialEventAsync_WithNonExistentId_ShouldReturnFalse()
    {
        // Act
        var result = await _service.DeleteSocialEventAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Social Event Status Tests

    [Fact]
    public async Task PublishSocialEventAsync_ShouldPublishEvent()
    {
        // Arrange
        var created = await CreateTestSocialEventAsync("To Publish", "to-publish", publish: false);

        // Act
        var result = await _service.PublishSocialEventAsync(created.Id);

        // Assert
        Assert.True(result);
        var published = await _service.GetSocialEventByIdAsync(created.Id);
        Assert.NotNull(published);
        Assert.True(published.IsPublished);
        Assert.Equal(SocialEventStatus.Published, published.Status);
    }

    [Fact]
    public async Task CancelSocialEventAsync_ShouldCancelEvent()
    {
        // Arrange
        var created = await CreateTestSocialEventAsync("To Cancel", "to-cancel", publish: true);

        // Act
        var result = await _service.CancelSocialEventAsync(created.Id, "Weather issues");

        // Assert
        Assert.True(result);
        var cancelled = await _service.GetSocialEventByIdAsync(created.Id);
        Assert.NotNull(cancelled);
        Assert.Equal(SocialEventStatus.Cancelled, cancelled.Status);
    }

    #endregion

    #region RSVP Tests

    [Fact]
    public async Task CreateRsvpAsync_WithAvailableCapacity_ShouldCreateRsvp()
    {
        // Arrange
        var socialEvent = await CreateTestSocialEventAsync(
            "Dinner with Capacity",
            "dinner-capacity",
            SocialEventType.Dinner,
            maxCapacity: 50);

        var request = new CreateRsvpRequest
        {
            GuestCount = 0,
            DietaryRequirements = "Vegetarian"
        };

        // Act
        var result = await _service.CreateRsvpAsync(socialEvent.Id, _testUserId, request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Rsvp);
        Assert.Equal(_testUserId, result.Rsvp.UserId);
        Assert.False(result.Rsvp.IsWaitlisted);
        Assert.Equal("Vegetarian", result.Rsvp.DietaryRequirements);
    }

    [Fact]
    public async Task CreateRsvpAsync_WithGuests_ShouldCreateRsvpWithGuests()
    {
        // Arrange
        var socialEvent = await CreateTestSocialEventAsync(
            "Party with Guests",
            "party-guests",
            SocialEventType.Party,
            maxCapacity: 100);

        var request = new CreateRsvpRequest
        {
            GuestCount = 2,
            GuestNames = "John Doe, Jane Doe"
        };

        // Act
        var result = await _service.CreateRsvpAsync(socialEvent.Id, _testUserId, request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Rsvp);
        Assert.Equal(2, result.Rsvp.GuestCount);
        Assert.NotNull(result.Rsvp.GuestNames);
        Assert.Contains("John Doe", result.Rsvp.GuestNames);
    }

    [Fact]
    public async Task CreateRsvpAsync_WhenAtCapacity_ShouldAddToWaitlist()
    {
        // Arrange
        var socialEvent = await CreateTestSocialEventAsync(
            "Small Event",
            "small-event",
            SocialEventType.Networking,
            maxCapacity: 1);

        // First user fills capacity
        var firstUserId = Guid.NewGuid();
        await _service.CreateRsvpAsync(socialEvent.Id, firstUserId, new CreateRsvpRequest());

        // Act - Second user should go to waitlist
        var result = await _service.CreateRsvpAsync(socialEvent.Id, _testUserId, new CreateRsvpRequest());

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Rsvp);
        Assert.True(result.Rsvp.IsWaitlisted);
        Assert.Equal(1, result.Rsvp.WaitlistPosition);
    }

    [Fact]
    public async Task CreateRsvpAsync_WhenUserAlreadyHasRsvp_ShouldFail()
    {
        // Arrange
        var socialEvent = await CreateTestSocialEventAsync(
            "Event with Existing RSVP",
            "existing-rsvp",
            SocialEventType.Networking,
            maxCapacity: 50);

        await _service.CreateRsvpAsync(socialEvent.Id, _testUserId, new CreateRsvpRequest());

        // Act - Try to RSVP again
        var result = await _service.CreateRsvpAsync(socialEvent.Id, _testUserId, new CreateRsvpRequest());

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateRsvpAsync_WithTooManyGuests_ShouldFail()
    {
        // Arrange
        var socialEvent = await CreateTestSocialEventAsync(
            "Event with Guest Limit",
            "guest-limit",
            SocialEventType.Dinner,
            maxCapacity: 100);

        var request = new CreateRsvpRequest
        {
            GuestCount = 5 // Max is 2
        };

        // Act
        var result = await _service.CreateRsvpAsync(socialEvent.Id, _testUserId, request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("guest", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelRsvpAsync_ShouldCancelRsvp()
    {
        // Arrange
        var socialEvent = await CreateTestSocialEventAsync(
            "Event for Cancel",
            "cancel-rsvp",
            SocialEventType.Networking,
            maxCapacity: 50);

        var rsvpResult = await _service.CreateRsvpAsync(socialEvent.Id, _testUserId, new CreateRsvpRequest());

        // Act
        var result = await _service.CancelRsvpAsync(rsvpResult.Rsvp!.Id);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Availability Tests

    [Fact]
    public async Task CheckRsvpAvailabilityAsync_WhenAvailable_ShouldReturnAvailable()
    {
        // Arrange
        var socialEvent = await CreateTestSocialEventAsync(
            "Available Event",
            "available",
            SocialEventType.Networking,
            maxCapacity: 50);

        // Act
        var result = await _service.CheckRsvpAvailabilityAsync(socialEvent.Id, 1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsAvailable);
        Assert.Equal(50, result.AvailableSpots);
    }

    [Fact]
    public async Task CheckRsvpAvailabilityAsync_WhenFull_ShouldShowWaitlistAvailable()
    {
        // Arrange
        var socialEvent = await CreateTestSocialEventAsync(
            "Full Event",
            "full-event",
            SocialEventType.Networking,
            maxCapacity: 1);

        // Fill capacity
        var firstUserId = Guid.NewGuid();
        await _service.CreateRsvpAsync(socialEvent.Id, firstUserId, new CreateRsvpRequest());

        // Act
        var result = await _service.CheckRsvpAvailabilityAsync(socialEvent.Id, 1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.WaitlistEnabled); // Waitlist is available
        Assert.Equal(0, result.AvailableSpots);
    }

    [Fact]
    public async Task CheckRsvpAvailabilityAsync_WhenWaitlistEnabled_ShouldShowWaitlistSize()
    {
        // Arrange
        var socialEvent = await CreateTestSocialEventAsync(
            "Waitlist Size Event",
            "waitlist-size-check",
            SocialEventType.Networking,
            maxCapacity: 1);

        // Fill capacity and add to waitlist
        var firstUserId = Guid.NewGuid();
        await _service.CreateRsvpAsync(socialEvent.Id, firstUserId, new CreateRsvpRequest());
        await _service.CreateRsvpAsync(socialEvent.Id, _testUserId, new CreateRsvpRequest());

        // Act
        var result = await _service.CheckRsvpAvailabilityAsync(socialEvent.Id, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.WaitlistSize);
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task SearchSocialEventsAsync_WithTypeFilter_ShouldReturnMatchingEvents()
    {
        // Arrange
        await CreateTestSocialEventAsync("Networking Event", "networking-search", SocialEventType.Networking);
        await CreateTestSocialEventAsync("Dinner Event", "dinner-search", SocialEventType.Dinner);
        await CreateTestSocialEventAsync("Lunch Event", "lunch-search", SocialEventType.Lunch);

        var request = new SocialEventSearchRequest
        {
            EventId = _testEventId,
            Type = SocialEventType.Dinner
        };

        // Act
        var result = await _service.SearchSocialEventsAsync(request);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Dinner Event", result.Items.First().Title);
    }

    [Fact]
    public async Task SearchSocialEventsAsync_WithSearchText_ShouldReturnMatchingEvents()
    {
        // Arrange
        await CreateTestSocialEventAsync("VIP Dinner", "vip-dinner", SocialEventType.Dinner);
        await CreateTestSocialEventAsync("Team Lunch", "team-lunch", SocialEventType.Lunch);

        var request = new SocialEventSearchRequest
        {
            EventId = _testEventId,
            SearchText = "VIP"
        };

        // Act
        var result = await _service.SearchSocialEventsAsync(request);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("VIP Dinner", result.Items.First().Title);
    }

    [Fact]
    public async Task SearchSocialEventsAsync_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange
        for (int i = 1; i <= 15; i++)
        {
            await CreateTestSocialEventAsync($"Event {i}", $"event-{i}", SocialEventType.Networking);
        }

        var request = new SocialEventSearchRequest
        {
            EventId = _testEventId,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.SearchSocialEventsAsync(request);

        // Assert
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    #endregion

    #region Waitlist Processing Tests

    [Fact]
    public async Task CancelRsvpAsync_WhenSpotOpens_ShouldAutoPromoteFromWaitlist()
    {
        // Arrange
        var socialEvent = await CreateTestSocialEventAsync(
            "Waitlist Test Event",
            "waitlist-process",
            SocialEventType.Networking,
            maxCapacity: 1);

        // First user fills capacity
        var firstUserId = Guid.NewGuid();
        var firstRsvp = await _service.CreateRsvpAsync(socialEvent.Id, firstUserId, new CreateRsvpRequest());

        // Second user goes to waitlist
        var secondRsvp = await _service.CreateRsvpAsync(socialEvent.Id, _testUserId, new CreateRsvpRequest());
        Assert.True(secondRsvp.Rsvp!.IsWaitlisted);

        // Act - Cancel first RSVP (should auto-promote from waitlist)
        await _service.CancelRsvpAsync(firstRsvp.Rsvp!.Id);

        // Assert - Second user should be promoted from waitlist
        var updatedRsvp = await _context.SocialEventRsvps.FindAsync(secondRsvp.Rsvp!.Id);
        Assert.NotNull(updatedRsvp);
        Assert.False(updatedRsvp.IsWaitlisted);
        Assert.Equal(SocialEventRsvpStatus.Confirmed, updatedRsvp.Status);
    }

    #endregion

    #region Check-in Tests

    [Fact]
    public async Task CheckInAttendeeAsync_WithValidRsvp_ShouldCheckIn()
    {
        // Arrange
        var socialEvent = await CreateTestSocialEventAsync(
            "Check-in Event",
            "check-in",
            SocialEventType.Networking,
            maxCapacity: 50);

        var rsvpResult = await _service.CreateRsvpAsync(socialEvent.Id, _testUserId, new CreateRsvpRequest());

        // Act
        var result = await _service.CheckInAttendeeAsync(rsvpResult.Rsvp!.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CheckInAttendeeAsync_WithNonExistentRsvp_ShouldReturnFalse()
    {
        // Act
        var result = await _service.CheckInAttendeeAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    #endregion
}
