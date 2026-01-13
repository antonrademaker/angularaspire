using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PrivateApi.EventManagement;
using Shared.EventManagement;
using Shared.EventManagement.Entities;

namespace PrivateApi.Tests.EventManagement;

public class EventTests
{
    private readonly Mock<IEventService> _mockEventService;
    private readonly Mock<ILogger<EventController>> _mockLogger;
    private readonly EventController _controller;

    public EventTests()
    {
        _mockEventService = new Mock<IEventService>();
        _mockLogger = new Mock<ILogger<EventController>>();
        _controller = new EventController(_mockEventService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateEvent_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = "Test Event",
            Slug = "test-event",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(2),
            Description = "Test Description"
        };

        var createdEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Slug = request.Slug,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        _mockEventService.Setup(s => s.CreateEventAsync(It.IsAny<CreateEventRequest>(), It.IsAny<Guid>()))
            .ReturnsAsync(createdEvent);

        // Act
        var result = await _controller.CreateEvent(request);

        // Assert
        var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnValue = Assert.IsType<Event>(actionResult.Value);
        Assert.Equal(createdEvent.Id, returnValue.Id);
        Assert.Equal(createdEvent.Title, returnValue.Title);
    }

    [Fact]
    public async Task GetEvent_ExistingId_ReturnsEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = new Event { Id = eventId, Title = "Existing Event" };

        _mockEventService.Setup(s => s.GetEventByIdAsync(eventId, true))
            .ReturnsAsync(existingEvent);

        // Act
        var result = await _controller.GetEvent(eventId);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<Event>(actionResult.Value);
        Assert.Equal(eventId, returnValue.Id);
    }

    [Fact]
    public async Task GetEvent_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _mockEventService.Setup(s => s.GetEventByIdAsync(eventId, true))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _controller.GetEvent(eventId);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }
}
