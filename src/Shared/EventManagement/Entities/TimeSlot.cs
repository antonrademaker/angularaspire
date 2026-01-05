using System;
using System.ComponentModel.DataAnnotations;

namespace Shared.EventManagement.Entities;

public enum TimeSlotType
{
    Session,
    Break,
    Lunch,
    Keynote,
    Networking,
    Workshop
}

public class TimeSlot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }

    public TimeSlotType Type { get; set; } = TimeSlotType.Session;

    /// <summary>
    /// If true, this slot applies to all tracks (e.g. Keynote, Lunch).
    /// If false, it's a slot that can be used for track-specific sessions.
    /// </summary>
    public bool IsEventLevel { get; set; }

    // Audit columns
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
