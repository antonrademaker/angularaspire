using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.EventManagement;
using Shared.UserManagement;

namespace Shared.Registration;

/// <summary>
/// Entity Framework DbContext for Registration management with PostgreSQL support
/// Handles registration data, queue processing, and user-event relationships
/// </summary>
public class RegistrationDbContext : DbContext
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<RegistrationDbContext>? _logger;
    private readonly bool _isInMemory;

    /// <summary>
    /// Registrations DbSet
    /// </summary>
    public DbSet<Registration> Registrations { get; set; } = null!;

    public RegistrationDbContext(
        DbContextOptions<RegistrationDbContext> options,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger<RegistrationDbContext>? logger = null)
        : base(options)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
        // Check if we're using InMemory provider by examining the options extensions
        _isInMemory = options.Extensions.Any(e => e.GetType().Name.Contains("InMemory"));
    }

    /// <summary>
    /// Configure the database connection and settings
    /// </summary>
    /// <param name="optionsBuilder">Options builder for configuration</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("DefaultConnection string is not configured");
            }

            optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);

                // Enable advanced PostgreSQL features
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            // Enable sensitive data logging in development
            if (_hostEnvironment.IsDevelopment())
            {
                optionsBuilder
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors()
                    .LogTo(message => _logger?.LogDebug(message));
            }
        }

        base.OnConfiguring(optionsBuilder);
    }

    /// <summary>
    /// Configure entity models, relationships, and database-specific settings
    /// </summary>
    /// <param name="modelBuilder">Model builder for entity configuration</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureRegistration(modelBuilder);
        ConfigureIndexes(modelBuilder);
        ConfigureConstraints(modelBuilder);
        
        // Configure table naming convention (snake_case)
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(entity.GetTableName()?.ToSnakeCase());
            
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.GetColumnName().ToSnakeCase());
            }
        }
    }

    /// <summary>
    /// Configure the Registration entity
    /// </summary>
    private static void ConfigureRegistration(ModelBuilder modelBuilder)
    {
        var registration = modelBuilder.Entity<Registration>();

        // Primary key
        registration.HasKey(r => r.Id);

        // Configure relationships
        registration
            .HasOne(r => r.Event)
            .WithMany() // Event can have many registrations
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        registration
            .HasOne(r => r.User)
            .WithMany() // User can have many registrations
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure JSONB column for registration data
        registration
            .Property(r => r.RegistrationDataJson)
            .HasColumnType("jsonb")
            .IsRequired(false);

        // Configure enum properties
        registration
            .Property(r => r.Status)
            .HasConversion<int>()
            .IsRequired();

        registration
            .Property(r => r.Priority)
            .HasConversion<int>()
            .IsRequired();

        // Configure string length constraints
        registration
            .Property(r => r.ConfirmationToken)
            .HasMaxLength(256);

        registration
            .Property(r => r.RegistrationSource)
            .HasMaxLength(50)
            .HasDefaultValue("web");

        registration
            .Property(r => r.IpAddress)
            .HasMaxLength(45); // IPv6 support

        registration
            .Property(r => r.UserAgent)
            .HasMaxLength(500);

        registration
            .Property(r => r.Notes)
            .HasMaxLength(2000);

        // Configure default values
        registration
            .Property(r => r.RegisteredAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        registration
            .Property(r => r.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        registration
            .Property(r => r.ConfirmationEmailSent)
            .HasDefaultValue(false);

        registration
            .Property(r => r.ReminderEmailsSent)
            .HasDefaultValue(0);

        // Configure computed columns
        registration
            .Ignore(r => r.IsConfirmed)
            .Ignore(r => r.IsQueued)
            .Ignore(r => r.IsCancelled)
            .Ignore(r => r.IsExpired)
            .Ignore(r => r.CanBeCancelled)
            .Ignore(r => r.EstimatedWaitTimeMinutes)
            .Ignore(r => r.RegistrationData);
    }

    /// <summary>
    /// Configure database indexes for optimal query performance
    /// </summary>
    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        var registration = modelBuilder.Entity<Registration>();

        // Composite index for user-event lookups (most common query)
        registration
            .HasIndex(r => new { r.UserId, r.EventId })
            .HasDatabaseName("ix_registrations_user_event")
            .IsUnique(); // Prevent duplicate registrations

        // Index for event-based queries (all registrations for an event)
        registration
            .HasIndex(r => r.EventId)
            .HasDatabaseName("ix_registrations_event_id");

        // Index for user-based queries (all registrations for a user)
        registration
            .HasIndex(r => r.UserId)
            .HasDatabaseName("ix_registrations_user_id");

        // Index for status-based queries (active registrations, queued, etc.)
        registration
            .HasIndex(r => r.Status)
            .HasDatabaseName("ix_registrations_status");

        // Composite index for queue processing (status + priority + queue position)
        registration
            .HasIndex(r => new { r.Status, r.Priority, r.QueuePosition })
            .HasDatabaseName("ix_registrations_queue_processing")
            .HasFilter("status = 2"); // Only for queued registrations

        // Index for date-based queries
        registration
            .HasIndex(r => r.RegisteredAt)
            .HasDatabaseName("ix_registrations_registered_at");

        registration
            .HasIndex(r => r.ConfirmedAt)
            .HasDatabaseName("ix_registrations_confirmed_at")
            .HasFilter("confirmed_at IS NOT NULL");

        // Index for expiration cleanup
        registration
            .HasIndex(r => r.ExpiresAt)
            .HasDatabaseName("ix_registrations_expires_at")
            .HasFilter("expires_at IS NOT NULL");

        // Index for confirmation token lookups
        registration
            .HasIndex(r => r.ConfirmationToken)
            .HasDatabaseName("ix_registrations_confirmation_token")
            .IsUnique()
            .HasFilter("confirmation_token IS NOT NULL");

        // GIN index for JSONB registration data
        registration
            .HasIndex(r => r.RegistrationDataJson)
            .HasDatabaseName("ix_registrations_data_gin")
            .HasMethod("gin")
            .HasFilter("registration_data IS NOT NULL");

        // Partial index for active registrations (most queried status)
        registration
            .HasIndex(r => new { r.EventId, r.UserId })
            .HasDatabaseName("ix_registrations_active_event_user")
            .HasFilter("status IN (0, 1, 2)"); // Pending, Confirmed, Queued
    }

    /// <summary>
    /// Configure database constraints
    /// </summary>
    private static void ConfigureConstraints(ModelBuilder modelBuilder)
    {
        var registration = modelBuilder.Entity<Registration>();

        // Check constraints for business rules
        registration
            .HasCheckConstraint("ck_registrations_queue_position_positive", 
                "queue_position IS NULL OR queue_position > 0");

        registration
            .HasCheckConstraint("ck_registrations_reminder_emails_non_negative", 
                "reminder_emails_sent >= 0");

        registration
            .HasCheckConstraint("ck_registrations_dates_logical", 
                "registered_at <= COALESCE(confirmed_at, registered_at) AND " +
                "registered_at <= updated_at");

        // Ensure queue position is only set for queued registrations
        registration
            .HasCheckConstraint("ck_registrations_queue_position_status", 
                "(status = 2 AND queue_position IS NOT NULL) OR " +
                "(status != 2 AND queue_position IS NULL)");

        // Ensure confirmation token is only set for pending/queued registrations
        registration
            .HasCheckConstraint("ck_registrations_confirmation_token", 
                "(status IN (0, 2) AND confirmation_token IS NOT NULL) OR " +
                "(status NOT IN (0, 2) AND confirmation_token IS NULL)");
    }

    /// <summary>
    /// Save changes with automatic UpdatedAt timestamp management
    /// </summary>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// Save changes asynchronously with automatic UpdatedAt timestamp management
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Update the UpdatedAt timestamp for modified entities
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<Registration>()
            .Where(e => e.State == EntityState.Modified)
            .ToList();

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Get all confirmed registrations for an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmed registrations</returns>
    public async Task<List<Registration>> GetConfirmedRegistrationsAsync(
        Guid eventId, 
        CancellationToken cancellationToken = default)
    {
        return await Registrations
            .Include(r => r.User)
            .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.Confirmed)
            .OrderBy(r => r.ConfirmedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get queued registrations for an event ordered by priority and registration time
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Queued registrations in processing order</returns>
    public async Task<List<Registration>> GetQueuedRegistrationsAsync(
        Guid eventId, 
        CancellationToken cancellationToken = default)
    {
        return await Registrations
            .Include(r => r.User)
            .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.Queued)
            .OrderBy(r => r.Priority) // Higher priority first
            .ThenBy(r => r.RegisteredAt) // Earlier registrations first within same priority
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get expired registrations that need cleanup
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Expired registrations</returns>
    public async Task<List<Registration>> GetExpiredRegistrationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await Registrations
            .Where(r => r.ExpiresAt != null && 
                       r.ExpiresAt < DateTime.UtcNow && 
                       r.Status == RegistrationStatus.Pending)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get registration count for an event by status
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="status">Registration status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of registrations</returns>
    public async Task<int> GetRegistrationCountAsync(
        Guid eventId, 
        RegistrationStatus status, 
        CancellationToken cancellationToken = default)
    {
        return await Registrations
            .Where(r => r.EventId == eventId && r.Status == status)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Find registration by confirmation token
    /// </summary>
    /// <param name="token">Confirmation token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration if found</returns>
    public async Task<Registration?> FindByConfirmationTokenAsync(
        string token, 
        CancellationToken cancellationToken = default)
    {
        return await Registrations
            .Include(r => r.User)
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.ConfirmationToken == token, cancellationToken);
    }
}

/// <summary>
/// Extension methods for string manipulation
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Convert PascalCase to snake_case
    /// </summary>
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        return System.Text.RegularExpressions.Regex.Replace(input, 
            "([a-z0-9])([A-Z])", "$1_$2").ToLower();
    }
}