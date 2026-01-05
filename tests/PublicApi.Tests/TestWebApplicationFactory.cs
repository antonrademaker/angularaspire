using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shared.ApiManagement;
using Shared.Common;
using Shared.EventManagement;
using Shared.Registration;
using Shared.SessionManagement;
using Shared.UserManagement;
using StackExchange.Redis;

namespace PublicApi.Tests;

/// <summary>
/// Custom WebApplicationFactory that configures all DbContexts with InMemory database
/// and mocks external dependencies like Redis for integration testing
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Configure DatabaseOptions to indicate we're using InMemory database
            // This is the recommended approach (vs checking ProviderName at runtime)
            services.Configure<DatabaseOptions>(options =>
            {
                options.UseInMemoryDatabase = true;
            });

            // Remove ALL DbContext-related registrations to avoid provider conflicts
            // This is more aggressive - we remove everything that could conflict
            var descriptorsToRemove = services.Where(d =>
                d.ServiceType == typeof(UserDbContext) ||
                d.ServiceType == typeof(EventDbContext) ||
                d.ServiceType == typeof(SessionDbContext) ||
                d.ServiceType == typeof(RegistrationDbContext) ||
                d.ServiceType == typeof(ApiKeyDbContext) ||
                d.ServiceType == typeof(DbContextOptions<UserDbContext>) ||
                d.ServiceType == typeof(DbContextOptions<EventDbContext>) ||
                d.ServiceType == typeof(DbContextOptions<SessionDbContext>) ||
                d.ServiceType == typeof(DbContextOptions<RegistrationDbContext>) ||
                d.ServiceType == typeof(DbContextOptions<ApiKeyDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                // Remove anything that looks like a Npgsql service
                (d.ServiceType.FullName?.Contains("Npgsql") == true) ||
                (d.ImplementationType?.FullName?.Contains("Npgsql") == true)
            ).ToList();

            foreach (var d in descriptorsToRemove)
            {
                services.Remove(d);
            }

            // Add InMemory DbContexts - use completely isolated service providers
            // to avoid any provider conflicts
            services.AddDbContext<UserDbContext>(options =>
            {
                options.UseInMemoryDatabase($"{_databaseName}_users");
                options.EnableSensitiveDataLogging();
                // Force EF Core to create a new internal service provider
                options.UseInternalServiceProvider(null);
            });

            services.AddDbContext<EventDbContext>(options =>
            {
                options.UseInMemoryDatabase($"{_databaseName}_events");
                options.EnableSensitiveDataLogging();
                options.UseInternalServiceProvider(null);
            });

            services.AddDbContext<SessionDbContext>(options =>
            {
                options.UseInMemoryDatabase($"{_databaseName}_sessions");
                options.EnableSensitiveDataLogging();
                options.UseInternalServiceProvider(null);
            });

            services.AddDbContext<RegistrationDbContext>(options =>
            {
                options.UseInMemoryDatabase($"{_databaseName}_registrations");
                options.EnableSensitiveDataLogging();
                options.UseInternalServiceProvider(null);
            });

            services.AddDbContext<ApiKeyDbContext>(options =>
            {
                options.UseInMemoryDatabase($"{_databaseName}_apikeys");
                options.EnableSensitiveDataLogging();
                options.UseInternalServiceProvider(null);
            });

            // Remove Redis IConnectionMultiplexer and replace with mock
            var redisDescriptors = services.Where(d => d.ServiceType == typeof(IConnectionMultiplexer)).ToList();
            foreach (var d in redisDescriptors)
            {
                services.Remove(d);
            }

            // Create a mock Redis connection that doesn't actually connect
            var mockMultiplexer = new Mock<IConnectionMultiplexer>();
            var mockDatabase = new Mock<IDatabase>();
            var mockBatch = new Mock<IBatch>();

            mockMultiplexer.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(mockDatabase.Object);
            mockMultiplexer.Setup(m => m.IsConnected).Returns(true);
            mockDatabase.Setup(d => d.CreateBatch(It.IsAny<object>())).Returns(mockBatch.Object);

            services.AddSingleton<IConnectionMultiplexer>(mockMultiplexer.Object);

            // Also add ISubscriptionService if missing (used by SubscriptionsController)
            if (!services.Any(d => d.ServiceType == typeof(ISubscriptionService)))
            {
                var mockSubscriptionService = new Mock<ISubscriptionService>();
                services.AddSingleton(mockSubscriptionService.Object);
            }
        });

        builder.UseEnvironment("Development");
    }
}
