using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.EventManagement.Entities;
using Shared.UserManagement;

namespace Shared.EventManagement;

/// <summary>
/// SocialEvent entity representing networking events, meals, and social activities
/// These are separate from sessions and have their own RSVP management
/// </summary>
[Table("social_events")]
public class SocialEvent
{
    /// <summary>
    /// Unique identifier for the social event
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
    /// Social event title
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the social event
    /// </summary>
    [Required]
    [MaxLength(2000)]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Social event slug for SEO-friendly URLs
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Type of social event
    /// </summary>
    [Column("type")]
    public SocialEventType Type { get; set; } = SocialEventType.Networking;

    /// <summary>
    /// Event start date and time
    /// </summary>
    [Required]
    [Column("start_time")]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Event end date and time
    /// </summary>
    [Required]
    [Column("end_time")]
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Duration in minutes (calculated)
    /// </summary>
    [Column("duration_minutes")]
    public int DurationMinutes => (int)(EndTime - StartTime).TotalMinutes;

    /// <summary>
    /// Location/venue name for the social event
    /// </summary>
    [MaxLength(200)]
    [Column("location")]
    public string? Location { get; set; }

    /// <summary>
    /// Room or specific area within the venue
    /// </summary>
    [MaxLength(100)]
    [Column("room")]
    public string? Room { get; set; }

    /// <summary>
    /// Maximum capacity for RSVPs (null for unlimited)
    /// </summary>
    [Column("max_capacity")]
    public int? MaxCapacity { get; set; }

    /// <summary>
    /// Current number of confirmed RSVPs
    /// </summary>
    [Column("current_rsvp_count")]
    public int CurrentRsvpCount { get; set; } = 0;

    /// <summary>
    /// Number of attendees on the waitlist
    /// </summary>
    [Column("waitlist_count")]
    public int WaitlistCount { get; set; } = 0;

    /// <summary>
    /// Whether RSVP is required for this social event
    /// </summary>
    [Column("rsvp_required")]
    public bool RsvpRequired { get; set; } = true;

    /// <summary>
    /// Deadline for RSVPs
    /// </summary>
    [Column("rsvp_deadline")]
    public DateTime? RsvpDeadline { get; set; }

    /// <summary>
    /// Whether guests are allowed (e.g., plus-ones)
    /// </summary>
    [Column("guests_allowed")]
    public bool GuestsAllowed { get; set; } = false;

    /// <summary>
    /// Maximum number of guests per attendee
    /// </summary>
    [Column("max_guests_per_attendee")]
    public int MaxGuestsPerAttendee { get; set; } = 0;

    /// <summary>
    /// Dress code or attire recommendation
    /// </summary>
    [MaxLength(200)]
    [Column("dress_code")]
    public string? DressCode { get; set; }

    /// <summary>
    /// Cost per person (0 for free events)
    /// </summary>
    [Column("cost_per_person")]
    public decimal CostPerPerson { get; set; } = 0;

    /// <summary>
    /// Currency code for cost (e.g., "USD", "EUR")
    /// </summary>
    [MaxLength(3)]
    [Column("currency")]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Special dietary requirements or menu info
    /// </summary>
    [MaxLength(1000)]
    [Column("dietary_info")]
    public string? DietaryInfo { get; set; }

    /// <summary>
    /// Additional notes or special instructions
    /// </summary>
    [Column("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// Image URL for the social event
    /// </summary>
    [MaxLength(500)]
    [Column("image_url")]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Current status of the social event
    /// </summary>
    [Column("status")]
    public SocialEventStatus Status { get; set; } = SocialEventStatus.Draft;

    /// <summary>
    /// Display order for sorting
    /// </summary>
    [Column("display_order")]
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Whether the social event is published and visible
    /// </summary>
    [Column("is_published")]
    public bool IsPublished { get; set; } = false;

    /// <summary>
    /// Tags for categorization (JSONB column)
    /// </summary>
    [Column("tags", TypeName = "jsonb")]
    public string? Tags { get; set; }

    /// <summary>
    /// Custom fields for extensibility (JSONB column)
    /// </summary>
    [Column("custom_fields", TypeName = "jsonb")]
    public string? CustomFields { get; set; }

    /// <summary>
    /// User who created this social event
    /// </summary>
    [Column("created_by_user_id")]
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// When the social event was created
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the social event was last updated
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// Navigation to the parent Event
    /// </summary>
    public virtual Event? Event { get; set; }

    /// <summary>
    /// Navigation to the user who created this social event
    /// </summary>
    public virtual User? CreatedByUser { get; set; }

    /// <summary>
    /// Collection of RSVPs for this social event
    /// </summary>
    public virtual ICollection<SocialEventRsvp> Rsvps { get; set; } = [];
}

/// <summary>
/// Types of social events
/// </summary>
public enum SocialEventType
{
    /// <summary>
    /// General networking event
    /// </summary>
    Networking = 0,

    /// <summary>
    /// Breakfast event
    /// </summary>
    Breakfast = 1,

    /// <summary>
    /// Lunch event
    /// </summary>
    Lunch = 2,

    /// <summary>
    /// Dinner event
    /// </summary>
    Dinner = 3,

    /// <summary>
    /// Cocktail reception or happy hour
    /// </summary>
    CocktailReception = 4,

    /// <summary>
    /// Coffee break or refreshments
    /// </summary>
    CoffeeBreak = 5,

    /// <summary>
    /// Team building activity
    /// </summary>
    TeamBuilding = 6,

    /// <summary>
    /// Party or celebration
    /// </summary>
    Party = 7,

    /// <summary>
    /// Tour or excursion
    /// </summary>
    Tour = 8,

    /// <summary>
    /// Other type of social event
    /// </summary>
    Other = 99
}

/// <summary>
/// Social event status
/// </summary>
public enum SocialEventStatus
{
    /// <summary>
    /// Draft - not yet published
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Published and accepting RSVPs
    /// </summary>
    Published = 1,

    /// <summary>
    /// RSVPs are closed
    /// </summary>
    RsvpClosed = 2,

    /// <summary>
    /// Event is in progress
    /// </summary>
    InProgress = 3,

    /// <summary>
    /// Event has completed
    /// </summary>
    Completed = 4,

    /// <summary>
    /// Event has been cancelled
    /// </summary>
    Cancelled = 5
}
