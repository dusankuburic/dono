using Blazored.LocalStorage;

namespace BuildingManager.Blazor.Services;

public class BuildingSelectionService : IBuildingSelectionService
{
    private const string StorageKey = "selected-building-id";

    private readonly ILocalStorageService _localStorage;
    private readonly IBuildingService _buildingService;

    public BuildingSelectionService(ILocalStorageService localStorage, IBuildingService buildingService)
    {
        _localStorage = localStorage;
        _buildingService = buildingService;
    }

    public async Task<string?> GetSelectedBuildingIdAsync()
    {
        var stored = await _localStorage.GetItemAsync<string>(StorageKey);
        if (!string.IsNullOrWhiteSpace(stored))
        {
            return stored;
        }

        var buildings = await _buildingService.GetBuildingsAsync();
        var first = buildings.FirstOrDefault()?.Id;
        if (first is not null)
        {
            await _localStorage.SetItemAsync(StorageKey, first);
        }

        return first;
    }

    public async Task SetSelectedBuildingIdAsync(string buildingId)
    {
        await _localStorage.SetItemAsync(StorageKey, buildingId);
    }
}
