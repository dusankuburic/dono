namespace BuildingManager.Orleans.Interfaces;

using Orleans;

public interface IUserSessionGrain : IGrainWithStringKey
{
        Task StartSessionAsync(UserSession session);

    Task<bool> ValidateSessionAsync();
    Task<UserSession> GetSessionAsync();
    Task UpdateLastActivityAsync();
    Task AddBuildingAccessAsync(string buildingId);
    Task RemoveBuildingAccessAsync(string buildingId);
    Task<IEnumerable<string>> GetAccessibleBuildingsAsync();
    Task TerminateAsync();

        Task SetRefreshTokenAsync(string refreshTokenHash);

        Task<bool> TryRotateRefreshTokenAsync(string oldRefreshTokenHash, string newRefreshTokenHash, DateTime newSessionExpiryUtc);
}

[GenerateSerializer]
public record UserSession
{
    [Id(0)] public string UserId { get; init; } = string.Empty;
    [Id(1)] public string Email { get; init; } = string.Empty;
    [Id(2)] public UserRole Role { get; init; }
    [Id(3)] public List<string> AccessibleBuildingIds { get; init; } = new();
    [Id(4)] public DateTime CreatedAt { get; init; }
    [Id(5)] public DateTime LastActivityAt { get; init; }
    [Id(6)] public DateTime ExpiresAt { get; init; }
}

public enum UserRole { Admin, CompanyAdmin, Manager, SubstituteManager, Owner, Tenant }
