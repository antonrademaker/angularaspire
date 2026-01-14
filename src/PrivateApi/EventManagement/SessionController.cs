using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.EventManagement;
using Shared.EventManagement.Entities;
using Shared.EventManagement.Services;
using EventSession = Shared.EventManagement.Entities.Session;

namespace PrivateApi.EventManagement;

[ApiController]
[Route("api/events/{eventId}/sessions")]
public class SessionController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEventSessionService _sessionService;

    public SessionController(AppDbContext context, IEventSessionService sessionService)
    {
        _context = context;
        _sessionService = sessionService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventSession>>> GetSessions(Guid eventId)
    {
        return await _context.Set<EventSession>()
            .Include(s => s.Assignments)
            .Where(s => s.EventId == eventId)
            .OrderBy(s => s.Title)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<EventSession>> CreateSession(Guid eventId, CreateSessionRequest request)
    {
        var session = new EventSession
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

        _context.Set<EventSession>().Add(session);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSessions), new { eventId }, session);
    }

    [HttpPut("{sessionId}")]
    public async Task<IActionResult> UpdateSession(Guid eventId, Guid sessionId, UpdateSessionRequest request)
    {
        var session = await _context.Set<EventSession>().FirstOrDefaultAsync(s => s.Id == sessionId && s.EventId == eventId);
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
        var session = await _context.Set<EventSession>().FirstOrDefaultAsync(s => s.Id == sessionId && s.EventId == eventId);
        if (session == null)
        {
            return NotFound();
        }

        _context.Set<EventSession>().Remove(session);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{sessionId}/assign")]
    public async Task<IActionResult> AssignSession(Guid eventId, Guid sessionId, AssignSessionRequest request)
    {
        var session = await _context.Set<EventSession>().FirstOrDefaultAsync(s => s.Id == sessionId && s.EventId == eventId);
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
        await _sessionService.SwapSessionsAsync(eventId, request.FirstSessionId, request.SecondSessionId);
        return NoContent();
    }

    [HttpDelete("{sessionId}/assign")]
    public async Task<IActionResult> UnassignSession(Guid eventId, Guid sessionId)
    {
        var session = await _context.Set<EventSession>().FirstOrDefaultAsync(s => s.Id == sessionId && s.EventId == eventId);
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

