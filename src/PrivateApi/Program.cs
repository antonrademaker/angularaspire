using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Shared.UserManagement;
using Shared.EventManagement;
using Shared.SessionManagement;

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

// Add user management services
builder.Services.AddUserManagement();

// Add event management services
builder.Services.AddEventManagement();

// Add session management services
builder.Services.AddSessionManagement(builder.Configuration);

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
    app.MapGet("/api/auth/test", () => new { message = "PrivateApi is running", timestamp = DateTime.UtcNow })
        .WithTags("Health");
}

app.Run();
