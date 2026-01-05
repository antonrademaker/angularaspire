using System.ComponentModel.DataAnnotations;

namespace Shared.EventManagement.Entities;

public class Person
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Link to Identity Provider User ID (if applicable)
    public string? UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Bio { get; set; }

    [MaxLength(500)]
    public string? PhotoUrl { get; set; }

    [MaxLength(200)]
    public string? Company { get; set; }

    [MaxLength(200)]
    public string? JobTitle { get; set; }

    [MaxLength(500)]
    public string? LinkedInUrl { get; set; }

    [MaxLength(500)]
    public string? TwitterUrl { get; set; }

    [MaxLength(500)]
    public string? WebsiteUrl { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
