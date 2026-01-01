using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Shared.UserManagement;

namespace Shared.SessionManagement;

/// <summary>
/// SpeakerProfile entity representing speaker information and metadata
/// Links to User entity for identity, with extended speaker-specific details
/// </summary>
[Table("speaker_profiles")]
public class SpeakerProfile
{
    /// <summary>
    /// Unique identifier for the speaker profile
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the User entity
    /// </summary>
    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Speaker's display name (may differ from User.FullName)
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Speaker's professional title or role
    /// </summary>
    [MaxLength(150)]
    [Column("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Speaker's company or organization
    /// </summary>
    [MaxLength(200)]
    [Column("company")]
    public string? Company { get; set; }

    /// <summary>
    /// Short biographical summary (plain text)
    /// </summary>
    [MaxLength(500)]
    [Column("short_bio")]
    public string? ShortBio { get; set; }

    /// <summary>
    /// Full biography with optional HTML/Markdown formatting
    /// </summary>
    [Column("full_bio")]
    public string? FullBio { get; set; }

    /// <summary>
    /// Profile photo URL
    /// </summary>
    [MaxLength(500)]
    [Column("photo_url")]
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Contact email for speaker communications (may differ from User.Email)
    /// </summary>
    [MaxLength(320)]
    [EmailAddress]
    [Column("contact_email")]
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Contact phone number
    /// </summary>
    [MaxLength(30)]
    [Column("phone_number")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Speaker's website URL
    /// </summary>
    [MaxLength(500)]
    [Column("website_url")]
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Social media links stored as JSON
    /// Example: { "twitter": "@handle", "linkedin": "profile-url", "github": "username" }
    /// </summary>
    [Column("social_links", TypeName = "jsonb")]
    public JsonDocument? SocialLinks { get; set; }

    /// <summary>
    /// Areas of expertise/topics as JSON array
    /// Example: ["Cloud Computing", "AI/ML", "DevOps"]
    /// </summary>
    [Column("expertise_areas", TypeName = "jsonb")]
    public JsonDocument? ExpertiseAreas { get; set; }

    /// <summary>
    /// Preferred session formats (Talk, Workshop, Panel, etc.)
    /// </summary>
    [MaxLength(200)]
    [Column("preferred_session_types")]
    public string? PreferredSessionTypes { get; set; }

    /// <summary>
    /// Speaker's availability notes or constraints
    /// </summary>
    [MaxLength(500)]
    [Column("availability_notes")]
    public string? AvailabilityNotes { get; set; }

    /// <summary>
    /// Whether this speaker profile is publicly visible
    /// </summary>
    [Column("is_public")]
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// Whether the speaker is currently active/accepting engagements
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Speaker status in the system
    /// </summary>
    [Column("status")]
    public SpeakerStatus Status { get; set; } = SpeakerStatus.Active;

    /// <summary>
    /// Custom fields for extensibility (JSONB column)
    /// </summary>
    [Column("custom_fields", TypeName = "jsonb")]
    public JsonDocument? CustomFields { get; set; }

    /// <summary>
    /// When the speaker profile was created
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time the speaker profile was updated
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// Navigation to the associated User entity
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// Sessions where this speaker is presenting
    /// </summary>
    public virtual ICollection<SessionSpeaker> SessionAssignments { get; set; } = [];
}

/// <summary>
/// Speaker status within the system
/// </summary>
public enum SpeakerStatus
{
    /// <summary>
    /// Active speaker - visible and can be assigned to sessions
    /// </summary>
    Active = 0,

    /// <summary>
    /// Pending approval - awaiting organizer review
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Inactive - not currently accepting engagements
    /// </summary>
    Inactive = 2,

    /// <summary>
    /// Suspended - temporarily restricted
    /// </summary>
    Suspended = 3,

    /// <summary>
    /// Archived - historical record only
    /// </summary>
    Archived = 4
}
