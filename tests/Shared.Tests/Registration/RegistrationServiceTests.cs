using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Common;
using Shared.EventManagement;
using Shared.Notifications;
using Shared.Registration;
using Shared.UserManagement;
using StackExchange.Redis;
using Xunit;

namespace Shared.Tests.Registration;

public class RegistrationServiceTests : IDisposable
{
    private readonly RegistrationDbContext _registrationContext;
    private readonly Mock<ILogger<RegistrationService>> _mockLogger;
    private readonly Mock<IEventService> _mockEventService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly RegistrationService _registrationService;

    public RegistrationServiceTests()
    {
        var registrationOptions = new DbContextOptionsBuilder<RegistrationDbContext>()
            .UseInMemoryDatabase(databaseName: $"RegistrationDb_{Guid.NewGuid()}")
            .Options;

        var mockConfig = new Mock<IConfiguration>();
        var mockEnvironment = new Mock<IHostEnvironment>();
        var mockRegLogger = new Mock<ILogger<RegistrationDbContext>>();
        
        _registrationContext = new RegistrationDbContext(registrationOptions, mockConfig.Object, mockEnvironment.Object, mockRegLogger.Object);

        _mockLogger = new Mock<ILogger<RegistrationService>>();
        _mockEventService = new Mock<IEventService>();
        _mockUserService = new Mock<IUserService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();

        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_mockDatabase.Object);

        // Configure DatabaseOptions to indicate InMemory mode
        var databaseOptions = Options.Create(new DatabaseOptions { UseInMemoryDatabase = true });

        _registrationService = new RegistrationService(
            _registrationContext,
            _mockUserService.Object,
            _mockEventService.Object,
            _mockEmailService.Object,
            _mockRedis.Object,
            _mockLogger.Object,
            databaseOptions
        );
    }

    [Fact]
    public async Task RegisterUserAsync_WithValidRequest_ShouldCreateRegistration()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FullName = "Test User"
        };

        var eventEntity = new Event
        {
            Id = eventId,
            Title = "Test Event",
            MaxAttendees = 100,
            CurrentAttendees = 50,
            RegistrationOpenDate = DateTime.UtcNow.AddDays(-1),
            RegistrationCloseDate = DateTime.UtcNow.AddDays(10),
            Status = EventStatus.Published
        };

        _mockUserService.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);
        _mockEventService.Setup(x => x.GetEventByIdAsync(eventId, It.IsAny<bool>()))
            .ReturnsAsync(eventEntity);
        _mockEventService.Setup(x => x.IsRegistrationAvailableAsync(eventId))
            .ReturnsAsync(true);

        var registrationRequest = new RegistrationRequest
        {
            EventId = eventId,
            UserId = userId,
            Priority = RegistrationPriority.Normal,
            RegistrationData = new Dictionary<string, object> { { "dietary", "vegetarian" } }
        };

        // Act
        var result = await _registrationService.RegisterUserAsync(registrationRequest);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Registration);
    }

    [Fact]
    public async Task CancelRegistrationAsync_WithValidRegistration_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();

        var registration = new Shared.Registration.Registration
        {
            Id = registrationId,
            UserId = userId,
            EventId = eventId,
            Status = RegistrationStatus.Confirmed,
            RegisteredAt = DateTime.UtcNow.AddDays(-5)
        };

        _registrationContext.Registrations.Add(registration);
        await _registrationContext.SaveChangesAsync();

        // Act
        var result = await _registrationService.CancelRegistrationAsync(registrationId, userId, "User requested cancellation");

        // Assert
        Assert.True(result);

        // Verify registration is cancelled
        var cancelledRegistration = await _registrationContext.Registrations
            .FirstOrDefaultAsync(r => r.Id == registrationId);
        
        Assert.NotNull(cancelledRegistration);
        Assert.Equal(RegistrationStatus.Cancelled, cancelledRegistration.Status);
        Assert.NotNull(cancelledRegistration.CancelledAt);
        Assert.Equal("User requested cancellation", cancelledRegistration.CancellationReason);
    }

    [Fact]
    public async Task GetRegistrationAsync_WithValidId_ShouldReturnRegistration()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();

        var registration = new Shared.Registration.Registration
        {
            Id = registrationId,
            UserId = userId,
            EventId = eventId,
            Status = RegistrationStatus.Confirmed,
            Priority = RegistrationPriority.Normal
        };

        _registrationContext.Registrations.Add(registration);
        await _registrationContext.SaveChangesAsync();

        // Act
        var result = await _registrationService.GetRegistrationAsync(registrationId, false, false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(registrationId, result.Id);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal(RegistrationStatus.Confirmed, result.Status);
    }

    [Fact(Skip = "Method not implemented yet - GetUserRegistrationsAsync throws NotImplementedException")]
    public async Task GetUserRegistrationsAsync_WithUserRegistrations_ShouldReturnList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();

        var registrations = new[]
        {
            new Shared.Registration.Registration
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EventId = eventId1,
                Status = RegistrationStatus.Confirmed
            },
            new Shared.Registration.Registration
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EventId = eventId2,
                Status = RegistrationStatus.Queued
            }
        };

        _registrationContext.Registrations.AddRange(registrations);
        await _registrationContext.SaveChangesAsync();

        // Act
        var result = await _registrationService.GetUserRegistrationsAsync(userId, null, false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(userId, r.UserId));
    }

    public void Dispose()
    {
        _registrationContext?.Dispose();
    }
}