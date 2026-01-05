using System.ComponentModel.DataAnnotations;

namespace Shared.UserManagement;

/// <summary>
/// User entity with OAuth 2.0 integration support
/// Represents both internal users and external OAuth users
/// </summary>
public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Email address - primary identifier for user accounts
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(320)] // RFC 5321 maximum email length
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Full display name of the user
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// First name extracted from OAuth provider or entered directly
    /// </summary>
    [MaxLength(100)]
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name extracted from OAuth provider or entered directly
    /// </summary>
    [MaxLength(100)]
    public string? LastName { get; set; }

    /// <summary>
    /// User's profile picture URL from OAuth provider or uploaded avatar
    /// </summary>
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// OAuth 2.0 provider identifier (e.g., "google", "microsoft", "github")
    /// Null for direct registration users
    /// </summary>
    [MaxLength(50)]
    public string? OAuthProvider { get; set; }

    /// <summary>
    /// OAuth provider's unique identifier for this user
    /// Null for direct registration users
    /// </summary>
    [MaxLength(200)]
    public string? OAuthProviderId { get; set; }

    /// <summary>
    /// User role within the system (Admin, Organizer, Attendee)
    /// </summary>
    [Required]
    public UserRole Role { get; set; } = UserRole.Attendee;

    /// <summary>
    /// Account status for moderation and access control
    /// </summary>
    [Required]
    public UserStatus Status { get; set; } = UserStatus.Active;

    /// <summary>
    /// When the user account was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time the user logged in or accessed the system
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Email verification status for direct registration users
    /// OAuth users are considered automatically verified
    /// </summary>
    public bool EmailVerified { get; set; } = false;

    /// <summary>
    /// Whether the user has agreed to receive notification emails
    /// </summary>
    public bool NotificationPreferences { get; set; } = true;

    /// <summary>
    /// User's preferred timezone for event scheduling display
    /// </summary>
    [MaxLength(50)]
    public string? Timezone { get; set; }

    // Navigation properties will be added as we implement related entities
    // public virtual ICollection<Registration.Registration> Registrations { get; set; } = [];
    // public virtual ICollection<SessionManagement.Subscription> SessionSubscriptions { get; set; } = [];
    // public virtual ICollection<EventManagement.Event> CreatedEvents { get; set; } = [];
}

/// <summary>
/// User roles within the event management system
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Regular event attendee - can register for events and sessions
    /// </summary>
    Attendee = 0,

    /// <summary>
    /// Event organizer - can create and manage events
    /// </summary>
    Organizer = 1,

    /// <summary>
    /// System administrator - full access to all features
    /// </summary>
    Admin = 2
}

/// <summary>
/// User account status for moderation and access control
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// Active user account with full access
    /// </summary>
    Active = 0,

    /// <summary>
    /// Inactive account - user cannot login
    /// </summary>
    Inactive = 1,

    /// <summary>
    /// Suspended account - temporary restriction
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// Permanently banned account
    /// </summary>
    Banned = 3,

    /// <summary>
    /// Account pending email verification
    /// </summary>
    PendingVerification = 4
}
