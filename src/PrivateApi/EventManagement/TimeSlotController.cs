using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.EventManagement;
using Shared.EventManagement.Entities;
using Shared.EventManagement.Services;

namespace PrivateApi.EventManagement;

[ApiController]
[Route("api/events/{eventId}/time-slots")]
public class TimeSlotController : ControllerBase
{
    private readonly AppDbContext _context;

    public TimeSlotController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TimeSlot>>> GetTimeSlots(Guid eventId)
    {
        return await _context.TimeSlots
            .Where(t => t.EventId == eventId)
            .OrderBy(t => t.StartTime)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<TimeSlot>> CreateTimeSlot(Guid eventId, CreateTimeSlotRequest request)
    {
        var timeSlot = new TimeSlot
        {
            EventId = eventId,
            Name = request.Name,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Type = request.Type,
            IsEventLevel = request.IsEventLevel
        };

        _context.TimeSlots.Add(timeSlot);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTimeSlots), new { eventId }, timeSlot);
    }

    [HttpPut("{timeSlotId}")]
    public async Task<IActionResult> UpdateTimeSlot(Guid eventId, Guid timeSlotId, UpdateTimeSlotRequest request)
    {
        var timeSlot = await _context.TimeSlots.FirstOrDefaultAsync(t => t.Id == timeSlotId && t.EventId == eventId);
        if (timeSlot == null)
        {
            return NotFound();
        }

        timeSlot.Name = request.Name;
        timeSlot.StartTime = request.StartTime;
        timeSlot.EndTime = request.EndTime;
        timeSlot.Type = request.Type;
        timeSlot.IsEventLevel = request.IsEventLevel;
        timeSlot.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{timeSlotId}")]
    public async Task<IActionResult> DeleteTimeSlot(Guid eventId, Guid timeSlotId)
    {
        var timeSlot = await _context.TimeSlots.FirstOrDefaultAsync(t => t.Id == timeSlotId && t.EventId == eventId);
        if (timeSlot == null)
        {
            return NotFound();
        }

        _context.TimeSlots.Remove(timeSlot);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("copy")]
    public async Task<IActionResult> CopyDaySchedule(Guid eventId, CopyDayScheduleRequest request, [FromServices] IScheduleService scheduleService)
    {
        await scheduleService.CopyDayScheduleAsync(eventId, request.SourceDate, request.TargetDate);
        return NoContent();
    }
}

public record CreateTimeSlotRequest(string Name, DateTimeOffset StartTime, DateTimeOffset EndTime, TimeSlotType Type, bool IsEventLevel);
public record UpdateTimeSlotRequest(string Name, DateTimeOffset StartTime, DateTimeOffset EndTime, TimeSlotType Type, bool IsEventLevel);
public record CopyDayScheduleRequest(DateTimeOffset SourceDate, DateTimeOffset TargetDate);

