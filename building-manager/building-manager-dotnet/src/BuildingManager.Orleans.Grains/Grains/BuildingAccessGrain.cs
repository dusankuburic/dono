using BuildingManager.Orleans.Interfaces;
using Orleans;
using Orleans.Runtime;

namespace BuildingManager.Orleans.Grains;

public class BuildingAccessGrain : Grain, IBuildingAccessGrain
{
    private readonly IPersistentState<BuildingAccessState> _state;

    public BuildingAccessGrain(
        [PersistentState("buildingAccess", "buildingStorage")]
        IPersistentState<BuildingAccessState> state)
    {
        _state = state;
    }

    public async Task AddMemberAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));

        if (_state.State.MemberEmails.Add(email))
        {
            await _state.WriteStateAsync();
        }
    }

    public async Task RemoveMemberAsync(string email)
    {
        if (_state.State.MemberEmails.Remove(email))
        {
            await _state.WriteStateAsync();
        }
    }

    public Task<List<string>> GetMemberEmailsAsync()
    {
        return Task.FromResult(_state.State.MemberEmails.OrderBy(e => e).ToList());
    }
}

[GenerateSerializer]
public class BuildingAccessState
{
    [Id(0)] public HashSet<string> MemberEmails { get; set; } = new();
}
