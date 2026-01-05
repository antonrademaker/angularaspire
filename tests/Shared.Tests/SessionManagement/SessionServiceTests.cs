using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.EventManagement;
using Shared.EventManagement.Entities;
using Shared.SessionManagement;
using Xunit;

namespace Shared.Tests.SessionManagement;

public class SessionServiceTests : IDisposable
{
    private readonly SessionDbContext _context;
    private readonly Mock<ILogger<SessionService>> _mockLogger;
    private readonly SessionService _sessionService;
    private readonly Guid _testEventId;

    public SessionServiceTests()
    {
        var options = new DbContextOptionsBuilder<SessionDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SessionDbContext(options);
        _mockLogger = new Mock<ILogger<SessionService>>();
        _sessionService = new SessionService(_context, _mockLogger.Object);
        _testEventId = Guid.NewGuid();

        // Seed test event
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add a test event to the database
        var testEvent = new Event
        {
            Id = _testEventId,
            Title = "Test Conference",
            Description = "A test conference",
            Slug = "test-conference",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(32),
            CreatedBy = Guid.NewGuid()
        };

        _context.Set<Event>().Add(testEvent);
        _context.SaveChanges();
    }

    // Session CRUD Tests

    [Fact]
    public async Task CreateSessionAsync_WithValidData_ShouldCreateSession()
    {
        // Arrange
        var request = new CreateSessionRequest
        {
            EventId = _testEventId,
            Title = "Introduction to Angular",
            Description = "Learn the basics of Angular framework",
            Slug = "intro-angular",
            StartTime = DateTime.UtcNow.AddDays(30).AddHours(9),
            EndTime = DateTime.UtcNow.AddDays(30).AddHours(10),
            Type = SessionType.Workshop,
            DifficultyLevel = SessionDifficulty.Beginner,
            MaxAttendees = 50,
            RequiresSubscription = true,
            Room = "Room A",
            Language = "English"
        };

        // Act
        var result = await _sessionService.CreateSessionAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal("Introduction to Angular", result.Value.Title);
        Assert.Equal("intro-angular", result.Value.Slug);
        Assert.Equal(SessionType.Workshop, result.Value.Type);
        Assert.Equal(SessionDifficulty.Beginner, result.Value.DifficultyLevel);
        Assert.Equal(50, result.Value.MaxAttendees);
    }

    [Fact]
    public async Task CreateSessionAsync_WithInvalidEventId_ShouldFail()
    {
        // Arrange
        var request = new CreateSessionRequest
        {
            EventId = Guid.NewGuid(), // Non-existent event
            Title = "Test Session",
            Description = "Test description",
            Slug = "test-session",
            StartTime = DateTime.UtcNow.AddDays(30).AddHours(9),
            EndTime = DateTime.UtcNow.AddDays(30).AddHours(10),
            Type = SessionType.Talk
        };

        // Act
        var result = await _sessionService.CreateSessionAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Event not found", result.Error);
    }

    [Fact]
    public async Task CreateSessionAsync_WithDuplicateSlug_ShouldFail()
    {
        // Arrange
        var request1 = new CreateSessionRequest
        {
            EventId = _testEventId,
            Title = "First Session",
            Description = "Description",
            Slug = "duplicate-slug",
            StartTime = DateTime.UtcNow.AddDays(30).AddHours(9),
            EndTime = DateTime.UtcNow.AddDays(30).AddHours(10),
            Type = SessionType.Talk
        };

        await _sessionService.CreateSessionAsync(request1);

        var request2 = new CreateSessionRequest
        {
            EventId = _testEventId,
            Title = "Second Session",
            Description = "Description",
            Slug = "duplicate-slug",
            StartTime = DateTime.UtcNow.AddDays(30).AddHours(11),
            EndTime = DateTime.UtcNow.AddDays(30).AddHours(12),
            Type = SessionType.Talk
        };

        // Act
        var result = await _sessionService.CreateSessionAsync(request2);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("slug already exists", result.Error);
    }

    [Fact]
    public async Task GetSessionAsync_WithExistingSession_ShouldReturnSession()
    {
        // Arrange
        var createRequest = new CreateSessionRequest
        {
            EventId = _testEventId,
            Title = "Retrievable Session",
            Description = "Test session for retrieval",
            Slug = "retrievable-session",
            StartTime = DateTime.UtcNow.AddDays(30).AddHours(9),
            EndTime = DateTime.UtcNow.AddDays(30).AddHours(10),
            Type = SessionType.Talk
        };

        var createResult = await _sessionService.CreateSessionAsync(createRequest);

        // Act
        var session = await _sessionService.GetSessionAsync(createResult.Value!.Id);

        // Assert
        Assert.NotNull(session);
        Assert.Equal("Retrievable Session", session.Title);
        Assert.Equal("retrievable-session", session.Slug);
    }

    [Fact]
    public async Task GetSessionAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Act
        var session = await _sessionService.GetSessionAsync(Guid.NewGuid());

        // Assert
        Assert.Null(session);
    }

    [Fact]
    public async Task GetSessionBySlugAsync_WithExistingSlug_ShouldReturnSession()
    {
        // Arrange
        var createRequest = new CreateSessionRequest
        {
            EventId = _testEventId,
            Title = "Slug Test Session",
            Description = "Test session for slug retrieval",
            Slug = "slug-test-session",
            StartTime = DateTime.UtcNow.AddDays(30).AddHours(9),
            EndTime = DateTime.UtcNow.AddDays(30).AddHours(10),
            Type = SessionType.Talk
        };

        await _sessionService.CreateSessionAsync(createRequest);

        // Act
        var session = await _sessionService.GetSessionBySlugAsync(_testEventId, "slug-test-session");

        // Assert
        Assert.NotNull(session);
        Assert.Equal("Slug Test Session", session.Title);
    }

    [Fact]
    public async Task UpdateSessionAsync_WithValidData_ShouldUpdateSession()
    {
        // Arrange
        var createRequest = new CreateSessionRequest
        {
            EventId = _testEventId,
            Title = "Original Title",
            Description = "Original description",
            Slug = "update-test-session",
            StartTime = DateTime.UtcNow.AddDays(30).AddHours(9),
            EndTime = DateTime.UtcNow.AddDays(30).AddHours(10),
            Type = SessionType.Talk,
            MaxAttendees = 50
        };

        var createResult = await _sessionService.CreateSessionAsync(createRequest);
        var sessionId = createResult.Value!.Id;

        var updateRequest = new UpdateSessionRequest
        {
            Title = "Updated Title",
            Description = "Updated description",
            MaxAttendees = 100
        };

        // Act
        var result = await _sessionService.UpdateSessionAsync(sessionId, updateRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Updated Title", result.Value.Title);
        Assert.Equal("Updated description", result.Value.Description);
        Assert.Equal(100, result.Value.MaxAttendees);
    }

    [Fact]
    public async Task UpdateSessionAsync_WithNonExistentSession_ShouldFail()
    {
        // Arrange
        var updateRequest = new UpdateSessionRequest
        {
            Title = "Updated Title"
        };

        // Act
        var result = await _sessionService.UpdateSessionAsync(Guid.NewGuid(), updateRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public async Task DeleteSessionAsync_WithNoSubscriptions_ShouldDeleteSession()
    {
        // Arrange
        var createRequest = new CreateSessionRequest
        {
            EventId = _testEventId,
            Title = "Session to Delete",
            Description = "This session will be deleted",
            Slug = "delete-test-session",
            StartTime = DateTime.UtcNow.AddDays(30).AddHours(9),
            EndTime = DateTime.UtcNow.AddDays(30).AddHours(10),
            Type = SessionType.Talk
        };

        var createResult = await _sessionService.CreateSessionAsync(createRequest);
        var sessionId = createResult.Value!.Id;

        // Act
        var result = await _sessionService.DeleteSessionAsync(sessionId);

        // Assert
        Assert.True(result.IsSuccess);
        var deletedSession = await _sessionService.GetSessionAsync(sessionId);
        Assert.Null(deletedSession);
    }

    // Session Status Tests

    [Fact]
    public async Task PublishSessionAsync_ShouldPublishSession()
    {
        // Arrange
        var createRequest = new CreateSessionRequest
        {
            EventId = _testEventId,
            Title = "Session to Publish",
            Description = "Description",
            Slug = "publish-test-session",
            StartTime = DateTime.UtcNow.AddDays(30).AddHours(9),
            EndTime = DateTime.UtcNow.AddDays(30).AddHours(10),
            Type = SessionType.Talk
        };

        var createResult = await _sessionService.CreateSessionAsync(createRequest);
        var sessionId = createResult.Value!.Id;

        // Act
        var result = await _sessionService.PublishSessionAsync(sessionId);

        // Assert
        Assert.True(result.IsSuccess);
        var publishedSession = await _sessionService.GetSessionAsync(sessionId);
        Assert.NotNull(publishedSession);
        Assert.True(publishedSession.IsPublished);
        Assert.Equal(SessionStatus.Published, publishedSession.Status);
    }

    [Fact]
    public async Task UnpublishSessionAsync_ShouldUnpublishSession()
    {
        // Arrange
        var createRequest = new CreateSessionRequest
        {
            EventId = _testEventId,
            Title = "Session to Unpublish",
            Description = "Description",
            Slug = "unpublish-test-session",
            StartTime = DateTime.UtcNow.AddDays(30).AddHours(9),
            EndTime = DateTime.UtcNow.AddDays(30).AddHours(10),
            Type = SessionType.Talk
        };

        var createResult = await _sessionService.CreateSessionAsync(createRequest);
        var sessionId = createResult.Value!.Id;

        await _sessionService.PublishSessionAsync(sessionId);

        // Act
        var result = await _sessionService.UnpublishSessionAsync(sessionId);

        // Assert
        Assert.True(result.IsSuccess);
        var unpublishedSession = await _sessionService.GetSessionAsync(sessionId);
        Assert.NotNull(unpublishedSession);
        Assert.False(unpublishedSession.IsPublished);
        Assert.Equal(SessionStatus.Draft, unpublishedSession.Status);
    }

    // Session Search Tests

    [Fact]
    public async Task SearchSessionsAsync_WithSearchText_ShouldReturnMatchingSessions()
    {
        // Arrange
        await CreateTestSession("Angular Workshop", "angular-workshop", SessionType.Workshop);
        await CreateTestSession("React Talk", "react-talk", SessionType.Talk);
        await CreateTestSession("Vue.js Demo", "vuejs-demo", SessionType.Demo);

        // Publish sessions so they can be found
        var sessions = await _context.Sessions.ToListAsync();
        foreach (var session in sessions)
        {
            session.IsPublished = true;
        }
        await _context.SaveChangesAsync();

        var searchRequest = new SessionSearchRequest
        {
            EventId = _testEventId,
            SearchText = "Angular",
            PageSize = 20
        };

        // Act
        var result = await _sessionService.SearchSessionsAsync(searchRequest);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Angular Workshop", result.Items.First().Title);
    }

    [Fact]
    public async Task SearchSessionsAsync_WithTypeFilter_ShouldReturnFilteredSessions()
    {
        // Arrange
        await CreateTestSession("Workshop 1", "workshop-1", SessionType.Workshop);
        await CreateTestSession("Workshop 2", "workshop-2", SessionType.Workshop);
        await CreateTestSession("Talk 1", "talk-1", SessionType.Talk);

        var sessions = await _context.Sessions.ToListAsync();
        foreach (var session in sessions)
        {
            session.IsPublished = true;
        }
        await _context.SaveChangesAsync();

        var searchRequest = new SessionSearchRequest
        {
            EventId = _testEventId,
            Types = new List<SessionType> { SessionType.Workshop },
            PageSize = 20
        };

        // Act
        var result = await _sessionService.SearchSessionsAsync(searchRequest);

        // Assert
        Assert.Equal(2, result.Items.Count());
        Assert.All(result.Items, s => Assert.Equal(SessionType.Workshop, s.Type));
    }

    [Fact]
    public async Task SearchSessionsAsync_WithDifficultyFilter_ShouldReturnFilteredSessions()
    {
        // Arrange
        await CreateTestSession("Beginner Session", "beginner-session", SessionType.Talk, SessionDifficulty.Beginner);
        await CreateTestSession("Advanced Session", "advanced-session", SessionType.Talk, SessionDifficulty.Advanced);

        var sessions = await _context.Sessions.ToListAsync();
        foreach (var session in sessions)
        {
            session.IsPublished = true;
        }
        await _context.SaveChangesAsync();

        var searchRequest = new SessionSearchRequest
        {
            EventId = _testEventId,
            DifficultyLevels = new List<SessionDifficulty> { SessionDifficulty.Beginner },
            PageSize = 20
        };

        // Act
        var result = await _sessionService.SearchSessionsAsync(searchRequest);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(SessionDifficulty.Beginner, result.Items.First().DifficultyLevel);
    }

    [Fact]
    public async Task GetEventSessionsAsync_ShouldReturnSessionsForEvent()
    {
        // Arrange
        await CreateTestSession("Event Session 1", "event-session-1", SessionType.Talk);
        await CreateTestSession("Event Session 2", "event-session-2", SessionType.Workshop);

        var sessions = await _context.Sessions.ToListAsync();
        foreach (var session in sessions)
        {
            session.IsPublished = true;
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _sessionService.GetEventSessionsAsync(_testEventId);

        // Assert
        Assert.Equal(2, result.Count());
    }

    // Subscription Tests

    [Fact]
    public async Task SubscribeToSessionAsync_WithAvailableCapacity_ShouldConfirmSubscription()
    {
        // Arrange
        var createResult = await CreateTestSession("Subscribe Test Session", "subscribe-test", SessionType.Workshop, maxAttendees: 10);
        var sessionId = createResult.Value!.Id;
        var userId = Guid.NewGuid();

        // Act
        var result = await _sessionService.SubscribeToSessionAsync(sessionId, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(SubscriptionStatus.Confirmed, result.Value.Status);
        Assert.False(result.Value.IsWaitlisted);
    }

    [Fact]
    public async Task SubscribeToSessionAsync_AtFullCapacity_ShouldAddToWaitlist()
    {
        // Arrange
        var createResult = await CreateTestSession("Full Capacity Session", "full-capacity", SessionType.Workshop, maxAttendees: 1);
        var sessionId = createResult.Value!.Id;

        // Fill capacity
        var user1 = Guid.NewGuid();
        await _sessionService.SubscribeToSessionAsync(sessionId, user1);

        var user2 = Guid.NewGuid();

        // Act
        var result = await _sessionService.SubscribeToSessionAsync(sessionId, user2);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(SubscriptionStatus.Waitlisted, result.Value.Status);
        Assert.True(result.Value.IsWaitlisted);
        Assert.Equal(1, result.Value.WaitlistPosition);
    }

    [Fact]
    public async Task SubscribeToSessionAsync_WhenAlreadySubscribed_ShouldFail()
    {
        // Arrange
        var createResult = await CreateTestSession("Duplicate Subscribe Session", "duplicate-subscribe", SessionType.Workshop);
        var sessionId = createResult.Value!.Id;
        var userId = Guid.NewGuid();

        await _sessionService.SubscribeToSessionAsync(sessionId, userId);

        // Act
        var result = await _sessionService.SubscribeToSessionAsync(sessionId, userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("already subscribed", result.Error);
    }

    [Fact]
    public async Task UnsubscribeFromSessionAsync_WithExistingSubscription_ShouldUnsubscribe()
    {
        // Arrange
        var createResult = await CreateTestSession("Unsubscribe Test Session", "unsubscribe-test", SessionType.Workshop);
        var sessionId = createResult.Value!.Id;
        var userId = Guid.NewGuid();

        await _sessionService.SubscribeToSessionAsync(sessionId, userId);

        // Act
        var result = await _sessionService.UnsubscribeFromSessionAsync(sessionId, userId);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetUserSubscriptionsAsync_ShouldReturnUserSubscriptions()
    {
        // Arrange
        await CreateTestSession("User Sub Session 1", "user-sub-1", SessionType.Workshop);
        await CreateTestSession("User Sub Session 2", "user-sub-2", SessionType.Workshop);

        var sessions = await _context.Sessions.ToListAsync();
        var userId = Guid.NewGuid();

        foreach (var session in sessions)
        {
            await _sessionService.SubscribeToSessionAsync(session.Id, userId);
        }

        // Act
        var result = await _sessionService.GetUserSubscriptionsAsync(userId, _testEventId);

        // Assert
        Assert.Equal(2, result.Count());
    }

    // Availability Tests

    [Fact]
    public async Task CheckSessionAvailabilityAsync_WithCapacityAvailable_ShouldReturnAvailable()
    {
        // Arrange
        var createResult = await CreateTestSession("Available Session", "available-session", SessionType.Workshop, maxAttendees: 10);
        var sessionId = createResult.Value!.Id;

        // Act
        var result = await _sessionService.CheckSessionAvailabilityAsync(sessionId);

        // Assert
        Assert.True(result.IsAvailable);
        Assert.Equal(10, result.RemainingCapacity);
        Assert.Equal(0, result.WaitlistCount);
    }

    [Fact]
    public async Task CheckSessionAvailabilityAsync_AtFullCapacity_ShouldReturnUnavailable()
    {
        // Arrange
        var createResult = await CreateTestSession("Full Session", "full-session", SessionType.Workshop, maxAttendees: 1);
        var sessionId = createResult.Value!.Id;

        await _sessionService.SubscribeToSessionAsync(sessionId, Guid.NewGuid());

        // Act
        var result = await _sessionService.CheckSessionAvailabilityAsync(sessionId);

        // Assert
        Assert.False(result.IsAvailable);
        Assert.Equal(0, result.RemainingCapacity);
        Assert.Contains("capacity", result.UnavailabilityReason);
    }

    [Fact]
    public async Task CheckSessionAvailabilityAsync_WithUnlimitedCapacity_ShouldReturnAvailable()
    {
        // Arrange
        var createResult = await CreateTestSession("Unlimited Session", "unlimited-session", SessionType.Workshop, maxAttendees: null);
        var sessionId = createResult.Value!.Id;

        // Act
        var result = await _sessionService.CheckSessionAvailabilityAsync(sessionId);

        // Assert
        Assert.True(result.IsAvailable);
        Assert.Null(result.RemainingCapacity);
    }

    // Waitlist Tests

    [Fact]
    public async Task ProcessWaitlistAsync_WhenCapacityBecomesAvailable_ShouldPromoteWaitlistedUsers()
    {
        // Arrange
        var createResult = await CreateTestSession("Waitlist Test Session", "waitlist-test", SessionType.Workshop, maxAttendees: 1);
        var sessionId = createResult.Value!.Id;

        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        await _sessionService.SubscribeToSessionAsync(sessionId, user1);
        await _sessionService.SubscribeToSessionAsync(sessionId, user2);

        // Manually update the session to allow one more person
        var session = await _context.Sessions.FindAsync(sessionId);
        session!.MaxAttendees = 2;
        await _context.SaveChangesAsync();

        // Act
        var promotedCount = await _sessionService.ProcessWaitlistAsync(sessionId);

        // Assert
        Assert.Equal(1, promotedCount);

        var subscriptions = await _sessionService.GetSessionSubscriptionsAsync(sessionId, includeWaitlisted: true);
        Assert.All(subscriptions, s => Assert.Equal(SubscriptionStatus.Confirmed, s.Status));
    }

    [Fact]
    public async Task GetSessionWaitlistAsync_ShouldReturnWaitlistedUsers()
    {
        // Arrange
        var createResult = await CreateTestSession("Waitlist List Session", "waitlist-list", SessionType.Workshop, maxAttendees: 1);
        var sessionId = createResult.Value!.Id;

        await _sessionService.SubscribeToSessionAsync(sessionId, Guid.NewGuid()); // Fills capacity
        await _sessionService.SubscribeToSessionAsync(sessionId, Guid.NewGuid()); // Goes to waitlist
        await _sessionService.SubscribeToSessionAsync(sessionId, Guid.NewGuid()); // Goes to waitlist

        // Act
        var waitlist = await _sessionService.GetSessionWaitlistAsync(sessionId);

        // Assert
        Assert.Equal(2, waitlist.Count());
        Assert.All(waitlist, s => Assert.True(s.IsWaitlisted));
    }

    // Conflict Detection Tests

    [Fact]
    public async Task DetectSessionConflictsAsync_WithRoomConflict_ShouldDetectConflict()
    {
        // Arrange
        var createRequest = new CreateSessionRequest
        {
            EventId = _testEventId,
            Title = "First Room Session",
            Description = "Description",
            Slug = "first-room-session",
            StartTime = DateTime.UtcNow.AddDays(30).AddHours(9),
            EndTime = DateTime.UtcNow.AddDays(30).AddHours(10),
            Type = SessionType.Talk,
            Room = "Room A"
        };

        var firstResult = await _sessionService.CreateSessionAsync(createRequest);

        // Publish first session
        var firstSession = await _context.Sessions.FindAsync(firstResult.Value!.Id);
        firstSession!.IsPublished = true;
        await _context.SaveChangesAsync();

        // Act
        var conflicts = await _sessionService.DetectSessionConflictsAsync(
            Guid.Empty,
            DateTime.UtcNow.AddDays(30).AddHours(9).AddMinutes(30),
            DateTime.UtcNow.AddDays(30).AddHours(10).AddMinutes(30),
            "Room A"
        );

        // Assert
        Assert.Single(conflicts);
        Assert.Equal("Room", conflicts.First().ConflictType);
        Assert.Equal("First Room Session", conflicts.First().ConflictingSessionTitle);
    }

    // Track Tests

    [Fact]
    public async Task CreateSessionAsync_WithValidTrack_ShouldAssociateSessionToTrack()
    {
        // Arrange
        var track = new Track
        {
            EventId = _testEventId,
            Name = "Web Development",
            Description = "Web development track",
            Slug = "web-development",
            Color = "#FF5733",
            DisplayOrder = 1,
            CreatedByUserId = Guid.NewGuid()
        };

        _context.Tracks.Add(track);
        await _context.SaveChangesAsync();

        var request = new CreateSessionRequest
        {
            EventId = _testEventId,
            TrackId = track.Id,
            Title = "Track Session",
            Description = "Description",
            Slug = "track-session",
            StartTime = DateTime.UtcNow.AddDays(30).AddHours(9),
            EndTime = DateTime.UtcNow.AddDays(30).AddHours(10),
            Type = SessionType.Talk
        };

        // Act
        var result = await _sessionService.CreateSessionAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(track.Id, result.Value!.TrackId);
    }

    [Fact]
    public async Task CreateSessionAsync_WithInvalidTrack_ShouldFail()
    {
        // Arrange
        var request = new CreateSessionRequest
        {
            EventId = _testEventId,
            TrackId = Guid.NewGuid(), // Non-existent track
            Title = "Invalid Track Session",
            Description = "Description",
            Slug = "invalid-track-session",
            StartTime = DateTime.UtcNow.AddDays(30).AddHours(9),
            EndTime = DateTime.UtcNow.AddDays(30).AddHours(10),
            Type = SessionType.Talk
        };

        // Act
        var result = await _sessionService.CreateSessionAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Track not found", result.Error);
    }

    [Fact]
    public async Task GetTrackSessionsAsync_ShouldReturnSessionsForTrack()
    {
        // Arrange
        var track = new Track
        {
            EventId = _testEventId,
            Name = "Track Sessions Test",
            Description = "Test track",
            Slug = "track-sessions-test",
            Color = "#336699",
            DisplayOrder = 1,
            CreatedByUserId = Guid.NewGuid()
        };

        _context.Tracks.Add(track);
        await _context.SaveChangesAsync();

        var session1 = new Session
        {
            EventId = _testEventId,
            TrackId = track.Id,
            Title = "Track Session 1",
            Description = "Description",
            Slug = "track-session-1",
            StartTime = DateTime.UtcNow.AddDays(30).AddHours(9),
            EndTime = DateTime.UtcNow.AddDays(30).AddHours(10),
            Type = SessionType.Talk,
            IsPublished = true,
            CreatedByUserId = Guid.NewGuid()
        };

        var session2 = new Session
        {
            EventId = _testEventId,
            TrackId = track.Id,
            Title = "Track Session 2",
            Description = "Description",
            Slug = "track-session-2",
            StartTime = DateTime.UtcNow.AddDays(30).AddHours(11),
            EndTime = DateTime.UtcNow.AddDays(30).AddHours(12),
            Type = SessionType.Workshop,
            IsPublished = true,
            CreatedByUserId = Guid.NewGuid()
        };

        _context.Sessions.AddRange(session1, session2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sessionService.GetTrackSessionsAsync(track.Id);

        // Assert
        Assert.Equal(2, result.Count());
    }

    // Helper Methods

    private async Task<Shared.Common.Result<SessionResponse>> CreateTestSession(
        string title,
        string slug,
        SessionType type,
        SessionDifficulty difficulty = SessionDifficulty.Intermediate,
        int? maxAttendees = null)
    {
        var request = new CreateSessionRequest
        {
            EventId = _testEventId,
            Title = title,
            Description = $"Description for {title}",
            Slug = slug,
            StartTime = DateTime.UtcNow.AddDays(30).AddHours(9 + _context.Sessions.Count()),
            EndTime = DateTime.UtcNow.AddDays(30).AddHours(10 + _context.Sessions.Count()),
            Type = type,
            DifficultyLevel = difficulty,
            MaxAttendees = maxAttendees,
            RequiresSubscription = maxAttendees.HasValue,
            Language = "English"
        };

        return await _sessionService.CreateSessionAsync(request);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
