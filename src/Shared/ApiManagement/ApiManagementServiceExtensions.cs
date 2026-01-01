using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.ApiManagement;

/// <summary>
/// Extension methods for registering API management services
/// </summary>
public static class ApiManagementServiceExtensions
{
    /// <summary>
    /// Add API management services to the service collection
    /// </summary>
    public static IServiceCollection AddApiManagement(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext
        services.AddDbContext<ApiKeyDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("EventDb");
            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                // Use in-memory for development/testing
                options.UseInMemoryDatabase("ApiKeyDb");
            }
        });

        // Register services
        services.AddScoped<IApiKeyService, ApiKeyService>();

        return services;
    }

    /// <summary>
    /// Add API management services with a specific connection string
    /// </summary>
    public static IServiceCollection AddApiManagement(
        this IServiceCollection services,
        string connectionString)
    {
        // Register DbContext
        services.AddDbContext<ApiKeyDbContext>(options =>
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                options.UseInMemoryDatabase("ApiKeyDb");
            }
        });

        // Register services
        services.AddScoped<IApiKeyService, ApiKeyService>();

        return services;
    }
}
