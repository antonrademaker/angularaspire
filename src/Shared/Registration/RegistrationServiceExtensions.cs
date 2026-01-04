using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Shared.Registration;

/// <summary>
/// Extension methods for registering registration services
/// </summary>
public static class RegistrationServiceExtensions
{
    /// <summary>
    /// Register registration services with dependency injection
    /// </summary>
    public static IServiceCollection AddRegistrationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register database context
        services.AddDbContext<RegistrationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Database=EventManagement;Username=dev;Password=dev123;";

            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
            });

            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(true);
        });

        // Register Redis connection for queue management
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var connectionString = configuration.GetConnectionString("Redis")
                ?? "localhost:6379";
            return ConnectionMultiplexer.Connect(connectionString);
        });

        // Register registration service
        services.AddScoped<IRegistrationService, RegistrationService>();

        return services;
    }

    /// <summary>
    /// Ensure registration database is created and migrated
    /// </summary>
    public static async Task<IServiceProvider> EnsureRegistrationDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RegistrationDbContext>();

        await context.Database.EnsureCreatedAsync();

        return serviceProvider;
    }
}