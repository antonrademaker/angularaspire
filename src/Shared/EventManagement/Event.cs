using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Shared.EventManagement;

/// <summary>
/// Event entity supporting multi-edition events with flexible JSON metadata
/// Represents conferences, workshops, or other gatherings with sessions and registrations
/// </summary>
public class Event
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Event title/name (e.g., "IlionX Dev Days 2025")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Brief description of the event
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Detailed event description with HTML formatting support
    /// </summary>
    public string? DetailedDescription { get; set; }

    /// <summary>
    /// Event slug for SEO-friendly URLs (e.g., "ilionx-dev-days-2025")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Event start date and time (with timezone support)
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Event end date and time (with timezone support)
    /// </summary>
    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Event timezone (e.g., "Europe/Amsterdam")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Timezone { get; set; } = "UTC";

    /// <summary>
    /// Maximum number of attendees allowed to register
    /// </summary>
    [Required]
    public int MaxAttendees { get; set; }

    /// <summary>
    /// Current number of confirmed registrations
    /// </summary>
    public int CurrentAttendees { get; set; } = 0;

    /// <summary>
    /// Registration opening date and time
    /// </summary>
    public DateTime? RegistrationOpenDate { get; set; }

    /// <summary>
    /// Registration closing date and time
    /// </summary>
    public DateTime? RegistrationCloseDate { get; set; }

    /// <summary>
    /// Event status for managing lifecycle
    /// </summary>
    [Required]
    public EventStatus Status { get; set; } = EventStatus.Draft;

    /// <summary>
    /// Event visibility setting
    /// </summary>
    [Required]
    public EventVisibility Visibility { get; set; } = EventVisibility.Public;

    /// <summary>
    /// Physical venue information
    /// </summary>
    [MaxLength(500)]
    public string? VenueName { get; set; }

    /// <summary>
    /// Venue address
    /// </summary>
    [MaxLength(1000)]
    public string? VenueAddress { get; set; }

    /// <summary>
    /// Whether this is a virtual/online event
    /// </summary>
    public bool IsVirtual { get; set; } = false;

    /// <summary>
    /// Virtual meeting link/URL for online events
    /// </summary>
    [MaxLength(500)]
    public string? VirtualMeetingUrl { get; set; }

    /// <summary>
    /// Event banner/hero image URL
    /// </summary>
    [MaxLength(500)]
    public string? BannerImageUrl { get; set; }

    /// <summary>
    /// Event logo image URL
    /// </summary>
    [MaxLength(500)]
    public string? LogoImageUrl { get; set; }

    /// <summary>
    /// Event website URL
    /// </summary>
    [MaxLength(500)]
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Contact email for event inquiries
    /// </summary>
    [MaxLength(320)]
    [EmailAddress]
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Flexible JSON metadata for custom fields, integrations, and edition-specific data
    /// Examples: social media links, sponsor information, custom registration fields, etc.
    /// </summary>
    public JsonDocument? CustomFields { get; set; }

    /// <summary>
    /// Maximum number of attendees for the event (null for unlimited)
    /// </summary>
    public int? MaxCapacity { get; set; }

    /// <summary>
    /// Tags for categorization and filtering (stored as JSON array)
    /// Examples: ["technology", "conference", "workshops", "networking"]
    /// </summary>
    public JsonDocument? Tags { get; set; }

    /// <summary>
    /// User who created this event (organizer or admin)
    /// </summary>
    [Required]
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// When the event record was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the event record was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to the user who created this event
    /// </summary>
    public virtual UserManagement.User? CreatedByUser { get; set; }

    // Navigation properties (will be enabled as we implement related entities)
    // public virtual ICollection<Registration.Registration> Registrations { get; set; } = [];
    // public virtual ICollection<SessionManagement.Track> Tracks { get; set; } = [];
    // public virtual ICollection<SocialEvent> SocialEvents { get; set; } = [];
}

/// <summary>
/// Event lifecycle status
/// </summary>
public enum EventStatus
{
    /// <summary>
    /// Event is being created/edited - not visible to public
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Event is published and registration is open
    /// </summary>
    Published = 1,

    /// <summary>
    /// Registration has closed but event hasn't started
    /// </summary>
    RegistrationClosed = 2,

    /// <summary>
    /// Event is currently in progress
    /// </summary>
    InProgress = 3,

    /// <summary>
    /// Event has completed
    /// </summary>
    Completed = 4,

    /// <summary>
    /// Event has been cancelled
    /// </summary>
    Cancelled = 5,

    /// <summary>
    /// Event has been postponed
    /// </summary>
    Postponed = 6
}

/// <summary>
/// Event visibility settings
/// </summary>
public enum EventVisibility
{
    /// <summary>
    /// Event is publicly visible and searchable
    /// </summary>
    Public = 0,

    /// <summary>
    /// Event is only visible to users with direct link
    /// </summary>
    Unlisted = 1,

    /// <summary>
    /// Event requires invitation or approval to view
    /// </summary>
    Private = 2
}