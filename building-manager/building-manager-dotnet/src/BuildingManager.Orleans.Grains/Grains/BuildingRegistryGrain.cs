using BuildingManager.Orleans.Interfaces;
using Orleans;
using Orleans.Runtime;

namespace BuildingManager.Orleans.Grains;

/// <summary>
/// Singleton grain (key "global") tracking all initialized buildings so they
/// can be enumerated. Single key = single activation = no concurrency issues.
/// </summary>
public class BuildingRegistryGrain : Grain, IBuildingRegistryGrain
{
    private readonly IPersistentState<BuildingRegistryState> _state;

    public BuildingRegistryGrain(
        [PersistentState("registry", "buildingStorage")]
        IPersistentState<BuildingRegistryState> state)
    {
        _state = state;
    }

    public async Task RegisterAsync(string buildingId, string name)
    {
        if (string.IsNullOrWhiteSpace(buildingId))
            throw new ArgumentException("Building id is required", nameof(buildingId));

        _state.State.Buildings[buildingId] = name ?? string.Empty;
        await _state.WriteStateAsync();
    }

    public async Task UnregisterAsync(string buildingId)
    {
        if (_state.State.Buildings.Remove(buildingId))
        {
            await _state.WriteStateAsync();
        }
    }

    public Task<List<BuildingListEntry>> GetAllAsync()
    {
        var entries = _state.State.Buildings
            .Select(kv => new BuildingListEntry { Id = kv.Key, Name = kv.Value })
            .OrderBy(e => e.Name)
            .ToList();
        return Task.FromResult(entries);
    }
}

[GenerateSerializer]
public class BuildingRegistryState
{
    [Id(0)] public Dictionary<string, string> Buildings { get; set; } = new();
}
