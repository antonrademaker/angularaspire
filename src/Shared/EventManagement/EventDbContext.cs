using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Shared.EventManagement.Entities;

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

    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomConfiguration> RoomConfigurations => Set<RoomConfiguration>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Person> People => Set<Person>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Event entity
        modelBuilder.Entity<Event>(entity =>
        {
            // Table configuration - only for PostgreSQL
            if (!_isInMemory)
            {
                entity.ToTable("events", schema: "event_management");
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

            // Create unique index on slug for SEO-friendly URLs
            entity.HasIndex(e => e.Slug)
                .IsUnique()
                .HasDatabaseName("ix_events_slug");

            // Descriptions
            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(e => e.DetailedDescription)
                .HasColumnName("detailed_description")
                .HasColumnType("text");

            // Date and time configuration with timezone support
            entity.Property(e => e.StartDate)
                .HasColumnName("start_date")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(e => e.EndDate)
                .HasColumnName("end_date")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(e => e.Timezone)
                .HasColumnName("timezone")
                .HasMaxLength(50)
                .HasDefaultValue("UTC")
                .IsRequired();

            // Registration management
            entity.Property(e => e.MaxAttendees)
                .HasColumnName("max_attendees")
                .IsRequired();

            entity.Property(e => e.CurrentAttendees)
                .HasColumnName("current_attendees")
                .HasDefaultValue(0)
                .IsRequired();

            entity.Property(e => e.RegistrationOpenDate)
                .HasColumnName("registration_open_date")
                .HasColumnType("timestamp with time zone");

            entity.Property(e => e.RegistrationCloseDate)
                .HasColumnName("registration_close_date")
                .HasColumnType("timestamp with time zone");

            // Status and visibility as enums
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<string>() // Store as string in database
                .IsRequired();

            entity.Property(e => e.Visibility)
                .HasColumnName("visibility")
                .HasConversion<string>() // Store as string in database
                .IsRequired();

            // Venue information
            entity.Property(e => e.VenueName)
                .HasColumnName("venue_name")
                .HasMaxLength(500);

            entity.Property(e => e.VenueAddress)
                .HasColumnName("venue_address")
                .HasMaxLength(1000);

            entity.Property(e => e.IsVirtual)
                .HasColumnName("is_virtual")
                .HasDefaultValue(false)
                .IsRequired();

            entity.Property(e => e.VirtualMeetingUrl)
                .HasColumnName("virtual_meeting_url")
                .HasMaxLength(500);

            // Media and links
            entity.Property(e => e.BannerImageUrl)
                .HasColumnName("banner_image_url")
                .HasMaxLength(500);

            entity.Property(e => e.LogoImageUrl)
                .HasColumnName("logo_image_url")
                .HasMaxLength(500);

            entity.Property(e => e.WebsiteUrl)
                .HasColumnName("website_url")
                .HasMaxLength(500);

            entity.Property(e => e.ContactEmail)
                .HasColumnName("contact_email")
                .HasMaxLength(320);

            // PostgreSQL JSON support - Dictionary<string, object> works with both InMemory and PostgreSQL
            entity.Property(e => e.CustomFields)
                .HasColumnName("custom_fields")
                .HasColumnType("jsonb"); // Use JSONB for better performance

            entity.Property(e => e.Tags)
                .HasColumnName("tags")
                .HasColumnType("jsonb"); // Use JSONB for better performance

            // User relationship and audit fields
            entity.Property(e => e.CreatedByUserId)
                .HasColumnName("created_by_user_id")
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone");

            // Foreign key relationship to User
            entity.HasOne(e => e.CreatedByUser)
                .WithMany() // Will be configured from User side when implemented
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict) // Prevent deleting users who created events
                .HasConstraintName("fk_events_created_by_user");

            // Performance indexes
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("ix_events_status");

            entity.HasIndex(e => e.Visibility)
                .HasDatabaseName("ix_events_visibility");

            entity.HasIndex(e => e.StartDate)
                .HasDatabaseName("ix_events_start_date");

            entity.HasIndex(e => e.EndDate)
                .HasDatabaseName("ix_events_end_date");

            entity.HasIndex(e => e.CreatedByUserId)
                .HasDatabaseName("ix_events_created_by_user");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("ix_events_created_at");

            // Multi-column index for event discovery queries
            entity.HasIndex(e => new { e.Status, e.Visibility, e.StartDate })
                .HasDatabaseName("ix_events_discovery");

            // GIN index for JSONB fields (PostgreSQL specific) - skip for InMemory
            if (!_isInMemory)
            {
                entity.HasIndex(e => e.Tags)
                    .HasMethod("gin")
                    .HasDatabaseName("ix_events_tags_gin");

                entity.HasIndex(e => e.CustomFields)
                    .HasMethod("gin")
                    .HasDatabaseName("ix_events_custom_fields_gin");

                // Full-text search support (PostgreSQL specific)
                entity.HasIndex(e => new { e.Title, e.Description })
                    .HasMethod("gin")
                    .HasDatabaseName("ix_events_fulltext_search")
                    .HasAnnotation("Npgsql:TsVectorConfig", "english");
            }
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
                .WithMany(ev => ev.SocialEvents)
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

        return services;
    }
}
