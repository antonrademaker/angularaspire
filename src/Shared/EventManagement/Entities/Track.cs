using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.EventManagement.Entities;

/// <summary>
/// Track entity representing a thematic track within a multi-track event
/// Tracks organize sessions by topic, technology, or audience level
/// </summary>
[Table("tracks")]
public class Track
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid EventId { get; set; }
    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(20)]
    public string? Color { get; set; } // Hex code

    public int Order { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid UpdatedBy { get; set; }
}
