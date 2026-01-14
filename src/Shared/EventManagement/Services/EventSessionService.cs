using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.EventManagement.Entities;

namespace Shared.EventManagement.Services;

public interface IEventSessionService
{
    Task SwapSessionsAsync(Guid eventId, Guid firstSessionId, Guid secondSessionId, CancellationToken cancellationToken = default);
}

public class EventSessionService : IEventSessionService
{
    private readonly AppDbContext _context;

    public EventSessionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task SwapSessionsAsync(Guid eventId, Guid firstSessionId, Guid secondSessionId, CancellationToken cancellationToken = default)
    {
        var assignments = await _context.Set<SessionAssignment>()
            .Where(a => a.Session.EventId == eventId &&
                       (a.SessionId == firstSessionId || a.SessionId == secondSessionId))
            .ToListAsync(cancellationToken);

        var firstAssignment = assignments.FirstOrDefault(a => a.SessionId == firstSessionId);
        var secondAssignment = assignments.FirstOrDefault(a => a.SessionId == secondSessionId);

        if (firstAssignment == null && secondAssignment == null)
        {
            return; // Nothing to swap
        }

        if (firstAssignment != null && secondAssignment != null)
        {
            // Swap TrackId and TimeSlotId
            var tempTrackId = firstAssignment.TrackId;
            var tempTimeSlotId = firstAssignment.TimeSlotId;

            firstAssignment.TrackId = secondAssignment.TrackId;
            firstAssignment.TimeSlotId = secondAssignment.TimeSlotId;

            secondAssignment.TrackId = tempTrackId;
            secondAssignment.TimeSlotId = tempTimeSlotId;
        }
        else if (firstAssignment != null)
        {
            // Move first assignment to second session (effectively unassigning first and assigning second)
            // But wait, if second is unassigned, we can't "swap" into void unless we create a new assignment for second
            // and delete first assignment.

            // If we are swapping a session on the grid with one off the grid:
            // The one on the grid comes off. The one off the grid goes on.

            var newAssignment = new SessionAssignment
            {
                SessionId = secondSessionId,
                TrackId = firstAssignment.TrackId,
                TimeSlotId = firstAssignment.TimeSlotId,
                RoomConfigurationId = firstAssignment.RoomConfigurationId
            };

            _context.Set<SessionAssignment>().Add(newAssignment);
            _context.Set<SessionAssignment>().Remove(firstAssignment);
        }
        else
        {
            // Move second assignment to first session
            var newAssignment = new SessionAssignment
            {
                SessionId = firstSessionId,
                TrackId = secondAssignment.TrackId,
                TimeSlotId = secondAssignment.TimeSlotId,
                RoomConfigurationId = secondAssignment.RoomConfigurationId
            };

            _context.Set<SessionAssignment>().Add(newAssignment);
            _context.Set<SessionAssignment>().Remove(secondAssignment);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
