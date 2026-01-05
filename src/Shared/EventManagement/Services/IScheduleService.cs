using Shared.EventManagement.Entities;

namespace Shared.EventManagement.Services;

public interface IScheduleService
{
    Task CopyDayScheduleAsync(Guid eventId, DateTimeOffset sourceDate, DateTimeOffset targetDate, CancellationToken cancellationToken = default);
}
