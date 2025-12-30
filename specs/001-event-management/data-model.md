# Data Model: Event Management System

**Date**: 2025-12-30  
**Feature**: Event Management System  
**Phase**: 1 - Data Model Design

## Core Entities

### Event Aggregate Root
```csharp
public class Event
{
    public Guid Id { get; init; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EventType Type { get; set; }
    public EventStatus Status { get; set; }
    
    // Scheduling
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Utc;
    
    // Location (supports hybrid events)
    public Location? PhysicalLocation { get; set; }
    public VirtualLocation? VirtualLocation { get; set; }
    
    // Registration settings
    public RegistrationSettings RegistrationSettings { get; set; } = new();
    
    // Organizational
    public List<Organizer> Organizers { get; init; } = new();
    public List<Track> Tracks { get; init; } = new();
    public List<SocialEvent> SocialEvents { get; init; } = new();
    
    // Metadata (volatile - designed for frequent changes)
    public Dictionary<string, object> Metadata { get; init; } = new();
    
    // Audit
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedBy { get; init; }
    public Guid LastUpdatedBy { get; set; }
    
    // Domain invariants
    public void ValidateEventDates()
    {
        if (StartDate >= EndDate)
            throw new DomainException("Event start date must be before end date");
    }
}

public enum EventType
{
    Conference,
    Workshop,
    Webinar,
    Meetup,
    Training,
    Hybrid
}

public enum EventStatus
{
    Draft,
    Published,
    RegistrationOpen,
    RegistrationClosed,
    InProgress,
    Completed,
    Cancelled,
    Archived
}
```

### Registration Aggregate Root
```csharp
public class Registration
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    
    // Registration details
    public RegistrationStatus Status { get; set; }
    public RegistrationType Type { get; set; }
    public DateTime RegisteredAt { get; init; }
    
    // Session selection
    public List<SessionRegistration> SessionRegistrations { get; init; } = new();
    
    // Social events
    public List<SocialEventRegistration> SocialEventRegistrations { get; init; } = new();
    
    // Custom fields (volatile - varies by event)
    public Dictionary<string, object> CustomFields { get; init; } = new();
    
    // Audit
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }
    
    // Business rules
    public void ConfirmRegistration()
    {
        if (Status != RegistrationStatus.Pending)
            throw new DomainException("Only pending registrations can be confirmed");
        
        Status = RegistrationStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void CancelRegistration(string reason)
    {
        if (Status == RegistrationStatus.Cancelled)
            return; // Idempotent
            
        Status = RegistrationStatus.Cancelled;
        CustomFields["CancellationReason"] = reason;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum RegistrationStatus
{
    Pending,
    Confirmed,
    CheckedIn,
    NoShow,
    Cancelled,
    Waitlisted
}

public enum RegistrationType
{
    Speaker,
    Attendee,
    VIP,
    Staff,
    Sponsor
}
```

### Session Aggregate Root  
```csharp
public class Session
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public Guid TrackId { get; init; }
    
    // Session details
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SessionType Type { get; set; }
    public SessionFormat Format { get; set; }
    
    // Scheduling
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? RoomName { get; set; }
    public int MaxCapacity { get; set; }
    
    // Speakers
    public List<SessionSpeaker> Speakers { get; init; } = new();
    
    // Prerequisites and target audience
    public SkillLevel RequiredSkillLevel { get; set; }
    public List<string> Prerequisites { get; init; } = new();
    public List<string> Tags { get; init; } = new();
    
    // Resources
    public List<SessionResource> Resources { get; init; } = new();
    
    // Attendance tracking
    public int RegisteredCount { get; private set; }
    public int CheckedInCount { get; private set; }
    
    // Metadata (volatile)
    public Dictionary<string, object> Metadata { get; init; } = new();
    
    // Audit
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }
    
    // Business rules
    public void ValidateScheduling(List<Session> existingSessions)
    {
        if (StartTime >= EndTime)
            throw new DomainException("Session start time must be before end time");
            
        var conflicts = existingSessions
            .Where(s => s.TrackId == TrackId && 
                       s.Id != Id &&
                       SessionsOverlap(s.StartTime, s.EndTime, StartTime, EndTime))
            .ToList();
            
        if (conflicts.Any())
            throw new DomainException($"Session conflicts with {conflicts.Count} other sessions in the same track");
    }
    
    private static bool SessionsOverlap(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
    {
        return start1 < end2 && start2 < end1;
    }
}

public enum SessionType
{
    Presentation,
    Workshop,
    Panel,
    Keynote,
    BreakoutSession,
    Demo,
    QnA
}

public enum SessionFormat
{
    InPerson,
    Virtual,
    Hybrid
}

public enum SkillLevel
{
    Beginner,
    Intermediate,
    Advanced,
    Expert
}
```

### User Entity (Reference)
```csharp
public class User
{
    public Guid Id { get; init; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? JobTitle { get; set; }
    public UserType Type { get; set; }
    
    // Profile (volatile - preferences change frequently)
    public Dictionary<string, object> Profile { get; init; } = new();
    
    // Preferences
    public NotificationPreferences NotificationPreferences { get; set; } = new();
    
    // Audit
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    public string FullName => $"{FirstName} {LastName}".Trim();
}

public enum UserType
{
    Attendee,
    Speaker,
    Organizer,
    Admin,
    Sponsor
}
```

## Supporting Value Objects

### Location Value Objects
```csharp
public record Location(
    string Name,
    string Address,
    string? City,
    string? StateProvince,
    string? Country,
    string? PostalCode,
    double? Latitude,
    double? Longitude);

public record VirtualLocation(
    string Platform,
    string AccessLink,
    string? AccessInstructions,
    Dictionary<string, object> PlatformSettings);
```

### Registration Settings Value Object
```csharp
public record RegistrationSettings(
    DateTime? OpenDate,
    DateTime? CloseDate,
    int? MaxCapacity,
    bool RequireApproval,
    bool AllowWaitlist,
    decimal? RegistrationFee,
    string Currency,
    Dictionary<string, object> CustomFieldDefinitions);
```

### Notification Preferences Value Object
```csharp
public record NotificationPreferences(
    bool EmailNotifications,
    bool SmsNotifications,
    bool PushNotifications,
    List<NotificationType> EnabledNotifications);

public enum NotificationType
{
    EventReminder,
    SessionReminder,
    EventUpdates,
    SocialEventInvitations,
    NewsletterSubscription,
    SpeakerAnnouncements
}
```

## Relationships and Foreign Keys

### Many-to-Many Relationships
```csharp
// Session Registration (Registration -> Session)
public class SessionRegistration
{
    public Guid RegistrationId { get; init; }
    public Guid SessionId { get; init; }
    public SessionRegistrationStatus Status { get; set; }
    public DateTime RegisteredAt { get; init; }
    public DateTime? CheckedInAt { get; set; }
}

// Session Speakers (Session -> User)
public class SessionSpeaker
{
    public Guid SessionId { get; init; }
    public Guid SpeakerId { get; init; }
    public SpeakerRole Role { get; set; }
    public string? Biography { get; set; }
    public int DisplayOrder { get; set; }
}

// Event Organizers (Event -> User)
public class Organizer
{
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public OrganizerRole Role { get; set; }
    public string? ContactInformation { get; set; }
}
```

## Entity Validation Rules

### Event Validation
- Title: Required, max 200 characters
- StartDate: Must be in the future for new events
- EndDate: Must be after StartDate
- MaxCapacity: Must be positive if specified
- At least one organizer required

### Registration Validation
- UserId: Must exist and be active
- EventId: Must exist and have open registration
- Cannot register for cancelled events
- Cannot exceed event capacity (unless waitlist enabled)
- Custom field validation based on event configuration

### Session Validation
- Title: Required, max 200 characters
- StartTime/EndTime: Must be within event date range
- MaxCapacity: Must be positive
- Cannot schedule overlapping sessions in same track/room
- At least one speaker required for presentations

## State Transitions

### Event Lifecycle
```
Draft -> Published -> RegistrationOpen -> RegistrationClosed -> InProgress -> Completed
  |                                                              ^
  v                                                              |
Cancelled <- <- <- <- <- <- <- <- <- <- <- <- <- <- <- <- <- <- +
  |
  v
Archived
```

### Registration Lifecycle  
```
Pending -> Confirmed -> CheckedIn
    |          |           |
    |          v           v
    +----> Cancelled <- NoShow
    |
    v
Waitlisted -> Confirmed (when capacity available)
```

### Session Status Transitions
```
Draft -> Scheduled -> InProgress -> Completed
  |          |            |          |
  v          v            v          v
Cancelled <- + <- <- <- Cancelled <- +
```

This data model supports:
- **Volatility**: Metadata dictionaries allow flexible schema evolution
- **Business boundaries**: Clear aggregate roots with consistent validation rules  
- **Performance**: Efficient queries through proper indexing opportunities
- **Auditability**: Full change tracking for compliance requirements
- **Scalability**: Normalized structure supports high-volume event scenarios