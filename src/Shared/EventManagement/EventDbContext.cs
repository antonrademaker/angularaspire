using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Shared.EventManagement.Entities;
using Shared.EventManagement.Services;

namespace Shared.EventManagement;

/// <summary>
/// Entity Framework DbContext for event management with PostgreSQL JSON support
/// </summary>
public class EventDbContext : DbContext
{
    private readonly bool _isInMemory;

    public EventDbContext(DbContextOptions<EventDbContext> options) : base(options)
    {
        // Check if we're using InMemory provider by examining the options extensions
        _isInMemory = options.Extensions.Any(e => e.GetType().Name.Contains("InMemory"));
    }

    /// <summary>
    /// Events table with JSON support for flexible metadata
    /// </summary>
    public DbSet<Event> Events => Set<Event>();

    /// <summary>
    /// Social events table for networking events, meals, and activities
    /// </summary>
    public DbSet<SocialEvent> SocialEvents => Set<SocialEvent>();

    /// <summary>
    /// Social event RSVPs table
    /// </summary>
    public DbSet<SocialEventRsvp> SocialEventRsvps => Set<SocialEventRsvp>();

    public DbSet<EventSeries> EventSeries => Set<EventSeries>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomConfiguration> RoomConfigurations => Set<RoomConfiguration>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionAssignment> SessionAssignments => Set<SessionAssignment>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<EventOrganizer> EventOrganizers => Set<EventOrganizer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Event entity
        modelBuilder.Entity<Event>(entity =>
        {
            if (!_isInMemory) entity.ToTable("events", schema: "event_management");
            entity.HasOne(e => e.Series).WithMany().HasForeignKey(e => e.SeriesId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EventOrganizer>(entity =>
        {
            if (!_isInMemory) entity.ToTable("event_organizers", schema: "event_management");
            entity.HasKey(eo => new { eo.EventId, eo.PersonId });
            entity.HasOne(eo => eo.Event).WithMany().HasForeignKey(eo => eo.EventId);
            entity.HasOne(eo => eo.Person).WithMany().HasForeignKey(eo => eo.PersonId);
        });

        modelBuilder.Entity<EventSeries>(entity =>
        {
            if (!_isInMemory) entity.ToTable("event_series", schema: "event_management");
        });

        // Configure SocialEvent entity
        modelBuilder.Entity<SocialEvent>(entity =>
        {
            // Table configuration - only for PostgreSQL
            if (!_isInMemory)
            {
                entity.ToTable("social_events", schema: "event_management");
            }

            // Primary key
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .IsRequired();

            // Title and identification
            entity.Property(e => e.Title)
                .HasColumnName("title")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Slug)
                .HasColumnName("slug")
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(e => new { e.EventId, e.Slug })
                .IsUnique()
                .HasDatabaseName("ix_social_events_event_slug");

            // Description
            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(2000)
                .IsRequired();

            // Type and status as enums
            entity.Property(e => e.Type)
                .HasColumnName("type")
                .HasConversion<string>()
                .IsRequired();

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .IsRequired();

            // Time configuration
            entity.Property(e => e.StartTime)
                .HasColumnName("start_time")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(e => e.EndTime)
                .HasColumnName("end_time")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            // Ignore computed property
            entity.Ignore(e => e.DurationMinutes);

            // Location
            entity.Property(e => e.Location)
                .HasColumnName("location")
                .HasMaxLength(200);

            entity.Property(e => e.Room)
                .HasColumnName("room")
                .HasMaxLength(100);

            // Capacity and RSVP
            entity.Property(e => e.MaxCapacity)
                .HasColumnName("max_capacity");

            entity.Property(e => e.CurrentRsvpCount)
                .HasColumnName("current_rsvp_count")
                .HasDefaultValue(0);

            entity.Property(e => e.WaitlistCount)
                .HasColumnName("waitlist_count")
                .HasDefaultValue(0);

            entity.Property(e => e.RsvpRequired)
                .HasColumnName("rsvp_required")
                .HasDefaultValue(true);

            entity.Property(e => e.RsvpDeadline)
                .HasColumnName("rsvp_deadline")
                .HasColumnType("timestamp with time zone");

            // Guest settings
            entity.Property(e => e.GuestsAllowed)
                .HasColumnName("guests_allowed")
                .HasDefaultValue(false);

            entity.Property(e => e.MaxGuestsPerAttendee)
                .HasColumnName("max_guests_per_attendee")
                .HasDefaultValue(0);

            // Event details
            entity.Property(e => e.DressCode)
                .HasColumnName("dress_code")
                .HasMaxLength(200);

            entity.Property(e => e.CostPerPerson)
                .HasColumnName("cost_per_person")
                .HasColumnType("decimal(10,2)")
                .HasDefaultValue(0);

            entity.Property(e => e.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .HasDefaultValue("USD");

            entity.Property(e => e.DietaryInfo)
                .HasColumnName("dietary_info")
                .HasMaxLength(1000);

            entity.Property(e => e.Notes)
                .HasColumnName("notes");

            entity.Property(e => e.ImageUrl)
                .HasColumnName("image_url")
                .HasMaxLength(500);

            entity.Property(e => e.DisplayOrder)
                .HasColumnName("display_order")
                .HasDefaultValue(0);

            entity.Property(e => e.IsPublished)
                .HasColumnName("is_published")
                .HasDefaultValue(false);

            // PostgreSQL JSON support - Dictionary<string, object> works with both InMemory and PostgreSQL
            entity.Property(e => e.Tags)
                .HasColumnName("tags")
                .HasColumnType("jsonb");

            entity.Property(e => e.CustomFields)
                .HasColumnName("custom_fields")
                .HasColumnType("jsonb");

            // Audit fields
            entity.Property(e => e.CreatedByUserId)
                .HasColumnName("created_by_user_id");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone");

            // Foreign key to Event
            entity.HasOne(e => e.Event)
                .WithMany()
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_social_events_event");

            // Foreign key to User
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_social_events_created_by_user");

            // Performance indexes
            entity.HasIndex(e => e.EventId)
                .HasDatabaseName("ix_social_events_event_id");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("ix_social_events_status");

            entity.HasIndex(e => e.Type)
                .HasDatabaseName("ix_social_events_type");

            entity.HasIndex(e => e.StartTime)
                .HasDatabaseName("ix_social_events_start_time");

            entity.HasIndex(e => new { e.EventId, e.Status, e.StartTime })
                .HasDatabaseName("ix_social_events_discovery");
        });

        // Configure SocialEventRsvp entity
        modelBuilder.Entity<SocialEventRsvp>(entity =>
        {
            // Table configuration - only for PostgreSQL
            if (!_isInMemory)
            {
                entity.ToTable("social_event_rsvps", schema: "event_management");
            }

            // Primary key
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id)
                .HasColumnName("id")
                .IsRequired();

            // Foreign keys
            entity.Property(r => r.SocialEventId)
                .HasColumnName("social_event_id")
                .IsRequired();

            entity.Property(r => r.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            // Unique constraint - one RSVP per user per social event
            entity.HasIndex(r => new { r.SocialEventId, r.UserId })
                .IsUnique()
                .HasDatabaseName("ix_social_event_rsvps_unique_user");

            // Status as enum
            entity.Property(r => r.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .IsRequired();

            // Guest information
            entity.Property(r => r.GuestCount)
                .HasColumnName("guest_count")
                .HasDefaultValue(0);

            entity.Property(r => r.GuestNames)
                .HasColumnName("guest_names")
                .HasMaxLength(500);

            entity.Property(r => r.DietaryRequirements)
                .HasColumnName("dietary_requirements")
                .HasMaxLength(500);

            entity.Property(r => r.Notes)
                .HasColumnName("notes")
                .HasMaxLength(1000);

            // Waitlist
            entity.Property(r => r.IsWaitlisted)
                .HasColumnName("is_waitlisted")
                .HasDefaultValue(false);

            entity.Property(r => r.WaitlistPosition)
                .HasColumnName("waitlist_position");

            // Timestamps
            entity.Property(r => r.RegisteredAt)
                .HasColumnName("registered_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(r => r.ConfirmedAt)
                .HasColumnName("confirmed_at")
                .HasColumnType("timestamp with time zone");

            entity.Property(r => r.DeclinedAt)
                .HasColumnName("declined_at")
                .HasColumnType("timestamp with time zone");

            entity.Property(r => r.NoShowAt)
                .HasColumnName("no_show_at")
                .HasColumnType("timestamp with time zone");

            entity.Property(r => r.CheckedInAt)
                .HasColumnName("checked_in_at")
                .HasColumnType("timestamp with time zone");

            entity.Property(r => r.IsCheckedIn)
                .HasColumnName("is_checked_in")
                .HasDefaultValue(false);

            // Payment
            entity.Property(r => r.AmountPaid)
                .HasColumnName("amount_paid")
                .HasColumnType("decimal(10,2)");

            entity.Property(r => r.PaymentStatus)
                .HasColumnName("payment_status")
                .HasConversion<string>()
                .HasDefaultValue(PaymentStatus.NotRequired);

            entity.Property(r => r.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone");

            // Foreign key to SocialEvent
            entity.HasOne(r => r.SocialEvent)
                .WithMany(se => se.Rsvps)
                .HasForeignKey(r => r.SocialEventId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_social_event_rsvps_social_event");

            // Foreign key to User
            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_social_event_rsvps_user");

            // Performance indexes
            entity.HasIndex(r => r.SocialEventId)
                .HasDatabaseName("ix_social_event_rsvps_social_event_id");

            entity.HasIndex(r => r.UserId)
                .HasDatabaseName("ix_social_event_rsvps_user_id");

            entity.HasIndex(r => r.Status)
                .HasDatabaseName("ix_social_event_rsvps_status");

            entity.HasIndex(r => r.RegisteredAt)
                .HasDatabaseName("ix_social_event_rsvps_registered_at");

            entity.HasIndex(r => new { r.SocialEventId, r.Status })
                .HasDatabaseName("ix_social_event_rsvps_event_status");
        });

        modelBuilder.Entity<Location>(entity =>
        {
            if (!_isInMemory) entity.ToTable("locations", schema: "event_management");
            entity.HasMany(l => l.Rooms).WithOne(r => r.Location).HasForeignKey(r => r.LocationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            if (!_isInMemory) entity.ToTable("rooms", schema: "event_management");
            entity.HasMany(r => r.Configurations).WithOne(c => c.Room).HasForeignKey(c => c.RoomId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoomConfiguration>(entity =>
        {
            if (!_isInMemory) entity.ToTable("room_configurations", schema: "event_management");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            if (!_isInMemory) entity.ToTable("tags", schema: "event_management");
        });

        modelBuilder.Entity<Person>(entity =>
        {
            if (!_isInMemory) entity.ToTable("people", schema: "event_management");
        });

        modelBuilder.Entity<TimeSlot>(entity =>
        {
            if (!_isInMemory) entity.ToTable("time_slots", schema: "event_management");
            entity.Property(t => t.Type).HasConversion<string>();
            entity.HasOne(t => t.Event).WithMany().HasForeignKey(t => t.EventId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Session>(entity =>
        {
            if (!_isInMemory) entity.ToTable("sessions", schema: "event_management");
            entity.Property(s => s.Status).HasConversion<string>();
            entity.Property(s => s.Level).HasConversion<string>();
            entity.Property(s => s.SubmissionStatus).HasConversion<string>();
            entity.HasOne(s => s.Event).WithMany().HasForeignKey(s => s.EventId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SessionAssignment>(entity =>
        {
            if (!_isInMemory) entity.ToTable("session_assignments", schema: "event_management");
            entity.HasOne(sa => sa.Session).WithMany(s => s.Assignments).HasForeignKey(sa => sa.SessionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(sa => sa.Track).WithMany().HasForeignKey(sa => sa.TrackId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(sa => sa.TimeSlot).WithMany().HasForeignKey(sa => sa.TimeSlotId).OnDelete(DeleteBehavior.Restrict);
        });

        // Configure JSON conversion for custom fields
        ConfigureJsonSerialization();
    }

    /// <summary>
    /// Configure JSON serialization options for PostgreSQL
    /// </summary>
    private void ConfigureJsonSerialization()
    {
        // Configure JSON serialization options for Entity Framework
        // This ensures proper handling of JsonDocument properties
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Enable sensitive data logging in development
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
        }
    }
}

/// <summary>
/// Extension methods for EventDbContext dependency injection
/// </summary>
public static class EventDbContextExtensions
{
    /// <summary>
    /// Configure EventDbContext with PostgreSQL integration
    /// </summary>
    public static IServiceCollection AddEventManagement(this IServiceCollection services, string connectionName = "eventdb")
    {
        // Add PostgreSQL DbContext - the connection will be configured by Aspire
        services.AddDbContext<EventDbContext>(options =>
        {
            // Connection configuration will be provided by Aspire via DI
        });

        // Register business services
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<ISocialEventService, SocialEventService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IEventSessionService, EventSessionService>();

        return services;
    }
}
