using System.ComponentModel.DataAnnotations;
using BuildingManager.Orleans.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Orleans;
using System.Security.Claims;
using System.Text;

namespace BuildingManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IGrainFactory _grainFactory;
    private readonly JwtOptions _jwt;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IGrainFactory grainFactory, IOptions<JwtOptions> jwt, ILogger<AuthController> logger)
    {
        _grainFactory = grainFactory;
        _jwt = jwt.Value;
        _logger = logger;
    }

    /// <summary>Roles that public self-registration may assign. Admin roles are excluded.</summary>
    private static readonly HashSet<UserRole> SelfRegistrationRoles = new()
    {
        UserRole.Owner,
        UserRole.Tenant,
        UserRole.Manager,
        UserRole.SubstituteManager
    };

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        if (!SelfRegistrationRoles.Contains(request.Role))
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid role",
                Detail = $"Role '{request.Role}' cannot be self-assigned."
            });

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var userGrain = _grainFactory.GetGrain<IUserGrain>(normalizedEmail);
        await userGrain.RegisterAsync(new RegisterUserRequest
        {
            Email = normalizedEmail,
            DisplayName = request.DisplayName.Trim(),
            Role = request.Role,
            Password = request.Password
        });

        var response = await IssueTokenAsync(userGrain, normalizedEmail);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var userGrain = _grainFactory.GetGrain<IUserGrain>(normalizedEmail);

        if (!await userGrain.ValidatePasswordAsync(request.Password))
        {
            // Same response for unknown account and wrong password.
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid credentials",
                Detail = "Email or password is incorrect."
            });
        }

        return Ok(await IssueTokenAsync(userGrain, normalizedEmail));
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<CurrentUser> Me()
    {
        return Ok(new CurrentUser
        {
            UserId = User.FindFirst("sub")?.Value ?? string.Empty,
            Email = User.FindFirst("email")?.Value ?? string.Empty,
            DisplayName = User.FindFirst("name")?.Value ?? string.Empty,
            Role = User.FindFirst("role")?.Value ?? string.Empty
        });
    }

    /// <summary>Terminates the session so the token stops working before its expiry.</summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var sessionId = User.FindFirst("jti")?.Value;
        if (!string.IsNullOrEmpty(sessionId))
        {
            var sessionGrain = _grainFactory.GetGrain<IUserSessionGrain>(sessionId);
            await sessionGrain.TerminateAsync();
        }

        return Ok();
    }

    private async Task<AuthResponse> IssueTokenAsync(IUserGrain userGrain, string normalizedEmail)
    {
        var profile = await userGrain.GetProfileAsync();

        var sessionId = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_jwt.ExpirationMinutes);

        var sessionGrain = _grainFactory.GetGrain<IUserSessionGrain>(sessionId);
        await sessionGrain.StartSessionAsync(new UserSession
        {
            UserId = profile.UserId.ToString(),
            Email = profile.Email,
            Role = profile.Role,
            AccessibleBuildingIds = new List<string>(),
            CreatedAt = now,
            LastActivityAt = now,
            ExpiresAt = expiresAt
        });

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            IssuedAt = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret)),
                SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = profile.UserId.ToString(),
                ["email"] = profile.Email,
                ["name"] = profile.DisplayName,
                ["role"] = profile.Role.ToString(),
                ["jti"] = sessionId
            }
        });

        _logger.LogInformation("Issued token for {Email} (role {Role})", normalizedEmail, profile.Role);

        return new AuthResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAt,
            UserId = profile.UserId,
            Email = profile.Email,
            DisplayName = profile.DisplayName,
            Role = profile.Role
        };
    }
}

public class JwtOptions
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "building-manager";
    public string Audience { get; set; } = "building-manager-api";
    public int ExpirationMinutes { get; set; } = 60;
}

public record RegisterRequest
{
    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; init; } = "";

    [Required, MaxLength(200)]
    public string DisplayName { get; init; } = "";

    public UserRole Role { get; init; } = UserRole.Owner;

    [Required, MinLength(8), MaxLength(200)]
    public string Password { get; init; } = "";
}

public record LoginRequest
{
    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; init; } = "";

    [Required, MaxLength(200)]
    public string Password { get; init; } = "";
}

public record CurrentUser
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}
