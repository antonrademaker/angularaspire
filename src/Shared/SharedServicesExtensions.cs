using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Data;
using Shared.EventManagement;
using Shared.EventManagement.Services;
using Shared.SessionManagement;
using Shared.UserManagement;

namespace Shared;

/// <summary>
/// Extension methods for registering all shared services with consolidated database context
/// </summary>
public static class SharedServicesExtensions
{
    /// <summary>
    /// Add all shared services including database context and business services
    /// </summary>
    public static IServiceCollection AddSharedServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionName = "eventdb")
    {
        // Add consolidated database context
        services.AddAppDatabase(configuration, connectionName);

        // Register Event Management services
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<ISocialEventService, SocialEventService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IEventSessionService, EventSessionService>();

        // Register User Management services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserService, UserService>();

        // Register Session Management services
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ITrackService, TrackService>();
        services.AddScoped<ISpeakerService, SpeakerService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();

        return services;
    }

    /// <summary>
    /// Add Event Management services only (requires AddAppDatabase to be called first)
    /// </summary>
    public static IServiceCollection AddEventManagementServices(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<ISocialEventService, SocialEventService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IEventSessionService, EventSessionService>();
        return services;
    }

    /// <summary>
    /// Add User Management services only (requires AddAppDatabase to be called first)
    /// </summary>
    public static IServiceCollection AddUserManagementServices(this IServiceCollection services)
    {
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserService, UserService>();
        return services;
    }

    /// <summary>
    /// Add Session Management services only (requires AddAppDatabase to be called first)
    /// </summary>
    public static IServiceCollection AddSessionManagementServices(this IServiceCollection services)
    {
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ITrackService, TrackService>();
        services.AddScoped<ISpeakerService, SpeakerService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        return services;
    }

    /// <summary>
    /// Add shared services for testing with in-memory database
    /// </summary>
    public static IServiceCollection AddSharedServicesInMemory(
        this IServiceCollection services,
        string? databaseName = null)
    {
        // Add in-memory database
        services.AddAppDatabaseInMemory(databaseName);

        // Register all business services
        services.AddEventManagementServices();
        services.AddUserManagementServices();
        services.AddSessionManagementServices();

        return services;
    }
}
