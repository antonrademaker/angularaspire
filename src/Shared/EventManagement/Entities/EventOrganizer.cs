using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.EventManagement.Entities;

public enum OrganizerRole
{
    Owner,
    Admin,
    Collaborator
}

public class EventOrganizer
{
    public Guid EventId { get; set; }
    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }

    public Guid PersonId { get; set; }
    [ForeignKey(nameof(PersonId))]
    public Person? Person { get; set; }

    public OrganizerRole Role { get; set; } = OrganizerRole.Collaborator;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
