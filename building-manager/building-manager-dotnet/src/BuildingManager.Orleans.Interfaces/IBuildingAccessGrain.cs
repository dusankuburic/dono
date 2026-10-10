using Orleans;

namespace BuildingManager.Orleans.Interfaces;

/// <summary>
/// Reverse index of building access, keyed by building id: which accounts may
/// access the building. The authority is IUserGrain (what the API enforces);
/// UserGrain keeps this index in sync on every grant/revoke so managers can
/// list a building's members without scanning all users.
/// </summary>
public interface IBuildingAccessGrain : IGrainWithStringKey
{
    Task AddMemberAsync(string email);
    Task RemoveMemberAsync(string email);
    Task<List<string>> GetMemberEmailsAsync();
}

/// <summary>A user with access to a building, as listed to its managers.</summary>
[GenerateSerializer]
public record BuildingMember
{
    [Id(0)] public string Email { get; init; } = string.Empty;
    [Id(1)] public string DisplayName { get; init; } = string.Empty;
    [Id(2)] public UserRole Role { get; init; }
}
