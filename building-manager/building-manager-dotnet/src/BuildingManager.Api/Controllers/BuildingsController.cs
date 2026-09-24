using System.ComponentModel.DataAnnotations;
using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace BuildingManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BuildingsController : ControllerBase
{
    private readonly IGrainFactory _grainFactory;

    public BuildingsController(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    /// <summary>
    /// Lists all initialized buildings by asking the registry grain for their
    /// ids and then fetching each building's summary.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BuildingSummary>>> GetAllBuildings()
    {
        var registry = _grainFactory.GetGrain<IBuildingRegistryGrain>("global");
        var entries = await registry.GetAllAsync();

        var summaries = new List<BuildingSummary>();
        foreach (var entry in entries)
        {
            try
            {
                var grain = _grainFactory.GetGrain<IBuildingGrain>(entry.Id);
                summaries.Add(await grain.GetSummaryAsync());
            }
            catch (KeyNotFoundException)
            {
                // Registered building without persisted state (e.g. wiped
                // storage): skip it rather than failing the whole listing.
            }
        }

        return Ok(summaries);
    }

    [HttpGet("{buildingId}")]
    public async Task<ActionResult<BuildingSummary>> GetBuilding(string buildingId)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var summary = await grain.GetSummaryAsync();
        return Ok(summary);
    }

    [HttpPost("{buildingId}/initialize")]
    public async Task<ActionResult> InitializeBuilding(string buildingId, [FromBody] BuildingInfoRequest request)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var info = new BuildingInfo
        {
            Name = request.Name,
            Street = request.Street,
            StreetNumber = request.StreetNumber,
            City = request.City,
            PostalCode = request.PostalCode,
            YearBuilt = request.YearBuilt,
            TotalFloors = request.TotalFloors,
            HasElevator = request.HasElevator,
            HasParking = request.HasParking,
            TotalAreaM2 = request.TotalAreaM2 ?? 0
        };
        await grain.InitializeAsync(info);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpGet("{buildingId}/units")]
    public async Task<ActionResult<IEnumerable<UnitInfo>>> GetUnits(string buildingId)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var units = await grain.GetAllUnitsAsync();
        return Ok(units);
    }

    [HttpPost("{buildingId}/units")]
    public async Task<ActionResult> AddUnit(string buildingId, [FromBody] UnitInfoRequest request)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var unit = new UnitInfo
        {
            Id = Guid.NewGuid(),
            Number = request.Number,
            Floor = request.Floor,
            Type = request.Type,
            Status = request.Status,
            AreaM2 = request.AreaM2,
            OwnershipShare = request.OwnershipShare,
            Entrance = request.Entrance
        };
        await grain.AddOrUpdateUnitAsync(unit);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpGet("{buildingId}/residents")]
    public async Task<ActionResult<IEnumerable<ResidentInfo>>> GetResidents(string buildingId)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var residents = await grain.GetResidentsAsync();
        return Ok(residents);
    }

    [HttpPost("{buildingId}/residents")]
    public async Task<ActionResult> AddResident(string buildingId, [FromBody] ResidentInfoRequest request)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var resident = new ResidentInfo
        {
            Id = Guid.NewGuid(),
            UnitNumber = request.UnitNumber,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Role = request.Role,
            OwnershipShare = request.OwnershipShare
        };
        await grain.AddResidentAsync(resident);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpGet("{buildingId}/keys")]
    public async Task<ActionResult<IEnumerable<KeyInfo>>> GetKeys(string buildingId)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var keys = await grain.GetAllKeysAsync();
        return Ok(keys);
    }

    [HttpPost("{buildingId}/keys")]
    public async Task<ActionResult> AddKey(string buildingId, [FromBody] KeyInfoRequest request)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var key = new KeyInfo
        {
            Id = Guid.NewGuid(),
            Type = request.KeyType,
            Description = request.Description,
            Identifier = request.Identifier ?? Guid.NewGuid().ToString()[..8],
            Status = KeyStatus.Available
        };
        await grain.AddKeyAsync(key);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("{buildingId}/keys/{keyId}/checkout")]
    public async Task<ActionResult<KeyInfo>> CheckoutKey(string buildingId, Guid keyId, [FromBody] KeyCheckoutRequest request)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var result = await grain.CheckoutKeyAsync(keyId, request);
        return Ok(result);
    }

    [HttpPost("{buildingId}/keys/{keyId}/checkin")]
    public async Task<ActionResult<KeyInfo>> CheckinKey(string buildingId, Guid keyId, [FromBody] KeyCheckinRequest request)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var result = await grain.CheckinKeyAsync(keyId, request.Condition, request.Notes);
        return Ok(result);
    }

    [HttpGet("{buildingId}/workorders")]
    public async Task<ActionResult<IEnumerable<WorkOrderInfo>>> GetWorkOrders(string buildingId, [FromQuery] WorkOrderStatus? status)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var workOrders = await grain.GetWorkOrdersAsync(status);
        return Ok(workOrders);
    }

    [HttpPost("{buildingId}/workorders")]
    public async Task<ActionResult<WorkOrderInfo>> CreateWorkOrder(string buildingId, [FromBody] CreateWorkOrderRequest request)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var result = await grain.CreateWorkOrderAsync(request);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPatch("{buildingId}/workorders/{workOrderId}/status")]
    public async Task<ActionResult<WorkOrderInfo>> UpdateWorkOrderStatus(
        string buildingId,
        Guid workOrderId,
        [FromBody] UpdateWorkOrderStatusRequest request)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var result = await grain.UpdateWorkOrderStatusAsync(workOrderId, request.Status, request.Notes);
        return Ok(result);
    }

    [HttpGet("{buildingId}/announcements")]
    public async Task<ActionResult<IEnumerable<AnnouncementInfo>>> GetAnnouncements(string buildingId)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var announcements = await grain.GetAnnouncementsAsync();
        return Ok(announcements);
    }

    [HttpPost("{buildingId}/announcements")]
    public async Task<ActionResult> CreateAnnouncement(string buildingId, [FromBody] CreateAnnouncementRequest request)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var announcement = new AnnouncementInfo
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content,
            Type = request.Type,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.CreatedBy
        };
        await grain.CreateAnnouncementAsync(announcement);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpGet("{buildingId}/finance/summary")]
    public async Task<ActionResult<FinancialSummary>> GetFinancialSummary(string buildingId)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var summary = await grain.GetFinancialSummaryAsync();
        return Ok(summary);
    }

    [HttpPost("{buildingId}/finance/payments")]
    public async Task<ActionResult> RecordPayment(string buildingId, [FromBody] PaymentInfoRequest request)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var payment = new PaymentInfo
        {
            Id = Guid.NewGuid(),
            UnitNumber = request.UnitNumber,
            Amount = request.Amount,
            PaymentDate = DateTime.UtcNow,
            Month = request.Month,
            Year = request.Year,
            Reference = request.Reference
        };
        await grain.RecordPaymentAsync(payment);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("{buildingId}/finance/expenses")]
    public async Task<ActionResult> RecordExpense(string buildingId, [FromBody] ExpenseInfoRequest request)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var expense = new ExpenseInfo
        {
            Id = Guid.NewGuid(),
            Category = request.Category,
            Amount = request.Amount,
            Description = request.Description ?? string.Empty,
            ExpenseDate = DateTime.UtcNow,
            Vendor = request.Vendor
        };
        await grain.RecordExpenseAsync(expense);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("{buildingId}/finance/fee-structure")]
    public async Task<ActionResult> SetFeeStructure(string buildingId, [FromBody] FeeStructureRequest request)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var fee = new FeeStructureInfo
        {
            Name = request.Name,
            AmountPerM2 = request.AmountPerM2,
            FixedAmount = request.FixedAmount,
            EffectiveFrom = DateTime.UtcNow
        };
        await grain.SetFeeStructureAsync(fee);
        return StatusCode(StatusCodes.Status201Created);
    }
}

public record BuildingInfoRequest
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = "";

    [Required, MaxLength(200)]
    public string Street { get; init; } = "";

    [Required, MaxLength(20)]
    public string StreetNumber { get; init; } = "";

    [Required, MaxLength(100)]
    public string City { get; init; } = "";

    [MaxLength(20)]
    public string? PostalCode { get; init; }

    [Range(1700, 2100)]
    public int? YearBuilt { get; init; }

    [Range(1, 200)]
    public int TotalFloors { get; init; }

    public bool HasElevator { get; init; }
    public bool HasParking { get; init; }

    [Range(0, 1_000_000)]
    public decimal? TotalAreaM2 { get; init; }
}

public record UnitInfoRequest
{
    [Required, MaxLength(20)]
    public string Number { get; init; } = "";

    [Range(-10, 200)]
    public int Floor { get; init; }

    public UnitType Type { get; init; }
    public UnitStatus Status { get; init; }

    [Range(0.01, 100_000)]
    public decimal AreaM2 { get; init; }

    [Range(0.0001, 100)]
    public decimal OwnershipShare { get; init; }

    [MaxLength(10)]
    public string? Entrance { get; init; }
}

public record ResidentInfoRequest
{
    [MaxLength(20)]
    public string? UnitNumber { get; init; }

    [Required, MaxLength(100)]
    public string FirstName { get; init; } = "";

    [Required, MaxLength(100)]
    public string LastName { get; init; } = "";

    [EmailAddress, MaxLength(200)]
    public string? Email { get; init; }

    [Phone, MaxLength(30)]
    public string? Phone { get; init; }

    public ResidentRole Role { get; init; }

    [Range(0, 100)]
    public decimal? OwnershipShare { get; init; }
}

public record KeyInfoRequest
{
    public KeyType KeyType { get; init; }

    [MaxLength(500)]
    public string? Description { get; init; }

    [MaxLength(100)]
    public string? Identifier { get; init; }
}

public record KeyCheckinRequest
{
    [MaxLength(100)]
    public string? Condition { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }
}

public record UpdateWorkOrderStatusRequest
{
    public WorkOrderStatus Status { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }
}

public record CreateAnnouncementRequest
{
    [Required, MaxLength(200)]
    public string Title { get; init; } = "";

    [Required, MaxLength(5000)]
    public string Content { get; init; } = "";

    public AnnouncementType Type { get; init; }
    public Guid CreatedBy { get; init; }
}

public record PaymentInfoRequest
{
    [Required, MaxLength(20)]
    public string UnitNumber { get; init; } = "";

    [Range(0.01, 100_000_000)]
    public decimal Amount { get; init; }

    [Range(1, 12)]
    public int? Month { get; init; }

    [Range(2000, 2200)]
    public int? Year { get; init; }

    [MaxLength(100)]
    public string? Reference { get; init; }
}

public record ExpenseInfoRequest
{
    public ExpenseCategory Category { get; init; }

    [Range(0.01, 100_000_000)]
    public decimal Amount { get; init; }

    [Required, MaxLength(500)]
    public string? Description { get; init; }

    [MaxLength(200)]
    public string? Vendor { get; init; }
}

public record FeeStructureRequest
{
    [Required, MaxLength(100)]
    public string Name { get; init; } = "";

    [Range(0, 1000)]
    public decimal AmountPerM2 { get; init; }

    [Range(0, 1_000_000)]
    public decimal? FixedAmount { get; init; }
}
