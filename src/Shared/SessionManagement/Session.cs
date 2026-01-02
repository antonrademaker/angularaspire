using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.EventManagement;
using Shared.UserManagement;

namespace Shared.SessionManagement;

/// <summary>
/// Session entity representing individual sessions within event tracks
/// Sessions are the atomic units of content delivery (talks, workshops, panels, etc.)
/// </summary>
[Table("sessions")]
public class Session
{
    /// <summary>
    /// Unique identifier for the session
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the parent Event
    /// </summary>
    [Required]
    [Column("event_id")]
    public Guid EventId { get; set; }

    /// <summary>
    /// Foreign key to the Track (optional for single-track events)
    /// </summary>
    [Column("track_id")]
    public Guid? TrackId { get; set; }

    /// <summary>
    /// Session title
    /// </summary>
    [Required]
    [MaxLength(300)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Brief session description
    /// </summary>
    [Required]
    [MaxLength(2000)]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Detailed session abstract with HTML formatting support
    /// </summary>
    [Column("abstract")]
    public string? Abstract { get; set; }

    /// <summary>
    /// Session slug for SEO-friendly URLs
    /// </summary>
    [Required]
    [MaxLength(150)]
    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Session start date and time
    /// </summary>
    [Required]
    [Column("start_time")]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Session end date and time
    /// </summary>
    [Required]
    [Column("end_time")]
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Session duration in minutes (calculated from start/end time)
    /// </summary>
    [Column("duration_minutes")]
    public int DurationMinutes => (int)(EndTime - StartTime).TotalMinutes;

    /// <summary>
    /// Session type/format
    /// </summary>
    [Column("session_type")]
    public SessionType Type { get; set; } = SessionType.Talk;

    /// <summary>
    /// Session difficulty level
    /// </summary>
    [Column("difficulty_level")]
    public SessionDifficulty DifficultyLevel { get; set; } = SessionDifficulty.Intermediate;

    /// <summary>
    /// Maximum number of attendees for this session (null for unlimited)
    /// </summary>
    [Column("max_attendees")]
    public int? MaxAttendees { get; set; }

    /// <summary>
    /// Current number of confirmed session subscriptions
    /// </summary>
    [Column("current_attendees")]
    public int CurrentAttendees { get; set; } = 0;

    /// <summary>
    /// Whether subscription is required for this session
    /// </summary>
    [Column("requires_subscription")]
    public bool RequiresSubscription { get; set; } = false;

    /// <summary>
    /// Physical room/venue for the session
    /// </summary>
    [MaxLength(200)]
    [Column("room")]
    public string? Room { get; set; }

    /// <summary>
    /// Building or venue section
    /// </summary>
    [MaxLength(200)]
    [Column("building")]
    public string? Building { get; set; }

    /// <summary>
    /// Whether this is a virtual/online session
    /// </summary>
    [Column("is_virtual")]
    public bool IsVirtual { get; set; } = false;

    /// <summary>
    /// Virtual meeting/streaming URL for online sessions
    /// </summary>
    [MaxLength(500)]
    [Column("virtual_url")]
    public string? VirtualUrl { get; set; }

    /// <summary>
    /// Recording/livestream URL (if available)
    /// </summary>
    [MaxLength(500)]
    [Column("recording_url")]
    public string? RecordingUrl { get; set; }

    /// <summary>
    /// Session materials/slides URL
    /// </summary>
    [MaxLength(500)]
    [Column("materials_url")]
    public string? MaterialsUrl { get; set; }

    /// <summary>
    /// Session status for lifecycle management
    /// </summary>
    [Column("status")]
    public SessionStatus Status { get; set; } = SessionStatus.Draft;

    /// <summary>
    /// Whether session is currently published and visible
    /// </summary>
    [Column("is_published")]
    public bool IsPublished { get; set; } = false;

    /// <summary>
    /// Whether session allows Q&A
    /// </summary>
    [Column("allow_questions")]
    public bool AllowQuestions { get; set; } = true;

    /// <summary>
    /// Whether session will be recorded
    /// </summary>
    [Column("is_recorded")]
    public bool IsRecorded { get; set; } = false;

    /// <summary>
    /// Language of the session presentation
    /// </summary>
    [MaxLength(10)]
    [Column("language")]
    public string Language { get; set; } = "en";

    /// <summary>
    /// Prerequisites or requirements for attendees
    /// </summary>
    [Column("prerequisites")]
    public string? Prerequisites { get; set; }

    /// <summary>
    /// Key takeaways or learning outcomes
    /// </summary>
    [Column("learning_outcomes")]
    public string? LearningOutcomes { get; set; }

    /// <summary>
    /// Target audience description
    /// </summary>
    [Column("target_audience")]
    public string? TargetAudience { get; set; }

    /// <summary>
    /// Session tags for categorization and filtering (stored as JSON)
    /// Examples: { "tag1": "javascript", "tag2": "react" }
    /// </summary>
    [Column("tags", TypeName = "jsonb")]
    public string? Tags { get; set; }

    /// <summary>
    /// Flexible JSON metadata for session-specific data
    /// Examples: sponsor information, special equipment needs, accessibility notes
    /// </summary>
    [Column("custom_fields", TypeName = "jsonb")]
    public string? CustomFields { get; set; }

    /// <summary>
    /// User who created this session (organizer or admin)
    /// </summary>
    [Required]
    [Column("created_by_user_id")]
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// When the session record was created
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the session record was last updated
    /// </summary>
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to the parent event
    /// </summary>
    public virtual Event? Event { get; set; }

    /// <summary>
    /// Navigation property to the parent track (optional)
    /// </summary>
    public virtual Track? Track { get; set; }

    /// <summary>
    /// Navigation property to the user who created this session
    /// </summary>
    public virtual User? CreatedByUser { get; set; }

    /// <summary>
    /// Navigation property to session subscriptions
    /// </summary>
    public virtual ICollection<Subscription> Subscriptions { get; set; } = [];

    /// <summary>
    /// Navigation property to session speakers
    /// </summary>
    public virtual ICollection<SessionSpeaker> SessionSpeakers { get; set; } = [];
}

/// <summary>
/// Session type/format enumeration
/// </summary>
public enum SessionType
{
    /// <summary>
    /// Traditional presentation/talk
    /// </summary>
    Talk = 0,

    /// <summary>
    /// Hands-on workshop or tutorial
    /// </summary>
    Workshop = 1,

    /// <summary>
    /// Panel discussion with multiple participants
    /// </summary>
    Panel = 2,

    /// <summary>
    /// Q&A session or interview
    /// </summary>
    Interview = 3,

    /// <summary>
    /// Lightning talk (short presentation)
    /// </summary>
    LightningTalk = 4,

    /// <summary>
    /// Keynote presentation
    /// </summary>
    Keynote = 5,

    /// <summary>
    /// Interactive demonstration
    /// </summary>
    Demo = 6,

    /// <summary>
    /// Breakout discussion session
    /// </summary>
    Breakout = 7,

    /// <summary>
    /// Networking break or social activity
    /// </summary>
    Networking = 8,

    /// <summary>
    /// Opening or closing ceremony
    /// </summary>
    Ceremony = 9
}

/// <summary>
/// Session difficulty level enumeration
/// </summary>
public enum SessionDifficulty
{
    /// <summary>
    /// Suitable for beginners
    /// </summary>
    Beginner = 0,

    /// <summary>
    /// Intermediate level content
    /// </summary>
    Intermediate = 1,

    /// <summary>
    /// Advanced professional level
    /// </summary>
    Advanced = 2,

    /// <summary>
    /// Expert level content
    /// </summary>
    Expert = 3
}

/// <summary>
/// Session status enumeration for lifecycle management
/// </summary>
public enum SessionStatus
{
    /// <summary>
    /// Session is being created/edited
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Session is under review
    /// </summary>
    UnderReview = 1,

    /// <summary>
    /// Session has been approved
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Session is published and visible
    /// </summary>
    Published = 3,

    /// <summary>
    /// Session is currently in progress
    /// </summary>
    InProgress = 4,

    /// <summary>
    /// Session has completed
    /// </summary>
    Completed = 5,

    /// <summary>
    /// Session has been cancelled
    /// </summary>
    Cancelled = 6
}

/// <summary>
/// Session-Speaker relationship entity for many-to-many mapping
/// </summary>
[Table("session_speakers")]
public class SessionSpeaker
{
    /// <summary>
    /// Primary key
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to Session
    /// </summary>
    [Required]
    [Column("session_id")]
    public Guid SessionId { get; set; }

    /// <summary>
    /// Foreign key to SpeakerProfile
    /// </summary>
    [Required]
    [Column("speaker_id")]
    public Guid SpeakerId { get; set; }

    /// <summary>
    /// Speaker role in this session
    /// </summary>
    [Column("role")]
    public SpeakerRole Role { get; set; } = SpeakerRole.Speaker;

    /// <summary>
    /// Display order for multiple speakers
    /// </summary>
    [Column("display_order")]
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Navigation property to the session
    /// </summary>
    public virtual Session? Session { get; set; }

    /// <summary>
    /// Navigation property to the speaker profile
    /// </summary>
    public virtual SpeakerProfile? Speaker { get; set; }
}

/// <summary>
/// Speaker role in a session
/// </summary>
public enum SpeakerRole
{
    /// <summary>
    /// Main presenter/speaker
    /// </summary>
    Speaker = 0,

    /// <summary>
    /// Co-presenter
    /// </summary>
    CoSpeaker = 1,

    /// <summary>
    /// Session moderator
    /// </summary>
    Moderator = 2,

    /// <summary>
    /// Panel participant
    /// </summary>
    Panelist = 3,

    /// <summary>
    /// Workshop facilitator
    /// </summary>
    Facilitator = 4
}