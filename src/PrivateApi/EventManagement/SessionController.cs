using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.EventManagement;
using Shared.EventManagement.Entities;

namespace PrivateApi.EventManagement;

[ApiController]
[Route("api/events/{eventId}/sessions")]
public class SessionController : ControllerBase
{
    private readonly EventDbContext _context;

    public SessionController(EventDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Session>>> GetSessions(Guid eventId)
    {
        return await _context.Sessions
            .Include(s => s.Assignments)
            .Where(s => s.EventId == eventId)
            .OrderBy(s => s.Title)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Session>> CreateSession(Guid eventId, CreateSessionRequest request)
    {
        var session = new Session
        {
            EventId = eventId,
            Title = request.Title,
            ShortCode = request.ShortCode,
            Abstract = request.Abstract,
            Duration = request.Duration,
            Level = request.Level,
            Language = request.Language,
            Capacity = request.Capacity,
            Status = SessionStatus.Draft,
            SubmissionStatus = SubmissionStatus.NotSubmitted
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSessions), new { eventId }, session);
    }

    [HttpPut("{sessionId}")]
    public async Task<IActionResult> UpdateSession(Guid eventId, Guid sessionId, UpdateSessionRequest request)
    {
        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.EventId == eventId);
        if (session == null)
        {
            return NotFound();
        }

        session.Title = request.Title;
        session.ShortCode = request.ShortCode;
        session.Abstract = request.Abstract;
        session.Duration = request.Duration;
        session.Level = request.Level;
        session.Language = request.Language;
        session.Capacity = request.Capacity;
        session.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{sessionId}")]
    public async Task<IActionResult> DeleteSession(Guid eventId, Guid sessionId)
    {
        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.EventId == eventId);
        if (session == null)
        {
            return NotFound();
        }

        _context.Sessions.Remove(session);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{sessionId}/assign")]
    public async Task<IActionResult> AssignSession(Guid eventId, Guid sessionId, AssignSessionRequest request)
    {
        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.EventId == eventId);
        if (session == null)
        {
            return NotFound();
        }

        // Check if assignment already exists for this session
        var existingAssignment = await _context.SessionAssignments
            .FirstOrDefaultAsync(sa => sa.SessionId == sessionId);

        if (existingAssignment != null)
        {
            // Update existing assignment
            existingAssignment.TrackId = request.TrackId;
            existingAssignment.TimeSlotId = request.TimeSlotId;
            existingAssignment.RoomConfigurationId = request.RoomConfigurationId;
            existingAssignment.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            // Create new assignment
            var assignment = new SessionAssignment
            {
                SessionId = sessionId,
                TrackId = request.TrackId,
                TimeSlotId = request.TimeSlotId,
                RoomConfigurationId = request.RoomConfigurationId
            };
            _context.SessionAssignments.Add(assignment);
        }

        session.Status = SessionStatus.Scheduled;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("swap")]
    public async Task<IActionResult> SwapSessions(Guid eventId, SwapSessionsRequest request)
    {
        var session1 = await _context.Sessions
            .Include(s => s.Assignments)
            .FirstOrDefaultAsync(s => s.Id == request.FirstSessionId && s.EventId == eventId);

        var session2 = await _context.Sessions
            .Include(s => s.Assignments)
            .FirstOrDefaultAsync(s => s.Id == request.SecondSessionId && s.EventId == eventId);

        if (session1 == null || session2 == null)
        {
            return NotFound("One or both sessions not found.");
        }

        var assignment1 = session1.Assignments.FirstOrDefault();
        var assignment2 = session2.Assignments.FirstOrDefault();

        if (assignment1 == null && assignment2 == null)
        {
            return BadRequest("Both sessions are unassigned.");
        }

        // If assignment1 is null, create it with assignment2's values, and delete assignment2
        if (assignment1 == null)
        {
            _context.SessionAssignments.Add(new SessionAssignment
            {
                SessionId = session1.Id,
                TrackId = assignment2!.TrackId,
                TimeSlotId = assignment2.TimeSlotId
            });
            _context.SessionAssignments.Remove(assignment2);
            session1.Status = SessionStatus.Scheduled;
            session2.Status = SessionStatus.Draft;
        }
        // If assignment2 is null, create it with assignment1's values, and delete assignment1
        else if (assignment2 == null)
        {
            _context.SessionAssignments.Add(new SessionAssignment
            {
                SessionId = session2.Id,
                TrackId = assignment1.TrackId,
                TimeSlotId = assignment1.TimeSlotId
            });
            _context.SessionAssignments.Remove(assignment1);
            session2.Status = SessionStatus.Scheduled;
            session1.Status = SessionStatus.Draft;
        }
        // Both assigned, swap values
        else
        {
            var tempTrackId = assignment1.TrackId;
            var tempTimeSlotId = assignment1.TimeSlotId;

            assignment1.TrackId = assignment2.TrackId;
            assignment1.TimeSlotId = assignment2.TimeSlotId;

            assignment2.TrackId = tempTrackId;
            assignment2.TimeSlotId = tempTimeSlotId;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{sessionId}/assign")]
    public async Task<IActionResult> UnassignSession(Guid eventId, Guid sessionId)
    {
        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.EventId == eventId);
        if (session == null)
        {
            return NotFound();
        }

        var assignment = await _context.SessionAssignments
            .FirstOrDefaultAsync(sa => sa.SessionId == sessionId);

        if (assignment != null)
        {
            _context.SessionAssignments.Remove(assignment);
            session.Status = SessionStatus.Draft;
            await _context.SaveChangesAsync();
        }

        return NoContent();
    }
}

public record CreateSessionRequest(string Title, string ShortCode, string? Abstract, int Duration, SessionLevel Level, string? Language, int? Capacity);
public record UpdateSessionRequest(string Title, string ShortCode, string? Abstract, int Duration, SessionLevel Level, string? Language, int? Capacity);
public record AssignSessionRequest(Guid TrackId, Guid TimeSlotId, Guid? RoomConfigurationId);
public record SwapSessionsRequest(Guid FirstSessionId, Guid SecondSessionId);
