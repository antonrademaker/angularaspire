using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace EventManagement.ServiceDefaults;

/// <summary>
/// Extensions for adding Event Management specific health checks.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Adds comprehensive health checks for the Event Management system.
    /// </summary>
    public static IHealthChecksBuilder AddEventManagementHealthChecks(
        this IHealthChecksBuilder builder,
        string? postgresConnectionString = null,
        string? redisConnectionString = null)
    {
        // PostgreSQL health check
        if (!string.IsNullOrEmpty(postgresConnectionString))
        {
            builder.AddNpgSql(
                connectionString: postgresConnectionString,
                name: "postgresql",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "db", "postgres", "ready" });
        }

        // Redis health check
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            builder.AddRedis(
                redisConnectionString: redisConnectionString,
                name: "redis",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "cache", "redis", "ready" });
        }

        // Memory health check
        builder.AddCheck<MemoryHealthCheck>(
            "memory",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "memory", "live" });

        return builder;
    }

    /// <summary>
    /// Adds Event Management custom metrics to DI.
    /// </summary>
    public static IServiceCollection AddEventManagementMetrics(this IServiceCollection services)
    {
        services.AddSingleton<EventManagementMetrics>();
        return services;
    }
}

/// <summary>
/// Health check that monitors memory usage.
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    private const long DegradedThresholdBytes = 1024L * 1024L * 1024L; // 1 GB
    private const long UnhealthyThresholdBytes = 2L * 1024L * 1024L * 1024L; // 2 GB

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var allocatedBytes = GC.GetTotalMemory(forceFullCollection: false);

        var data = new Dictionary<string, object>
        {
            { "allocated_bytes", allocatedBytes },
            { "allocated_mb", allocatedBytes / (1024 * 1024) },
            { "gen0_collections", GC.CollectionCount(0) },
            { "gen1_collections", GC.CollectionCount(1) },
            { "gen2_collections", GC.CollectionCount(2) }
        };

        if (allocatedBytes >= UnhealthyThresholdBytes)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Memory usage is critical: {allocatedBytes / (1024 * 1024)} MB",
                data: data));
        }

        if (allocatedBytes >= DegradedThresholdBytes)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Memory usage is high: {allocatedBytes / (1024 * 1024)} MB",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Memory usage is normal: {allocatedBytes / (1024 * 1024)} MB",
            data: data));
    }
}

/// <summary>
/// Health check for checking registration queue depth.
/// </summary>
public class RegistrationQueueHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;
    private const int DegradedThreshold = 100;
    private const int UnhealthyThreshold = 500;

    public RegistrationQueueHealthCheck(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var queueLength = await db.ListLengthAsync("registration_queue");

            var data = new Dictionary<string, object>
            {
                { "queue_length", queueLength }
            };

            if (queueLength >= UnhealthyThreshold)
            {
                return HealthCheckResult.Unhealthy(
                    $"Registration queue is backed up: {queueLength} items",
                    data: data);
            }

            if (queueLength >= DegradedThreshold)
            {
                return HealthCheckResult.Degraded(
                    $"Registration queue is growing: {queueLength} items",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"Registration queue is healthy: {queueLength} items",
                data: data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Failed to check registration queue",
                exception: ex);
        }
    }
}
