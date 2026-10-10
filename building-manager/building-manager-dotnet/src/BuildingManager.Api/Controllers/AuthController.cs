using System.ComponentModel.DataAnnotations;
using BuildingManager.Core.Security;
using BuildingManager.Orleans.Interfaces;
using BuildingManager.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    private readonly IGrainFactory _grainFactory;
    private readonly JwtOptions _jwt;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IGrainFactory grainFactory,
        IOptions<JwtOptions> jwt,
        IEmailSender emailSender,
        ILogger<AuthController> logger)
    {
        _grainFactory = grainFactory;
        _jwt = jwt.Value;
        _emailSender = emailSender;
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
    [EnableRateLimiting("auth")]
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

        var profile = await userGrain.GetProfileAsync();
        return StatusCode(StatusCodes.Status201Created, await StartNewSessionAsync(profile));
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
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

        var profile = await userGrain.GetProfileAsync();
        return Ok(await StartNewSessionAsync(profile));
    }

    /// <summary>Exchanges a valid refresh token for a new access token + rotated refresh token.</summary>
    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request)
    {
        var parts = (request.RefreshToken ?? string.Empty).Split(':', 2);
        if (parts.Length != 2 || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1]))
        {
            return Unauthorized(new ProblemDetails { Title = "Invalid refresh token" });
        }

        var (sessionId, secret) = (parts[0], parts[1]);
        var sessionGrain = _grainFactory.GetGrain<IUserSessionGrain>(sessionId);

        UserSession session;
        try
        {
            session = await sessionGrain.GetSessionAsync();
        }
        catch (KeyNotFoundException)
        {
            return Unauthorized(new ProblemDetails { Title = "Invalid refresh token" });
        }

        var newSecret = TokenHasher.Generate();
        var rotated = await sessionGrain.TryRotateRefreshTokenAsync(
            TokenHasher.Hash(secret),
            TokenHasher.Hash(newSecret),
            DateTime.UtcNow.Add(RefreshTokenLifetime));

        if (!rotated)
        {
            return Unauthorized(new ProblemDetails { Title = "Invalid or already-used refresh token" });
        }

        var profile = await _grainFactory.GetGrain<IUserGrain>(session.Email).GetProfileAsync();
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwt.ExpirationMinutes);

        return Ok(new AuthResponse
        {
            Token = CreateAccessToken(profile, sessionId, expiresAt),
            ExpiresAtUtc = expiresAt,
            RefreshToken = $"{sessionId}:{newSecret}",
            UserId = profile.UserId,
            Email = profile.Email,
            DisplayName = profile.DisplayName,
            Role = profile.Role
        });
    }

    /// <summary>Requests a password reset. Always 200 — never reveals whether the account exists.</summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var userGrain = _grainFactory.GetGrain<IUserGrain>(normalizedEmail);
        var resetToken = await userGrain.CreatePasswordResetTokenAsync();

        if (resetToken is not null)
        {
            await _emailSender.SendPasswordResetEmailAsync(normalizedEmail, resetToken);
        }

        return Ok();
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var userGrain = _grainFactory.GetGrain<IUserGrain>(normalizedEmail);

        bool reset;
        try
        {
            reset = await userGrain.ResetPasswordAsync(request.ResetToken, request.NewPassword);
        }
        catch (ArgumentException)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid password", Detail = "Password must be at least 8 characters." });
        }

        if (!reset)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid reset token", Detail = "The token is invalid or has expired." });
        }

        return Ok();
    }

    /// <summary>
    /// Changes the password. Terminates ALL of the user's sessions (including
    /// the current one) — the client should redirect to the login page.
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var email = User.FindFirst("email")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            return Unauthorized();
        }

        try
        {
            await _grainFactory.GetGrain<IUserGrain>(email)
                .ChangePasswordAsync(request.CurrentPassword, request.NewPassword);
        }
        catch (InvalidOperationException)
        {
            return BadRequest(new ProblemDetails { Title = "Wrong password", Detail = "The current password is incorrect." });
        }
        catch (ArgumentException)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid password", Detail = "New password must be at least 8 characters." });
        }

        return Ok();
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
        var email = User.FindFirst("email")?.Value;
        if (!string.IsNullOrEmpty(sessionId))
        {
            await _grainFactory.GetGrain<IUserSessionGrain>(sessionId).TerminateAsync();

            if (!string.IsNullOrEmpty(email))
            {
                await _grainFactory.GetGrain<IUserGrain>(email).UnregisterSessionAsync(sessionId);
            }
        }

        return Ok();
    }

    private async Task<AuthResponse> StartNewSessionAsync(UserAccount profile)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_jwt.ExpirationMinutes);
        var sessionExpiry = now.Add(RefreshTokenLifetime);
        var secret = TokenHasher.Generate();

        var sessionGrain = _grainFactory.GetGrain<IUserSessionGrain>(sessionId);
        await sessionGrain.StartSessionAsync(new UserSession
        {
            UserId = profile.UserId.ToString(),
            Email = profile.Email,
            Role = profile.Role,
            AccessibleBuildingIds = new List<string>(),
            CreatedAt = now,
            LastActivityAt = now,
            // The session lives as long as its refresh token: the access token
            // expiry (60 min) is enforced by the JWT itself.
            ExpiresAt = sessionExpiry
        });
        await sessionGrain.SetRefreshTokenAsync(TokenHasher.Hash(secret));

        // Track the session on the account so a password reset/change can
        // revoke it (and every other device's session).
        await _grainFactory.GetGrain<IUserGrain>(profile.Email).RegisterSessionAsync(sessionId);

        _logger.LogInformation("Issued token for {Email} (role {Role})", profile.Email, profile.Role);

        return new AuthResponse
        {
            Token = CreateAccessToken(profile, sessionId, expiresAt),
            ExpiresAtUtc = expiresAt,
            RefreshToken = $"{sessionId}:{secret}",
            UserId = profile.UserId,
            Email = profile.Email,
            DisplayName = profile.DisplayName,
            Role = profile.Role
        };
    }

    private string CreateAccessToken(UserAccount profile, string sessionId, DateTime expiresAt)
    {
        var now = DateTime.UtcNow;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
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

public record RefreshRequest
{
    [Required]
    public string? RefreshToken { get; init; }
}

public record ForgotPasswordRequest
{
    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; init; } = "";
}

public record ResetPasswordRequest
{
    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; init; } = "";

    [Required]
    public string ResetToken { get; init; } = "";

    [Required, MinLength(8), MaxLength(200)]
    public string NewPassword { get; init; } = "";
}

public record ChangePasswordRequest
{
    [Required, MaxLength(200)]
    public string CurrentPassword { get; init; } = "";

    [Required, MinLength(8), MaxLength(200)]
    public string NewPassword { get; init; } = "";
}

public record CurrentUser
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}
