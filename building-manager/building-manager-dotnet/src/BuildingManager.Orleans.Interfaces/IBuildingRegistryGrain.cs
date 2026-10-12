using Orleans;

namespace BuildingManager.Orleans.Interfaces;

public interface IBuildingRegistryGrain : IGrainWithStringKey
{
    Task RegisterAsync(string buildingId, string name);
    Task UnregisterAsync(string buildingId);
    Task<List<BuildingListEntry>> GetAllAsync();
}

[GenerateSerializer]
public record BuildingListEntry
{
    [Id(0)] public string Id { get; init; } = string.Empty;
    [Id(1)] public string Name { get; init; } = string.Empty;
}
