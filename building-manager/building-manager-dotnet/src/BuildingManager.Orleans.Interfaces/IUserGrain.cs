using Orleans;

namespace BuildingManager.Orleans.Interfaces;

/// <summary>
/// A user account, keyed by normalized (lowercased) email. Holds credentials
/// (PBKDF2 hash) and the profile. Registration, login validation and profile
/// lookups all go through this grain.
/// </summary>
public interface IUserGrain : IGrainWithStringKey
{
    /// <summary>Creates the account. Throws InvalidOperationException if it already exists.</summary>
    Task RegisterAsync(RegisterUserRequest request);

    /// <summary>Returns false for unknown accounts and wrong passwords alike (no user enumeration).</summary>
    Task<bool> ValidatePasswordAsync(string password);

    /// <summary>Throws KeyNotFoundException for unknown accounts.</summary>
    Task<UserAccount> GetProfileAsync();

    Task ChangePasswordAsync(string currentPassword, string newPassword);

    /// <summary>
    /// Persistent building access. Unlike the session-scoped list on
    /// IUserSessionGrain, these survive logins and are what the API enforces.
    /// </summary>
    Task GrantBuildingAccessAsync(string buildingId);
    Task RevokeBuildingAccessAsync(string buildingId);
    Task<bool> HasBuildingAccessAsync(string buildingId);
    Task<List<string>> GetAccessibleBuildingIdsAsync();

    /// <summary>
    /// Creates a single-use password reset token valid for one hour.
    /// Returns null for unknown accounts (callers must not reveal which).
    /// Terminates all of the user's active sessions after a successful reset.
    /// </summary>
    Task<string?> CreatePasswordResetTokenAsync();

    /// <summary>Consumes the reset token and sets the new password. False when the token is invalid/expired.</summary>
    Task<bool> ResetPasswordAsync(string resetToken, string newPassword);

    /// <summary>
    /// Active-session index: login registers the session id, logout unregisters
    /// it, and password reset/change terminates every tracked session.
    /// </summary>
    Task RegisterSessionAsync(string sessionId);
    Task UnregisterSessionAsync(string sessionId);
    Task TerminateAllSessionsAsync();
}

[GenerateSerializer]
public record RegisterUserRequest
{
    [Id(0)] public string Email { get; init; } = string.Empty;
    [Id(1)] public string DisplayName { get; init; } = string.Empty;
    [Id(2)] public UserRole Role { get; init; } = UserRole.Owner;
    [Id(3)] public string Password { get; init; } = string.Empty;
}

[GenerateSerializer]
public record UserAccount
{
    [Id(0)] public Guid UserId { get; init; }
    [Id(1)] public string Email { get; init; } = string.Empty;
    [Id(2)] public string DisplayName { get; init; } = string.Empty;
    [Id(3)] public UserRole Role { get; init; }
    [Id(4)] public DateTime CreatedAt { get; init; }
    [Id(5)] public DateTime? LastLoginAt { get; init; }
}

/// <summary>
/// Response of register/login/refresh. Shared between the API and the Blazor
/// client so the client does not have to redeclare the shape.
/// </summary>
[GenerateSerializer]
public record AuthResponse
{
    [Id(0)] public string Token { get; init; } = string.Empty;
    [Id(1)] public DateTime ExpiresAtUtc { get; init; }
    [Id(2)] public Guid UserId { get; init; }
    [Id(3)] public string Email { get; init; } = string.Empty;
    [Id(4)] public string DisplayName { get; init; } = string.Empty;
    [Id(5)] public UserRole Role { get; init; }
    /// <summary>Opaque "sessionId:secret" value; only the hash of the secret is stored server-side.</summary>
    [Id(6)] public string RefreshToken { get; init; } = string.Empty;
}
