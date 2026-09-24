using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;

namespace BuildingManager.Blazor.Services;

public interface IBuildingService
{
    Task<List<BuildingSummary>> GetBuildingsAsync();
    Task<BuildingSummary?> GetBuildingAsync(string buildingId);
    Task InitializeBuildingAsync(string buildingId, BuildingInfoRequest info);
    Task SetFeeStructureAsync(string buildingId, string name, decimal amountPerM2, decimal? fixedAmount);
    Task<IEnumerable<UnitInfo>> GetUnitsAsync(string buildingId);
    Task AddUnitAsync(string buildingId, UnitInfoRequest unit);
    Task<IEnumerable<ResidentInfo>> GetResidentsAsync(string buildingId);
    Task AddResidentAsync(string buildingId, ResidentInfoRequest resident);
    Task<IEnumerable<KeyInfo>> GetKeysAsync(string buildingId);
    Task<IEnumerable<WorkOrderInfo>> GetWorkOrdersAsync(string buildingId, WorkOrderStatus? status = null);
    Task<WorkOrderInfo> CreateWorkOrderAsync(string buildingId, CreateWorkOrderRequest request);
    Task<FinancialSummary> GetFinancialSummaryAsync(string buildingId);
}

public record BuildingInfoRequest
{
    public string Name { get; init; } = "";
    public string Street { get; init; } = "";
    public string StreetNumber { get; init; } = "";
    public string City { get; init; } = "";
    public string? PostalCode { get; init; }
    public int? YearBuilt { get; init; }
    public int TotalFloors { get; init; }
    public bool HasElevator { get; init; }
    public bool HasParking { get; init; }
    public decimal? TotalAreaM2 { get; init; }
}

public record UnitInfoRequest
{
    public string Number { get; init; } = "";
    public int Floor { get; init; }
    public UnitType Type { get; init; }
    public UnitStatus Status { get; init; }
    public decimal AreaM2 { get; init; }
    public decimal OwnershipShare { get; init; }
    public string? Entrance { get; init; }
}

public record ResidentInfoRequest
{
    public string? UnitNumber { get; init; }
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public ResidentRole Role { get; init; }
    public decimal? OwnershipShare { get; init; }
}
