using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
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
            !context.User.Identity?.IsAuthenticated == true || 
            context.User.Identity.IsAuthenticated));
});

// Add user management services
builder.Services.AddUserManagement();

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

// Map default health checks
app.MapDefaultEndpoints();

// Map controllers
app.MapControllers();

// Test endpoint (remove in production)
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/health", () => new { message = "PublicApi is running", timestamp = DateTime.UtcNow })
        .WithTags("Health");
}

app.Run();
