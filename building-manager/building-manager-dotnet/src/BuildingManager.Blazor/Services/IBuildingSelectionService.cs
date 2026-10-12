namespace BuildingManager.Blazor.Services;

public interface IBuildingSelectionService
{
        Task<string?> GetSelectedBuildingIdAsync();

    Task SetSelectedBuildingIdAsync(string buildingId);
}
