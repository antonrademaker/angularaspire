using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.EventManagement;
using Shared.EventManagement.Entities;

namespace Shared.SessionManagement;

/// <summary>
/// Track entity representing a thematic track within a multi-track event
/// Tracks organize sessions by topic, technology, or audience level
/// Examples: "Frontend", "Backend", "DevOps", "Beginner Track", "Advanced Track"
/// </summary>
[Table("tracks")]
public class Track
{
    /// <summary>
    /// Unique identifier for the track
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the Event entity
    /// </summary>
    [Required]
    [Column("event_id")]
    public Guid EventId { get; set; }

    /// <summary>
    /// Track name/title (e.g., "Frontend Development", "AI & Machine Learning")
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Brief description of the track theme and target audience
    /// </summary>
    [Required]
    [MaxLength(1000)]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Detailed track information with HTML formatting support
    /// </summary>
    [Column("detailed_description")]
    public string? DetailedDescription { get; set; }

    /// <summary>
    /// Track slug for SEO-friendly URLs (e.g., "frontend-development")
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Track color/theme for UI visualization (hex color code)
    /// </summary>
    [MaxLength(7)]
    [Column("color")]
    public string? Color { get; set; }

    /// <summary>
    /// Track icon class or URL for UI display
    /// </summary>
    [MaxLength(500)]
    [Column("icon")]
    public string? Icon { get; set; }

    /// <summary>
    /// Display order for track listing
    /// </summary>
    [Column("display_order")]
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Whether this track is currently active and accepting sessions
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Maximum number of concurrent sessions in this track (for scheduling)
    /// </summary>
    [Column("max_concurrent_sessions")]
    public int? MaxConcurrentSessions { get; set; }

    /// <summary>
    /// Target audience level for this track
    /// </summary>
    [Column("audience_level")]
    public TrackAudienceLevel AudienceLevel { get; set; } = TrackAudienceLevel.All;

    /// <summary>
    /// Track category for grouping and filtering
    /// </summary>
    [Column("category")]
    public TrackCategory Category { get; set; } = TrackCategory.Technical;

    /// <summary>
    /// Tags for track categorization and filtering (stored as JSON)
    /// Examples: { "tag1": "javascript", "tag2": "react" }
    /// </summary>
    [Column("tags", TypeName = "jsonb")]
    public string? Tags { get; set; }

    /// <summary>
    /// Flexible JSON metadata for track-specific data and customization
    /// Examples: sponsor information, special requirements, custom attributes
    /// </summary>
    [Column("custom_fields", TypeName = "jsonb")]
    public string? CustomFields { get; set; }

    /// <summary>
    /// User who created this track (organizer or admin)
    /// </summary>
    [Required]
    [Column("created_by_user_id")]
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// When the track record was created
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the track record was last updated
    /// </summary>
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to the parent event
    /// </summary>
    public virtual Event? Event { get; set; }

    /// <summary>
    /// Navigation property to the user who created this track
    /// </summary>
    public virtual UserManagement.User? CreatedByUser { get; set; }

    /// <summary>
    /// Navigation property to sessions in this track
    /// </summary>
    public virtual ICollection<Session> Sessions { get; set; } = [];
}

/// <summary>
/// Track audience level for targeting appropriate content
/// </summary>
public enum TrackAudienceLevel
{
    /// <summary>
    /// Content suitable for all experience levels
    /// </summary>
    All = 0,

    /// <summary>
    /// Content for beginners and newcomers
    /// </summary>
    Beginner = 1,

    /// <summary>
    /// Content for intermediate practitioners
    /// </summary>
    Intermediate = 2,

    /// <summary>
    /// Content for advanced professionals
    /// </summary>
    Advanced = 3,

    /// <summary>
    /// Content for experts and thought leaders
    /// </summary>
    Expert = 4
}

/// <summary>
/// Track category for organization and filtering
/// </summary>
public enum TrackCategory
{
    /// <summary>
    /// Technical sessions covering development, architecture, tools
    /// </summary>
    Technical = 0,

    /// <summary>
    /// Business and strategy focused content
    /// </summary>
    Business = 1,

    /// <summary>
    /// Design, UX/UI, and creative content
    /// </summary>
    Design = 2,

    /// <summary>
    /// Soft skills, career development, leadership
    /// </summary>
    SoftSkills = 3,

    /// <summary>
    /// Industry trends, innovation, future outlook
    /// </summary>
    Industry = 4,

    /// <summary>
    /// Hands-on workshops and practical sessions
    /// </summary>
    Workshop = 5,

    /// <summary>
    /// Panel discussions and interviews
    /// </summary>
    Discussion = 6,

    /// <summary>
    /// Networking and social activities
    /// </summary>
    Networking = 7
}
