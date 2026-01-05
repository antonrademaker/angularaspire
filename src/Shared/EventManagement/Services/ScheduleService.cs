using Microsoft.EntityFrameworkCore;
using Shared.EventManagement.Entities;

namespace Shared.EventManagement.Services;

public class ScheduleService : IScheduleService
{
    private readonly EventDbContext _context;

    public ScheduleService(EventDbContext context)
    {
        _context = context;
    }

    public async Task CopyDayScheduleAsync(Guid eventId, DateTimeOffset sourceDate, DateTimeOffset targetDate, CancellationToken cancellationToken = default)
    {
        // Normalize dates to start of day in UTC
        var sourceStart = new DateTimeOffset(sourceDate.Date, TimeSpan.Zero);
        var sourceEnd = sourceStart.AddDays(1);

        var sourceSlots = await _context.TimeSlots
            .Where(t => t.EventId == eventId && t.StartTime >= sourceStart && t.StartTime < sourceEnd)
            .ToListAsync(cancellationToken);

        if (!sourceSlots.Any())
        {
            return;
        }

        var timeDifference = targetDate.Date - sourceDate.Date;

        foreach (var slot in sourceSlots)
        {
            var newSlot = new TimeSlot
            {
                EventId = eventId,
                Name = slot.Name,
                StartTime = slot.StartTime.Add(timeDifference),
                EndTime = slot.EndTime.Add(timeDifference),
                Type = slot.Type,
                IsEventLevel = slot.IsEventLevel
            };

            _context.TimeSlots.Add(newSlot);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
