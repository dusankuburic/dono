using Orleans;

namespace BuildingManager.Orleans.Interfaces;

public interface IUserGrain : IGrainWithStringKey
{
        Task RegisterAsync(RegisterUserRequest request);

        Task<bool> ValidatePasswordAsync(string password);

        Task<UserAccount> GetProfileAsync();

    Task ChangePasswordAsync(string currentPassword, string newPassword);

        Task GrantBuildingAccessAsync(string buildingId);
    Task RevokeBuildingAccessAsync(string buildingId);
    Task<bool> HasBuildingAccessAsync(string buildingId);
    Task<List<string>> GetAccessibleBuildingIdsAsync();

        Task<string?> CreatePasswordResetTokenAsync();

        Task<bool> ResetPasswordAsync(string resetToken, string newPassword);

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

[GenerateSerializer]
public record AuthResponse
{
    [Id(0)] public string Token { get; init; } = string.Empty;
    [Id(1)] public DateTime ExpiresAtUtc { get; init; }
    [Id(2)] public Guid UserId { get; init; }
    [Id(3)] public string Email { get; init; } = string.Empty;
    [Id(4)] public string DisplayName { get; init; } = string.Empty;
    [Id(5)] public UserRole Role { get; init; }
        [Id(6)] public string RefreshToken { get; init; } = string.Empty;
}
