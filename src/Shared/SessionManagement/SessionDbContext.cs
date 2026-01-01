using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.EventManagement;
using Shared.UserManagement;

namespace Shared.SessionManagement;

/// <summary>
/// Entity Framework DbContext for session management
/// Manages tracks, sessions, subscriptions, and speaker relationships
/// </summary>
public class SessionDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of SessionDbContext
    /// </summary>
    /// <param name="options">DbContext options</param>
    public SessionDbContext(DbContextOptions<SessionDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// DbSet for Track entities
    /// </summary>
    public DbSet<Track> Tracks => Set<Track>();

    /// <summary>
    /// DbSet for Session entities
    /// </summary>
    public DbSet<Session> Sessions => Set<Session>();

    /// <summary>
    /// DbSet for Subscription entities
    /// </summary>
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    /// <summary>
    /// DbSet for SessionSpeaker relationship entities
    /// </summary>
    public DbSet<SessionSpeaker> SessionSpeakers => Set<SessionSpeaker>();

    /// <summary>
    /// Configures the database schema and relationships
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Check if we're using InMemory provider (for testing)
        var isInMemory = Database.IsInMemory();

        // Configure Event entity (referenced from Session/Track) - ignore JsonDocument for InMemory
        if (isInMemory)
        {
            modelBuilder.Entity<Event>(entity =>
            {
                entity.Ignore(e => e.CustomFields);
                entity.Ignore(e => e.Tags);
            });
        }

        // Configure Track entity
        modelBuilder.Entity<Track>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => new { t.EventId, t.Slug }).IsUnique();
            entity.HasIndex(t => t.DisplayOrder);
            entity.HasIndex(t => t.IsActive);

            // Relationships
            entity.HasOne<Event>()
                .WithMany()
                .HasForeignKey(t => t.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(t => t.Sessions)
                .WithOne(s => s.Track)
                .HasForeignKey(s => s.TrackId)
                .OnDelete(DeleteBehavior.SetNull);

            // JSON column configurations - only for PostgreSQL, ignore for InMemory
            if (isInMemory)
            {
                entity.Ignore(t => t.Tags);
                entity.Ignore(t => t.CustomFields);
            }
            else
            {
                entity.Property(t => t.Tags)
                    .HasColumnType("jsonb");

                entity.Property(t => t.CustomFields)
                    .HasColumnType("jsonb");
            }
        });

        // Configure Session entity
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => new { s.EventId, s.Slug }).IsUnique();
            entity.HasIndex(s => s.StartTime);
            entity.HasIndex(s => s.Status);
            entity.HasIndex(s => s.IsPublished);
            entity.HasIndex(s => new { s.TrackId, s.StartTime });

            // Relationships
            entity.HasOne(s => s.Event)
                .WithMany()
                .HasForeignKey(s => s.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Track)
                .WithMany(t => t.Sessions)
                .HasForeignKey(s => s.TrackId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(s => s.CreatedByUser)
                .WithMany()
                .HasForeignKey(s => s.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(s => s.Subscriptions)
                .WithOne(sub => sub.Session)
                .HasForeignKey(sub => sub.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(s => s.SessionSpeakers)
                .WithOne(ss => ss.Session)
                .HasForeignKey(ss => ss.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // JSON column configurations - only for PostgreSQL, ignore for InMemory
            if (isInMemory)
            {
                entity.Ignore(s => s.Tags);
                entity.Ignore(s => s.CustomFields);
            }
            else
            {
                entity.Property(s => s.Tags)
                    .HasColumnType("jsonb");

                entity.Property(s => s.CustomFields)
                    .HasColumnType("jsonb");
            }

            // Check constraints - only for PostgreSQL (InMemory doesn't support them)
            if (!isInMemory)
            {
                entity.HasCheckConstraint("CK_Session_EndTimeAfterStartTime", 
                    "end_time > start_time");
                entity.HasCheckConstraint("CK_Session_MaxAttendeesPositive", 
                    "max_attendees IS NULL OR max_attendees > 0");
                entity.HasCheckConstraint("CK_Session_CurrentAttendeesNonNegative", 
                    "current_attendees >= 0");
            }
        });

        // Configure Subscription entity
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => new { s.SessionId, s.UserId }).IsUnique();
            entity.HasIndex(s => s.CreatedAt);
            entity.HasIndex(s => s.Status);

            // Relationships
            entity.HasOne(s => s.Session)
                .WithMany(sess => sess.Subscriptions)
                .HasForeignKey(s => s.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure SessionSpeaker entity
        modelBuilder.Entity<SessionSpeaker>(entity =>
        {
            entity.HasKey(ss => ss.Id);
            entity.HasIndex(ss => new { ss.SessionId, ss.SpeakerId }).IsUnique();
            entity.HasIndex(ss => ss.DisplayOrder);

            // Relationships
            entity.HasOne(ss => ss.Session)
                .WithMany(s => s.SessionSpeakers)
                .HasForeignKey(ss => ss.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Note: SpeakerProfile relationship will be configured when implemented in T063
        });
    }

    /// <summary>
    /// Configures the database connection and options
    /// </summary>
    /// <param name="optionsBuilder">The options builder</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // This will be overridden by DI configuration
            optionsBuilder.UseNpgsql();
        }
    }
}

/// <summary>
/// Extension methods for SessionDbContext DI registration
/// </summary>
public static class SessionDbContextExtensions
{
    /// <summary>
    /// Adds SessionDbContext to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddSessionDbContext(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string is not configured");

        services.AddDbContext<SessionDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_Session");
            });

            // Enable detailed logging in development
            if (configuration.GetValue<bool>("Logging:EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
            }

            if (configuration.GetValue<bool>("Logging:EnableDetailedErrors"))
            {
                options.EnableDetailedErrors();
            }
        });

        return services;
    }

    /// <summary>
    /// Adds SessionDbContext for testing with in-memory database
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="databaseName">The in-memory database name</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddSessionDbContextInMemory(
        this IServiceCollection services, 
        string? databaseName = null)
    {
        services.AddDbContext<SessionDbContext>(options =>
        {
            options.UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString());
        });

        return services;
    }
}