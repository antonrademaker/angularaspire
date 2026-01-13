using Shared.Common;
using Shared.EventManagement.Entities;

namespace Shared.EventManagement.Domain;

public static class EventStatusMachine
{
    public static Result CanTransition(EventStatus current, EventStatus next)
    {
        if (current == next)
            return Result.Success();

        return current switch
        {
            EventStatus.Draft => next switch
            {
                EventStatus.Published => Result.Success(),
                EventStatus.Cancelled => Result.Success(),
                _ => Result.Failure($"Cannot transition from {current} to {next}")
            },
            EventStatus.Published => next switch
            {
                EventStatus.Active => Result.Success(),
                EventStatus.Cancelled => Result.Success(),
                EventStatus.Draft => Result.Success(), // Unpublish
                _ => Result.Failure($"Cannot transition from {current} to {next}")
            },
            EventStatus.Active => next switch
            {
                EventStatus.Completed => Result.Success(),
                EventStatus.Cancelled => Result.Success(),
                _ => Result.Failure($"Cannot transition from {current} to {next}")
            },
            EventStatus.Completed => next switch
            {
                EventStatus.Archived => Result.Success(),
                _ => Result.Failure($"Cannot transition from {current} to {next}")
            },
            EventStatus.Cancelled => next switch
            {
                EventStatus.Archived => Result.Success(),
                EventStatus.Draft => Result.Success(), // Reopen as draft
                _ => Result.Failure($"Cannot transition from {current} to {next}")
            },
            EventStatus.Archived => Result.Failure($"Cannot transition from {current} to {next}"),
            _ => Result.Failure($"Unknown status {current}")
        };
    }
}
