using Blazored.LocalStorage;
using BuildingManager.Orleans.Interfaces;

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

        List<BuildingSummary> buildings;
        try
        {
            buildings = await _buildingService.GetBuildingsAsync();
        }
        catch when (!string.IsNullOrWhiteSpace(stored))
        {
            // API unreachable: keep the last choice; pages show their own errors.
            return stored;
        }

        // The stored choice may belong to another account that used this
        // browser, or to a building whose access was revoked: keep it only
        // while it is still one of this user's buildings.
        if (!string.IsNullOrWhiteSpace(stored) && buildings.Any(b => b.Id == stored))
        {
            return stored;
        }

        var first = buildings.FirstOrDefault()?.Id;
        if (first is not null)
        {
            await _localStorage.SetItemAsync(StorageKey, first);
        }
        else
        {
            await _localStorage.RemoveItemAsync(StorageKey);
        }

        return first;
    }

    public async Task SetSelectedBuildingIdAsync(string buildingId)
    {
        await _localStorage.SetItemAsync(StorageKey, buildingId);
    }
}
