using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.UserManagement;

/// <summary>
/// Entity Framework DbContext for user management with PostgreSQL
/// </summary>
public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Users table with OAuth integration support
    /// </summary>
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            // Table configuration
            entity.ToTable("users", schema: "user_management");

            // Primary key
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id)
                .HasColumnName("id")
                .IsRequired();

            // Email configuration
            entity.Property(u => u.Email)
                .HasColumnName("email")
                .HasMaxLength(320)
                .IsRequired();

            // Create unique index on email
            entity.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("ix_users_email");

            // Full name
            entity.Property(u => u.FullName)
                .HasColumnName("full_name")
                .HasMaxLength(200)
                .IsRequired();

            // First and last names
            entity.Property(u => u.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(100);

            entity.Property(u => u.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(100);

            // Avatar URL
            entity.Property(u => u.AvatarUrl)
                .HasColumnName("avatar_url")
                .HasMaxLength(500);

            // OAuth provider information
            entity.Property(u => u.OAuthProvider)
                .HasColumnName("oauth_provider")
                .HasMaxLength(50);

            entity.Property(u => u.OAuthProviderId)
                .HasColumnName("oauth_provider_id")
                .HasMaxLength(200);

            // Create unique index on OAuth provider combination
            entity.HasIndex(u => new { u.OAuthProvider, u.OAuthProviderId })
                .IsUnique()
                .HasDatabaseName("ix_users_oauth_provider")
                .HasFilter("oauth_provider IS NOT NULL AND oauth_provider_id IS NOT NULL");

            // User role and status as enums
            entity.Property(u => u.Role)
                .HasColumnName("role")
                .HasConversion<string>() // Store as string in database
                .IsRequired();

            entity.Property(u => u.Status)
                .HasColumnName("status")
                .HasConversion<string>() // Store as string in database
                .IsRequired();

            // Timestamps
            entity.Property(u => u.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(u => u.LastLoginAt)
                .HasColumnName("last_login_at")
                .HasColumnType("timestamp with time zone");

            // Boolean flags
            entity.Property(u => u.EmailVerified)
                .HasColumnName("email_verified")
                .HasDefaultValue(false)
                .IsRequired();

            entity.Property(u => u.NotificationPreferences)
                .HasColumnName("notification_preferences")
                .HasDefaultValue(true)
                .IsRequired();

            // Timezone
            entity.Property(u => u.Timezone)
                .HasColumnName("timezone")
                .HasMaxLength(50);

            // Indexes for performance
            entity.HasIndex(u => u.Role)
                .HasDatabaseName("ix_users_role");

            entity.HasIndex(u => u.Status)
                .HasDatabaseName("ix_users_status");

            entity.HasIndex(u => u.CreatedAt)
                .HasDatabaseName("ix_users_created_at");

            entity.HasIndex(u => u.LastLoginAt)
                .HasDatabaseName("ix_users_last_login_at");
        });

        // Configure relationships (these will be added as we create related entities)
        // Note: Navigation properties are configured when the related entities are defined
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Enable sensitive data logging in development
        if (!optionsBuilder.IsConfigured)
        {
            // This will be overridden by DI configuration in actual apps
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
        }
    }
}

/// <summary>
/// Extension methods for UserDbContext dependency injection
/// </summary>
public static class UserDbContextExtensions
{
    /// <summary>
    /// Configure UserDbContext with PostgreSQL integration
    /// </summary>
    public static IServiceCollection AddUserManagement(this IServiceCollection services, string connectionName = "eventdb")
    {
        // Add PostgreSQL DbContext - the connection will be configured by Aspire
        services.AddDbContext<UserDbContext>(options =>
        {
            // Connection configuration will be provided by Aspire via DI
        });

        // Register TokenService
        services.AddScoped<ITokenService, TokenService>();

        // Register UserService
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
