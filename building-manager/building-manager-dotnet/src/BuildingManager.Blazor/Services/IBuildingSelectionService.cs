namespace BuildingManager.Blazor.Services;

/// <summary>
/// Tracks which building the user is currently working with. The selection is
/// persisted in local storage; when none is stored, the first building from the
/// API is selected automatically.
/// </summary>
public interface IBuildingSelectionService
{
    /// <summary>Returns the selected building id, or null when no buildings exist.</summary>
    Task<string?> GetSelectedBuildingIdAsync();

    Task SetSelectedBuildingIdAsync(string buildingId);
}
