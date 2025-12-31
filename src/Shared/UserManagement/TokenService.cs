using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Shared.UserManagement;

/// <summary>
/// Service for creating and validating JWT tokens for authentication
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generate JWT token for authenticated user
    /// </summary>
    Task<string> GenerateTokenAsync(User user);

    /// <summary>
    /// Validate JWT token and extract user claims
    /// </summary>
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);

    /// <summary>
    /// Extract user ID from JWT token
    /// </summary>
    Task<Guid?> GetUserIdFromTokenAsync(string token);
}

/// <summary>
/// JWT token service implementation using RSA256 signing
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        
        // Get JWT configuration
        var jwtKey = _configuration["Jwt:Key"] ?? throw new ArgumentException("JWT Key not configured");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? throw new ArgumentException("JWT Issuer not configured");
        var jwtAudience = _configuration["Jwt:Audience"] ?? throw new ArgumentException("JWT Audience not configured");

        // Set up signing credentials
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Set up validation parameters
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes clock skew
        };
    }

    /// <summary>
    /// Generate JWT token with user claims
    /// </summary>
    public Task<string> GenerateTokenAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("status", user.Status.ToString()),
            new("sub", user.Id.ToString()), // Standard JWT subject claim
            new("email_verified", user.EmailVerified.ToString().ToLower()),
            new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique token identifier
        };

        // Add OAuth provider information if available
        if (!string.IsNullOrEmpty(user.OAuthProvider))
        {
            claims.Add(new Claim("oauth_provider", user.OAuthProvider));
        }

        if (!string.IsNullOrEmpty(user.OAuthProviderId))
        {
            claims.Add(new Claim("oauth_provider_id", user.OAuthProviderId));
        }

        // Add timezone if available
        if (!string.IsNullOrEmpty(user.Timezone))
        {
            claims.Add(new Claim("timezone", user.Timezone));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(
                _configuration.GetValue<int>("Jwt:ExpirationHours", 24)), // Default 24 hours
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = _signingCredentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        var token = tokenHandler.WriteToken(securityToken);

        return Task.FromResult(token);
    }

    /// <summary>
    /// Validate JWT token and return claims principal
    /// </summary>
    public Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, _validationParameters, out _);
            return Task.FromResult<ClaimsPrincipal?>(principal);
        }
        catch (SecurityTokenException)
        {
            // Token validation failed
            return Task.FromResult<ClaimsPrincipal?>(null);
        }
        catch (ArgumentException)
        {
            // Invalid token format
            return Task.FromResult<ClaimsPrincipal?>(null);
        }
    }

    /// <summary>
    /// Extract user ID from valid JWT token
    /// </summary>
    public async Task<Guid?> GetUserIdFromTokenAsync(string token)
    {
        var principal = await ValidateTokenAsync(token);
        if (principal == null) return null;

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}