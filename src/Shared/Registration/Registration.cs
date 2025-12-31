using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Shared.EventManagement;
using Shared.UserManagement;

namespace Shared.Registration;

/// <summary>
/// Registration status enumeration for tracking registration lifecycle
/// </summary>
public enum RegistrationStatus
{
    /// <summary>
    /// Registration is pending confirmation (initial state)
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Registration has been confirmed and is active
    /// </summary>
    Confirmed = 1,
    
    /// <summary>
    /// Registration is in queue waiting for availability
    /// </summary>
    Queued = 2,
    
    /// <summary>
    /// Registration has been cancelled by user or system
    /// </summary>
    Cancelled = 3,
    
    /// <summary>
    /// User attended the event (post-event status)
    /// </summary>
    Attended = 4,
    
    /// <summary>
    /// User was no-show for the event
    /// </summary>
    NoShow = 5
}

/// <summary>
/// Registration priority levels for queue processing
/// </summary>
public enum RegistrationPriority
{
    /// <summary>
    /// Standard priority registration
    /// </summary>
    Normal = 0,
    
    /// <summary>
    /// High priority registration (e.g., early bird, VIP)
    /// </summary>
    High = 1,
    
    /// <summary>
    /// Premium priority registration (highest tier)
    /// </summary>
    Premium = 2
}

/// <summary>
/// Represents a user's registration for an event with queue processing support
/// </summary>
[Table("registrations")]
public class Registration
{
    /// <summary>
    /// Unique identifier for the registration
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
    /// Navigation property to the Event
    /// </summary>
    [ForeignKey(nameof(EventId))]
    public virtual Event Event { get; set; } = null!;

    /// <summary>
    /// Foreign key to the User entity
    /// </summary>
    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the User
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Current status of the registration
    /// </summary>
    [Required]
    [Column("status")]
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;

    /// <summary>
    /// Priority level for queue processing
    /// </summary>
    [Required]
    [Column("priority")]
    public RegistrationPriority Priority { get; set; } = RegistrationPriority.Normal;

    /// <summary>
    /// Position in the queue (null if not queued)
    /// </summary>
    [Column("queue_position")]
    public int? QueuePosition { get; set; }

    /// <summary>
    /// When the registration was initially created
    /// </summary>
    [Required]
    [Column("registered_at")]
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the registration was confirmed (null if still pending/queued)
    /// </summary>
    [Column("confirmed_at")]
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>
    /// When the registration was last updated
    /// </summary>
    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the registration expires if not confirmed
    /// </summary>
    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// When the registration was cancelled (null if not cancelled)
    /// </summary>
    [Column("cancelled_at")]
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Reason for cancellation (null if not cancelled)
    /// </summary>
    [Column("cancellation_reason")]
    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Registration confirmation token for email verification
    /// </summary>
    [Column("confirmation_token")]
    [MaxLength(256)]
    public string? ConfirmationToken { get; set; }

    /// <summary>
    /// Whether the registration confirmation email has been sent
    /// </summary>
    [Required]
    [Column("confirmation_email_sent")]
    public bool ConfirmationEmailSent { get; set; } = false;

    /// <summary>
    /// Number of reminder emails sent for this registration
    /// </summary>
    [Required]
    [Column("reminder_emails_sent")]
    public int ReminderEmailsSent { get; set; } = 0;

    /// <summary>
    /// Custom registration data specific to the event (stored as JSONB in PostgreSQL)
    /// Examples: dietary restrictions, accessibility needs, emergency contact, custom form responses
    /// </summary>
    [Column("registration_data", TypeName = "jsonb")]
    public string? RegistrationDataJson { get; set; }

    /// <summary>
    /// Helper property to work with registration data as strongly typed object
    /// </summary>
    [NotMapped]
    public Dictionary<string, object>? RegistrationData
    {
        get
        {
            if (string.IsNullOrWhiteSpace(RegistrationDataJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(RegistrationDataJson);
            }
            catch
            {
                return null;
            }
        }
        set
        {
            if (value == null)
            {
                RegistrationDataJson = null;
            }
            else
            {
                RegistrationDataJson = JsonSerializer.Serialize(value);
            }
        }
    }

    /// <summary>
    /// Notes about the registration (internal use)
    /// </summary>
    [Column("notes")]
    [MaxLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Source of the registration (web, mobile, admin, etc.)
    /// </summary>
    [Column("registration_source")]
    [MaxLength(50)]
    public string? RegistrationSource { get; set; } = "web";

    /// <summary>
    /// IP address where registration was submitted (for fraud prevention)
    /// </summary>
    [Column("ip_address")]
    [MaxLength(45)] // Support IPv6
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the browser/app used for registration
    /// </summary>
    [Column("user_agent")]
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Check if registration is currently active and confirmed
    /// </summary>
    [NotMapped]
    public bool IsConfirmed => Status == RegistrationStatus.Confirmed;

    /// <summary>
    /// Check if registration is in queue
    /// </summary>
    [NotMapped]
    public bool IsQueued => Status == RegistrationStatus.Queued;

    /// <summary>
    /// Check if registration is cancelled
    /// </summary>
    [NotMapped]
    public bool IsCancelled => Status == RegistrationStatus.Cancelled;

    /// <summary>
    /// Check if registration has expired
    /// </summary>
    [NotMapped]
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    /// <summary>
    /// Check if registration can be cancelled
    /// </summary>
    [NotMapped]
    public bool CanBeCancelled => Status is RegistrationStatus.Pending or RegistrationStatus.Confirmed or RegistrationStatus.Queued;

    /// <summary>
    /// Get estimated wait time in queue (in minutes, null if not queued)
    /// </summary>
    [NotMapped]
    public int? EstimatedWaitTimeMinutes
    {
        get
        {
            if (!IsQueued || !QueuePosition.HasValue) return null;
            
            // Simple estimation: assume 2 minutes per position in queue
            // In real implementation, this could be more sophisticated based on historical data
            return QueuePosition.Value * 2;
        }
    }

    /// <summary>
    /// Set registration data for a specific key
    /// </summary>
    /// <param name="key">The data key</param>
    /// <param name="value">The data value</param>
    public void SetRegistrationData(string key, object value)
    {
        var data = RegistrationData ?? new Dictionary<string, object>();
        data[key] = value;
        RegistrationData = data;
    }

    /// <summary>
    /// Get registration data for a specific key
    /// </summary>
    /// <typeparam name="T">Expected return type</typeparam>
    /// <param name="key">The data key</param>
    /// <returns>The value cast to T, or default(T) if not found</returns>
    public T? GetRegistrationData<T>(string key)
    {
        var data = RegistrationData;
        if (data == null || !data.TryGetValue(key, out var value))
            return default(T);

        if (value is JsonElement jsonElement)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
            }
            catch
            {
                return default(T);
            }
        }

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default(T);
        }
    }

    /// <summary>
    /// Confirm the registration
    /// </summary>
    public void Confirm()
    {
        if (Status != RegistrationStatus.Pending && Status != RegistrationStatus.Queued)
            throw new InvalidOperationException($"Cannot confirm registration in status: {Status}");

        Status = RegistrationStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        QueuePosition = null; // Remove from queue
    }

    /// <summary>
    /// Add registration to queue
    /// </summary>
    /// <param name="position">Position in queue</param>
    public void AddToQueue(int position)
    {
        if (Status != RegistrationStatus.Pending)
            throw new InvalidOperationException($"Cannot queue registration in status: {Status}");

        Status = RegistrationStatus.Queued;
        QueuePosition = position;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancel the registration
    /// </summary>
    /// <param name="reason">Optional reason for cancellation</param>
    public void Cancel(string? reason = null)
    {
        if (!CanBeCancelled)
            throw new InvalidOperationException($"Cannot cancel registration in status: {Status}");

        Status = RegistrationStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
        UpdatedAt = DateTime.UtcNow;
        QueuePosition = null;
    }

    /// <summary>
    /// Mark registration as attended
    /// </summary>
    public void MarkAsAttended()
    {
        if (Status != RegistrationStatus.Confirmed)
            throw new InvalidOperationException($"Cannot mark as attended registration in status: {Status}");

        Status = RegistrationStatus.Attended;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark registration as no-show
    /// </summary>
    public void MarkAsNoShow()
    {
        if (Status != RegistrationStatus.Confirmed)
            throw new InvalidOperationException($"Cannot mark as no-show registration in status: {Status}");

        Status = RegistrationStatus.NoShow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update queue position
    /// </summary>
    /// <param name="newPosition">New position in queue</param>
    public void UpdateQueuePosition(int newPosition)
    {
        if (Status != RegistrationStatus.Queued)
            throw new InvalidOperationException($"Cannot update queue position for registration not in queue");

        QueuePosition = newPosition;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Generate a new confirmation token
    /// </summary>
    public void GenerateConfirmationToken()
    {
        ConfirmationToken = Guid.NewGuid().ToString("N");
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Set expiration time for the registration
    /// </summary>
    /// <param name="expirationMinutes">Minutes from now when registration expires</param>
    public void SetExpiration(int expirationMinutes)
    {
        ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
        UpdatedAt = DateTime.UtcNow;
    }
}