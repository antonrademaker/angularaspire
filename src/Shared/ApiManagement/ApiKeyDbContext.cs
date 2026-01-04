using Microsoft.EntityFrameworkCore;

namespace Shared.ApiManagement;

/// <summary>
/// Database context for API key management
/// </summary>
public class ApiKeyDbContext : DbContext
{
    public ApiKeyDbContext(DbContextOptions<ApiKeyDbContext> options)
        : base(options)
    {
    }

    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<ApiKeyUsageLog> ApiKeyUsageLogs => Set<ApiKeyUsageLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // API Key configuration
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.ToTable("api_keys");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.KeyHash)
                .HasColumnName("key_hash")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.KeyPrefix)
                .HasColumnName("key_prefix")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(500);

            entity.Property(e => e.Tier)
                .HasColumnName("tier")
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(e => e.OrganizationName)
                .HasColumnName("organization_name")
                .HasMaxLength(100);

            entity.Property(e => e.Scopes)
                .HasColumnName("scopes")
                .HasMaxLength(500);

            entity.Property(e => e.AllowedIpAddresses)
                .HasColumnName("allowed_ip_addresses")
                .HasMaxLength(1000);

            entity.Property(e => e.AllowedOrigins)
                .HasColumnName("allowed_origins")
                .HasMaxLength(1000);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at");

            entity.Property(e => e.ExpiresAt)
                .HasColumnName("expires_at");

            entity.Property(e => e.LastUsedAt)
                .HasColumnName("last_used_at");

            entity.Property(e => e.TotalRequests)
                .HasColumnName("total_requests")
                .HasDefaultValue(0);

            entity.Property(e => e.CurrentWindowRequests)
                .HasColumnName("current_window_requests")
                .HasDefaultValue(0);

            entity.Property(e => e.CurrentWindowStart)
                .HasColumnName("current_window_start");

            entity.Property(e => e.WebhooksEnabled)
                .HasColumnName("webhooks_enabled")
                .HasDefaultValue(false);

            entity.Property(e => e.WebhookUrl)
                .HasColumnName("webhook_url")
                .HasMaxLength(500);

            entity.Property(e => e.WebhookSecret)
                .HasColumnName("webhook_secret")
                .HasMaxLength(100);

            entity.Property(e => e.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb");

            // Indexes
            entity.HasIndex(e => e.KeyHash)
                .IsUnique()
                .HasDatabaseName("ix_api_keys_key_hash");

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("ix_api_keys_user_id");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("ix_api_keys_status");

            entity.HasIndex(e => e.Tier)
                .HasDatabaseName("ix_api_keys_tier");

            // Foreign key to User (no navigation to avoid circular dependency)
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // API Key Usage Log configuration
        modelBuilder.Entity<ApiKeyUsageLog>(entity =>
        {
            entity.ToTable("api_key_usage_logs");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.ApiKeyId)
                .HasColumnName("api_key_id")
                .IsRequired();

            entity.Property(e => e.Endpoint)
                .HasColumnName("endpoint")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.HttpMethod)
                .HasColumnName("http_method")
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(e => e.StatusCode)
                .HasColumnName("status_code")
                .IsRequired();

            entity.Property(e => e.IpAddress)
                .HasColumnName("ip_address")
                .HasMaxLength(50);

            entity.Property(e => e.UserAgent)
                .HasColumnName("user_agent")
                .HasMaxLength(500);

            entity.Property(e => e.ResponseTimeMs)
                .HasColumnName("response_time_ms");

            entity.Property(e => e.Timestamp)
                .HasColumnName("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.ErrorMessage)
                .HasColumnName("error_message")
                .HasMaxLength(1000);

            // Indexes
            entity.HasIndex(e => e.ApiKeyId)
                .HasDatabaseName("ix_api_key_usage_logs_api_key_id");

            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("ix_api_key_usage_logs_timestamp");

            entity.HasIndex(e => new { e.ApiKeyId, e.Timestamp })
                .HasDatabaseName("ix_api_key_usage_logs_api_key_timestamp");

            // Foreign key
            entity.HasOne(e => e.ApiKey)
                .WithMany()
                .HasForeignKey(e => e.ApiKeyId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
