using System.ComponentModel.DataAnnotations;

namespace Shared.EventManagement.Entities;

public enum TagType
{
    Subject,
    Technology,
    Audience,
    Level,
    Other
}

public class Tag
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public TagType Type { get; set; } = TagType.Other;

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }
}
