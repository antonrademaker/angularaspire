using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.UserManagement;

namespace Shared.EventManagement;

/// <summary>
/// SocialEventRsvp entity representing RSVPs for social events
/// Tracks attendee responses and guest information
/// </summary>
[Table("social_event_rsvps")]
public class SocialEventRsvp
{
    /// <summary>
    /// Unique identifier for the RSVP
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the social event
    /// </summary>
    [Required]
    [Column("social_event_id")]
    public Guid SocialEventId { get; set; }

    /// <summary>
    /// Foreign key to the user who RSVP'd
    /// </summary>
    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Current RSVP status
    /// </summary>
    [Column("status")]
    public SocialEventRsvpStatus Status { get; set; } = SocialEventRsvpStatus.Registered;

    /// <summary>
    /// Number of additional guests
    /// </summary>
    [Column("guest_count")]
    public int GuestCount { get; set; } = 0;

    /// <summary>
    /// Names of guests (comma-separated or formatted)
    /// </summary>
    [MaxLength(500)]
    [Column("guest_names")]
    public string? GuestNames { get; set; }

    /// <summary>
    /// Dietary restrictions or special requirements
    /// </summary>
    [MaxLength(500)]
    [Column("dietary_requirements")]
    public string? DietaryRequirements { get; set; }

    /// <summary>
    /// Additional notes from the attendee
    /// </summary>
    [MaxLength(1000)]
    [Column("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// Whether attendee is on the waitlist
    /// </summary>
    [Column("is_waitlisted")]
    public bool IsWaitlisted { get; set; } = false;

    /// <summary>
    /// Position in the waitlist (if waitlisted)
    /// </summary>
    [Column("waitlist_position")]
    public int? WaitlistPosition { get; set; }

    /// <summary>
    /// When the RSVP was initially created
    /// </summary>
    [Required]
    [Column("registered_at")]
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the RSVP was confirmed (if applicable)
    /// </summary>
    [Column("confirmed_at")]
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>
    /// When the attendee declined (if applicable)
    /// </summary>
    [Column("declined_at")]
    public DateTime? DeclinedAt { get; set; }

    /// <summary>
    /// When the attendee was marked as no-show (if applicable)
    /// </summary>
    [Column("no_show_at")]
    public DateTime? NoShowAt { get; set; }

    /// <summary>
    /// When the attendee checked in (if applicable)
    /// </summary>
    [Column("checked_in_at")]
    public DateTime? CheckedInAt { get; set; }

    /// <summary>
    /// Whether the attendee has checked in
    /// </summary>
    [Column("is_checked_in")]
    public bool IsCheckedIn { get; set; } = false;

    /// <summary>
    /// Any payment/cost information
    /// </summary>
    [Column("amount_paid")]
    public decimal? AmountPaid { get; set; }

    /// <summary>
    /// Payment status
    /// </summary>
    [Column("payment_status")]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.NotRequired;

    /// <summary>
    /// When the RSVP was last updated
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// Navigation to the social event
    /// </summary>
    public virtual SocialEvent? SocialEvent { get; set; }

    /// <summary>
    /// Navigation to the user who RSVP'd
    /// </summary>
    public virtual User? User { get; set; }
}

/// <summary>
/// RSVP status for social events
/// Matches the SocialEventRegistrationStatus from the API contract
/// </summary>
public enum SocialEventRsvpStatus
{
    /// <summary>
    /// Initial registration submitted
    /// </summary>
    Registered = 0,

    /// <summary>
    /// Attendance confirmed by attendee or admin
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Attendee declined the invitation
    /// </summary>
    Declined = 2,

    /// <summary>
    /// Attendee did not show up
    /// </summary>
    NoShow = 3,

    /// <summary>
    /// RSVP was cancelled
    /// </summary>
    Cancelled = 4
}

/// <summary>
/// Payment status for social events with costs
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// No payment required (free event)
    /// </summary>
    NotRequired = 0,

    /// <summary>
    /// Payment is pending
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Payment has been completed
    /// </summary>
    Paid = 2,

    /// <summary>
    /// Payment has been refunded
    /// </summary>
    Refunded = 3,

    /// <summary>
    /// Payment failed
    /// </summary>
    Failed = 4
}
