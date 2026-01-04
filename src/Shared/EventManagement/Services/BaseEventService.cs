using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Shared.EventManagement.Services;

public abstract class BaseEventService
{
    protected readonly ILogger _logger;

    protected BaseEventService(ILogger logger)
    {
        _logger = logger;
    }

    protected void ValidateEventDates(DateTime start, DateTime end)
    {
        if (end < start)
        {
            throw new ArgumentException("End date must be after start date.");
        }
    }

    protected void ValidateEntity(object entity)
    {
        // Basic validation logic using DataAnnotations
        var context = new ValidationContext(entity);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(entity, context, results, true))
        {
            var messages = string.Join("; ", results.Select(r => r.ErrorMessage));
            throw new ArgumentException($"Validation failed: {messages}");
        }
    }
}
