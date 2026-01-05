using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PublicApi.Hubs;
using PublicApi.Middleware;
using Shared.ApiManagement;
using Shared.EventManagement;
using Shared.Notifications;
using Shared.Registration;
using Shared.UserManagement;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (Aspire integration)
builder.AddServiceDefaults();

// Add authentication services (optional for most endpoints)
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

        // Allow anonymous access to most endpoints
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                // Log authentication failures but don't block requests
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                // Support SignalR authentication via query string token
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// Add authorization with flexible policies
builder.Services.AddAuthorization(options =>
{
    // Authenticated user policy (for registration, etc.)
    options.AddPolicy("Authenticated", policy =>
        policy.RequireAuthenticatedUser());

    // Optional auth policy (allows anonymous or authenticated)
    options.AddPolicy("OptionalAuth", policy =>
        policy.RequireAssertion(context =>
            context.User.Identity?.IsAuthenticated != true ||
            context.User.Identity.IsAuthenticated));
});

// Add user management services
builder.Services.AddUserManagement();

// Add event management services
builder.Services.AddEventManagement();

// Add registration services
builder.Services.AddRegistrationServices(builder.Configuration);

// Add email notification services
builder.Services.AddEmailService(builder.Configuration);

// Add API management services for rate limiting
builder.Services.AddApiManagement(builder.Configuration);

// Add API rate limiting middleware services
builder.Services.AddApiRateLimiting(options =>
{
    // Configure protected paths for external API
    options.ProtectedPaths = ["/api/external"];

    // Configure excluded paths
    options.ExcludedPaths =
    [
        "/api/health",
        "/api/docs",
        "/api/events",        // Public event discovery
        "/api/social-events", // Public social event discovery
        "/openapi",
        "/swagger",
        "/hubs"
    ];
});

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
            .AllowCredentials(); // Required for SignalR
    });
});

// Add SignalR for real-time notifications
builder.Services.AddSignalR(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors = true;
    }

    // Configure message size limits
    options.MaximumReceiveMessageSize = 32 * 1024; // 32KB
    options.StreamBufferCapacity = 10;

    // Client timeout settings
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});

// Add controllers and API services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure OpenAPI for public API
builder.Services.AddOpenApi("v1", openApi =>
{
    openApi.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "Event Management Public API",
            Version = "v1.0",
            Description = "Public API for event discovery, registration, and attendee features"
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

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

// API rate limiting middleware (for external API endpoints)
app.UseApiRateLimiting();

// Map default health checks
app.MapDefaultEndpoints();

// Map controllers
app.MapControllers();

// Map SignalR hubs
app.MapHub<RegistrationHub>("/hubs/registration");

// Test endpoint (remove in production)
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/health", () => new { message = "PublicApi is running", timestamp = DateTime.UtcNow })
        .WithTags("Health");
}

app.Run();
