using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;

namespace BuildingManager.Blazor.Services;

public interface IBuildingService
{
    Task<List<BuildingSummary>> GetBuildingsAsync();
    Task<BuildingSummary?> GetBuildingAsync(string buildingId);
    Task InitializeBuildingAsync(string buildingId, BuildingInfoRequest info);
    Task<BuildingInfo> GetBuildingInfoAsync(string buildingId);
    Task UpdateBuildingAsync(string buildingId, BuildingInfoRequest info);

    Task SetFeeStructureAsync(string buildingId, string name, decimal amountPerM2, decimal? fixedAmount, DateTime? effectiveFrom = null);
    Task<List<FeeStructureInfo>> GetFeeStructuresAsync(string buildingId);

    Task<IEnumerable<UnitInfo>> GetUnitsAsync(string buildingId);

    Task AddUnitAsync(string buildingId, UnitInfoRequest unit);
    Task RemoveUnitAsync(string buildingId, string unitNumber);

    Task<IEnumerable<ResidentInfo>> GetResidentsAsync(string buildingId);
    Task AddResidentAsync(string buildingId, ResidentInfoRequest resident);
    Task UpdateResidentAsync(string buildingId, Guid residentId, ResidentInfoRequest resident);
    Task MoveOutResidentAsync(string buildingId, Guid residentId);

    Task<IEnumerable<KeyInfo>> GetKeysAsync(string buildingId);
    Task AddKeyAsync(string buildingId, KeyInfoRequest key);
    Task CheckoutKeyAsync(string buildingId, Guid keyId, KeyCheckoutRequest checkout);
    Task CheckinKeyAsync(string buildingId, Guid keyId, string? condition, string? notes);
    Task SetKeyStatusAsync(string buildingId, Guid keyId, KeyStatus status, string? note);
    Task<List<KeyCheckoutRecord>> GetKeyHistoryAsync(string buildingId, Guid keyId);

    Task<IEnumerable<WorkOrderInfo>> GetWorkOrdersAsync(string buildingId, WorkOrderStatus? status = null);
    Task<WorkOrderInfo> CreateWorkOrderAsync(string buildingId, CreateWorkOrderRequest request);
    Task<WorkOrderInfo> UpdateWorkOrderStatusAsync(string buildingId, Guid workOrderId, WorkOrderStatus status, string? notes);
    Task<FinancialSummary> GetFinancialSummaryAsync(string buildingId);

    Task<List<BuildingMember>> GetMembersAsync(string buildingId);

    Task GrantAccessAsync(string buildingId, string email);

    Task RevokeAccessAsync(string buildingId, string email);
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

public record KeyInfoRequest
{
    public KeyType KeyType { get; init; }
    public string? Identifier { get; init; }
    public string? Description { get; init; }
    public string? UnitNumber { get; init; }
}
