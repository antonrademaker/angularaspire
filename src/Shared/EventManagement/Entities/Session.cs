using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.EventManagement.Entities;

public enum SessionStatus
{
    Draft,
    Scheduled,
    Cancelled
}

public enum SessionLevel
{
    Beginner,
    Intermediate,
    Advanced
}

public enum SubmissionStatus
{
    NotSubmitted,
    Submitted,
    UnderReview,
    Accepted,
    Rejected
}

[Table("sessions", Schema = "event_management")]
public class Session
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid EventId { get; set; }
    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }

    [Required]
    [MaxLength(50)]
    public string ShortCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Abstract { get; set; }

    public SessionStatus Status { get; set; } = SessionStatus.Draft;

    public bool IsConfirmed { get; set; }
    public bool IsPublished { get; set; }

    public int Duration { get; set; } // In minutes

    public SessionLevel Level { get; set; } = SessionLevel.Beginner;

    [MaxLength(10)]
    public string? Language { get; set; }

    public int? Capacity { get; set; }

    public SubmissionStatus SubmissionStatus { get; set; } = SubmissionStatus.NotSubmitted;

    // Audit columns
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<SessionAssignment> Assignments { get; set; } = new List<SessionAssignment>();
}

[Table("session_assignments", Schema = "event_management")]
public class SessionAssignment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid SessionId { get; set; }
    [ForeignKey(nameof(SessionId))]
    public Session? Session { get; set; }

    [Required]
    public Guid TrackId { get; set; }
    [ForeignKey(nameof(TrackId))]
    public Track? Track { get; set; }

    [Required]
    public Guid TimeSlotId { get; set; }
    [ForeignKey(nameof(TimeSlotId))]
    public TimeSlot? TimeSlot { get; set; }

    public Guid? RoomConfigurationId { get; set; }
    [ForeignKey(nameof(RoomConfigurationId))]
    public RoomConfiguration? RoomConfiguration { get; set; }

    // Storing snapshot as JSON string for now, could be JsonDocument if using Npgsql specific types
    [Column(TypeName = "jsonb")]
    public string? RoomSnapshot { get; set; }

    // Audit columns
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
