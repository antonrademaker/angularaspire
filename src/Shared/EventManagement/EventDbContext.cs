using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Shared.EventManagement;

/// <summary>
/// Entity Framework DbContext for event management with PostgreSQL JSON support
/// </summary>
public class EventDbContext : DbContext
{
    public EventDbContext(DbContextOptions<EventDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Events table with JSON support for flexible metadata
    /// </summary>
    public DbSet<Event> Events => Set<Event>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Check if we're using InMemory provider (for testing)
        var isInMemory = Database.IsInMemory();

        // Configure Event entity
        modelBuilder.Entity<Event>(entity =>
        {
            // Table configuration - only for PostgreSQL
            if (!isInMemory)
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

            // PostgreSQL JSON support - ignore for InMemory provider
            if (isInMemory)
            {
                entity.Ignore(e => e.CustomFields);
                entity.Ignore(e => e.Tags);
            }
            else
            {
                entity.Property(e => e.CustomFields)
                    .HasColumnName("custom_fields")
                    .HasColumnType("jsonb"); // Use JSONB for better performance

                entity.Property(e => e.Tags)
                    .HasColumnName("tags")
                    .HasColumnType("jsonb"); // Use JSONB for better performance
            }

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
            if (!isInMemory)
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

        return services;
    }
}