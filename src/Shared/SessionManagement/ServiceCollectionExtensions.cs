using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.SessionManagement;

/// <summary>
/// Extension methods for registering SessionManagement services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add SessionManagement services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddSessionManagement(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddSessionDbContext(configuration);

        // Add services
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ITrackService, TrackService>();

        return services;
    }

    /// <summary>
    /// Add SessionManagement services with in-memory database for testing
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="databaseName">Optional database name for testing</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddSessionManagementInMemory(this IServiceCollection services, string? databaseName = null)
    {
        // Add in-memory DbContext
        services.AddSessionDbContextInMemory(databaseName);

        // Add services
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ITrackService, TrackService>();

        return services;
    }
}
