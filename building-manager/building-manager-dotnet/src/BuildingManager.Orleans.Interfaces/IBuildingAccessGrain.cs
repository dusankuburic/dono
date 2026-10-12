using Orleans;

namespace BuildingManager.Orleans.Interfaces;

public interface IBuildingAccessGrain : IGrainWithStringKey
{
    Task AddMemberAsync(string email);
    Task RemoveMemberAsync(string email);
    Task<List<string>> GetMemberEmailsAsync();
}

[GenerateSerializer]
public record BuildingMember
{
    [Id(0)] public string Email { get; init; } = string.Empty;
    [Id(1)] public string DisplayName { get; init; } = string.Empty;
    [Id(2)] public UserRole Role { get; init; }
}
