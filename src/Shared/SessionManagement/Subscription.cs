using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.SessionManagement;

/// <summary>
/// Subscription entity representing attendee subscriptions to specific sessions
/// Manages capacity control and attendance tracking for sessions
/// </summary>
[Table("subscriptions")]
public class Subscription
{
    /// <summary>
    /// Unique identifier for the subscription
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the session being subscribed to
    /// </summary>
    [Required]
    [Column("session_id")]
    public Guid SessionId { get; set; }

    /// <summary>
    /// Foreign key to the user subscribing to the session
    /// </summary>
    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Subscription status
    /// </summary>
    [Column("status")]
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Confirmed;

    /// <summary>
    /// When the subscription was created
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the subscription was last updated
    /// </summary>
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// When the subscription was cancelled (if applicable)
    /// </summary>
    [Column("cancelled_at")]
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Reason for cancellation (if applicable)
    /// </summary>
    [MaxLength(500)]
    [Column("cancellation_reason")]
    public string? CancellationReason { get; set; }

    /// <summary>
    /// User agent/source of the subscription for analytics
    /// </summary>
    [MaxLength(200)]
    [Column("user_agent")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// IP address of the subscription request for security/analytics
    /// </summary>
    [MaxLength(45)]
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Whether the user actually attended the session
    /// Updated post-event or during the session
    /// </summary>
    [Column("attended")]
    public bool? Attended { get; set; }

    /// <summary>
    /// Attendance check-in time (when user arrived)
    /// </summary>
    [Column("checked_in_at")]
    public DateTime? CheckedInAt { get; set; }

    /// <summary>
    /// Notes about attendance or special requirements
    /// </summary>
    [MaxLength(1000)]
    [Column("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// Whether the subscription is on a waitlist (for over-capacity sessions)
    /// </summary>
    [Column("is_waitlisted")]
    public bool IsWaitlisted { get; set; } = false;

    /// <summary>
    /// Position in the waitlist (if waitlisted)
    /// </summary>
    [Column("waitlist_position")]
    public int? WaitlistPosition { get; set; }

    /// <summary>
    /// When the user was moved from waitlist to confirmed (if applicable)
    /// </summary>
    [Column("waitlist_confirmed_at")]
    public DateTime? WaitlistConfirmedAt { get; set; }

    /// <summary>
    /// Navigation property to the subscribed session
    /// </summary>
    public virtual Session? Session { get; set; }

    // Note: User navigation property will be added when User entity is available
    // public virtual User? User { get; set; }
}

/// <summary>
/// Subscription status enumeration
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>
    /// Subscription is confirmed and active
    /// </summary>
    Confirmed = 0,

    /// <summary>
    /// Subscription is pending approval/confirmation
    /// </summary>
    Pending = 1,

    /// <summary>
    /// User is on the waitlist
    /// </summary>
    Waitlisted = 2,

    /// <summary>
    /// Subscription has been cancelled by user
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Subscription was rejected or denied
    /// </summary>
    Rejected = 4,

    /// <summary>
    /// User attended the session
    /// </summary>
    Attended = 5,

    /// <summary>
    /// User did not show up for the session
    /// </summary>
    NoShow = 6
}
