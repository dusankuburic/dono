using Orleans;

namespace BuildingManager.Orleans.Interfaces;

/// <summary>
/// Cluster-wide registry of initialized buildings. Building grains register
/// themselves during initialization; this grain is the only way to enumerate
/// buildings because Orleans grains are not queryable by type.
/// </summary>
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
