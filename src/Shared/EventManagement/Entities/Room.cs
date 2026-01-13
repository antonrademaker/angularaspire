using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.EventManagement.Entities;

public class Room
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid LocationId { get; set; }
    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public bool IsActive { get; set; } = true;

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public ICollection<RoomConfiguration> Configurations { get; set; } = new List<RoomConfiguration>();
}

public class RoomConfiguration
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid RoomId { get; set; }
    [ForeignKey(nameof(RoomId))]
    public Room? Room { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty; // e.g. Theater, Classroom

    public int Capacity { get; set; }
}
