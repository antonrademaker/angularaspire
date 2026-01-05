using Microsoft.EntityFrameworkCore;
using Shared.EventManagement;
using Shared.EventManagement.Entities;
using Shared.EventManagement.Services;
using Xunit;

namespace Shared.Tests.EventManagement;

public class SessionSwapTests : IDisposable
{
    private readonly EventDbContext _context;
    private readonly EventSessionService _service;

    public SessionSwapTests()
    {
        var options = new DbContextOptionsBuilder<EventDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EventDbContext(options);
        _service = new EventSessionService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task SwapSessionsAsync_BothAssigned_ShouldSwapAssignments()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var track1Id = Guid.NewGuid();
        var track2Id = Guid.NewGuid();
        var timeSlot1Id = Guid.NewGuid();
        var timeSlot2Id = Guid.NewGuid();

        var session1 = new Session { Id = Guid.NewGuid(), EventId = eventId, Title = "Session 1", ShortCode = "s1" };
        var session2 = new Session { Id = Guid.NewGuid(), EventId = eventId, Title = "Session 2", ShortCode = "s2" };

        var assignment1 = new SessionAssignment
        {
            SessionId = session1.Id,
            TrackId = track1Id,
            TimeSlotId = timeSlot1Id
        };

        var assignment2 = new SessionAssignment
        {
            SessionId = session2.Id,
            TrackId = track2Id,
            TimeSlotId = timeSlot2Id
        };

        _context.Sessions.AddRange(session1, session2);
        _context.SessionAssignments.AddRange(assignment1, assignment2);
        await _context.SaveChangesAsync();

        // Act
        await _service.SwapSessionsAsync(eventId, session1.Id, session2.Id);

        // Assert
        var updatedAssignment1 = await _context.SessionAssignments.FirstOrDefaultAsync(a => a.SessionId == session1.Id);
        var updatedAssignment2 = await _context.SessionAssignments.FirstOrDefaultAsync(a => a.SessionId == session2.Id);

        Assert.NotNull(updatedAssignment1);
        Assert.NotNull(updatedAssignment2);

        // Session 1 should now have Track 2 / TimeSlot 2
        Assert.Equal(track2Id, updatedAssignment1.TrackId);
        Assert.Equal(timeSlot2Id, updatedAssignment1.TimeSlotId);

        // Session 2 should now have Track 1 / TimeSlot 1
        Assert.Equal(track1Id, updatedAssignment2.TrackId);
        Assert.Equal(timeSlot1Id, updatedAssignment2.TimeSlotId);
    }

    [Fact]
    public async Task SwapSessionsAsync_FirstAssignedSecondUnassigned_ShouldMoveAssignment()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var trackId = Guid.NewGuid();
        var timeSlotId = Guid.NewGuid();

        var session1 = new Session { Id = Guid.NewGuid(), EventId = eventId, Title = "Session 1", ShortCode = "s1" };
        var session2 = new Session { Id = Guid.NewGuid(), EventId = eventId, Title = "Session 2", ShortCode = "s2" };

        var assignment1 = new SessionAssignment
        {
            SessionId = session1.Id,
            TrackId = trackId,
            TimeSlotId = timeSlotId
        };

        _context.Sessions.AddRange(session1, session2);
        _context.SessionAssignments.Add(assignment1);
        await _context.SaveChangesAsync();

        // Act
        await _service.SwapSessionsAsync(eventId, session1.Id, session2.Id);

        // Assert
        var updatedAssignment1 = await _context.SessionAssignments.FirstOrDefaultAsync(a => a.SessionId == session1.Id);
        var updatedAssignment2 = await _context.SessionAssignments.FirstOrDefaultAsync(a => a.SessionId == session2.Id);

        // Session 1 should be unassigned
        Assert.Null(updatedAssignment1);

        // Session 2 should be assigned to Track / TimeSlot
        Assert.NotNull(updatedAssignment2);
        Assert.Equal(trackId, updatedAssignment2.TrackId);
        Assert.Equal(timeSlotId, updatedAssignment2.TimeSlotId);
    }

    [Fact]
    public async Task SwapSessionsAsync_FirstUnassignedSecondAssigned_ShouldMoveAssignment()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var trackId = Guid.NewGuid();
        var timeSlotId = Guid.NewGuid();

        var session1 = new Session { Id = Guid.NewGuid(), EventId = eventId, Title = "Session 1", ShortCode = "s1" };
        var session2 = new Session { Id = Guid.NewGuid(), EventId = eventId, Title = "Session 2", ShortCode = "s2" };

        var assignment2 = new SessionAssignment
        {
            SessionId = session2.Id,
            TrackId = trackId,
            TimeSlotId = timeSlotId
        };

        _context.Sessions.AddRange(session1, session2);
        _context.SessionAssignments.Add(assignment2);
        await _context.SaveChangesAsync();

        // Act
        await _service.SwapSessionsAsync(eventId, session1.Id, session2.Id);

        // Assert
        var updatedAssignment1 = await _context.SessionAssignments.FirstOrDefaultAsync(a => a.SessionId == session1.Id);
        var updatedAssignment2 = await _context.SessionAssignments.FirstOrDefaultAsync(a => a.SessionId == session2.Id);

        // Session 2 should be unassigned
        Assert.Null(updatedAssignment2);

        // Session 1 should be assigned to Track / TimeSlot
        Assert.NotNull(updatedAssignment1);
        Assert.Equal(trackId, updatedAssignment1.TrackId);
        Assert.Equal(timeSlotId, updatedAssignment1.TimeSlotId);
    }

    [Fact]
    public async Task SwapSessionsAsync_BothUnassigned_ShouldDoNothing()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var session1 = new Session { Id = Guid.NewGuid(), EventId = eventId, Title = "Session 1", ShortCode = "s1" };
        var session2 = new Session { Id = Guid.NewGuid(), EventId = eventId, Title = "Session 2", ShortCode = "s2" };

        _context.Sessions.AddRange(session1, session2);
        await _context.SaveChangesAsync();

        // Act
        await _service.SwapSessionsAsync(eventId, session1.Id, session2.Id);

        // Assert
        var updatedAssignment1 = await _context.SessionAssignments.FirstOrDefaultAsync(a => a.SessionId == session1.Id);
        var updatedAssignment2 = await _context.SessionAssignments.FirstOrDefaultAsync(a => a.SessionId == session2.Id);

        Assert.Null(updatedAssignment1);
        Assert.Null(updatedAssignment2);
    }
}
