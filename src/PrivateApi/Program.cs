using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PrivateApi.Middleware;
using Shared;
using Shared.ApiManagement;
using Shared.Data;
using Shared.UserManagement;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (Aspire integration)
builder.AddServiceDefaults();

// Add authentication services
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new ArgumentException("JWT Key not configured"));

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

// Add authorization with role-based policies
builder.Services.AddAuthorization(options =>
{
    // Admin-only policy
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole(UserRole.Admin.ToString()));

    // Organizer or Admin policy
    options.AddPolicy("OrganizerOrAdmin", policy =>
        policy.RequireRole(UserRole.Organizer.ToString(), UserRole.Admin.ToString()));

    // Any authenticated user policy
    options.AddPolicy("Authenticated", policy =>
        policy.RequireAuthenticatedUser());
});

// Add all shared services (database + business logic)
builder.Services.AddSharedServices(builder.Configuration);

// Add API key management services
builder.Services.AddApiManagement(builder.Configuration);

// Add CORS for Angular applications
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApps", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200", // PublicApp dev server
                "http://localhost:4201", // PrivateApp dev server
                "https://localhost:4200", // PublicApp dev server HTTPS
                "https://localhost:4201"  // PrivateApp dev server HTTPS
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add controllers and API services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure OpenAPI with JWT authentication
builder.Services.AddOpenApi("v1", openApi =>
{
    openApi.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "Event Management Private API",
            Version = "v1.0",
            Description = "Administrative API for event management system with JWT authentication"
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Apply database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<AppDbContext>();
    
    try
    {
        // In development, check if database reset is requested
        var resetDb = builder.Configuration.GetValue<bool>("ResetDatabase");
        if (app.Environment.IsDevelopment() && resetDb)
        {
            logger.LogWarning("Resetting database as requested by configuration");
            await context.Database.EnsureDeletedAsync();
            logger.LogInformation("Database deleted successfully");
        }
        
        // Apply consolidated database migrations
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            logger.LogInformation("Applying {Count} pending migrations", pendingMigrations.Count());
            await context.Database.MigrateAsync();
            logger.LogInformation("Migrations applied successfully");
        }
        else
        {
            logger.LogInformation("Database is up to date");
        }
        
        logger.LogInformation("Database setup completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while setting up the database");
        
        // In development, provide helpful guidance
        if (app.Environment.IsDevelopment())
        {
            logger.LogError("To reset the database, set 'ResetDatabase=true' in appsettings.Development.json or environment variables");
            logger.LogError("Or manually drop the database and restart the application");
        }
        
        throw;
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Security middleware
app.UseHttpsRedirection();
app.UseCors("AngularApps");
app.UseAuthentication();
app.UseAuthorization();

// File upload validation middleware
app.UseFileUploadValidation();

// Map default health checks
app.MapDefaultEndpoints();

// Map detailed health check endpoint for Angular admin dashboard
app.MapGet("/api/health/detailed", async (IServiceProvider services) =>
{
    var healthCheckService = services.GetRequiredService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();
    var report = await healthCheckService.CheckHealthAsync();

    var response = new
    {
        status = report.Status.ToString(),
        totalDuration = report.TotalDuration.TotalMilliseconds,
        timestamp = DateTime.UtcNow,
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            duration = e.Value.Duration.TotalMilliseconds,
            description = e.Value.Description,
            exception = e.Value.Exception?.Message,
            data = e.Value.Data
        })
    };

    return report.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy
        ? Results.Ok(response)
        : Results.Json(response, statusCode: 503);
}).WithTags("Health").AllowAnonymous();

// Map controllers
app.MapControllers();

// Test endpoint (remove in production)
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/auth/test", () => new { message = "PrivateApi is running", timestamp = DateTime.UtcNow })
        .WithTags("Health");
}

app.Run();
