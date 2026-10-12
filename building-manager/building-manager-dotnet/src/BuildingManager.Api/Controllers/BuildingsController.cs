using System.ComponentModel.DataAnnotations;
using BuildingManager.Api.Authorization;
using BuildingManager.Api.Reports;
using BuildingManager.Api.Services;
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
    private readonly INotificationDispatcher _notifications;
    private readonly FinanceReportGenerator _financeReports;

    public BuildingsController(IGrainFactory grainFactory, INotificationDispatcher notifications,
        FinanceReportGenerator financeReports)
    {
        _grainFactory = grainFactory;
        _notifications = notifications;
        _financeReports = financeReports;
    }

        [HttpGet]
    public async Task<ActionResult<IEnumerable<BuildingSummary>>> GetAllBuildings()
    {
        var registry = _grainFactory.GetGrain<IBuildingRegistryGrain>("global");
        var entries = await registry.GetAllAsync();

        var role = User.FindFirst("role")?.Value;
        if (role is not ("Admin" or "CompanyAdmin"))
        {
            var email = User.FindFirst("email")?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return Ok(Array.Empty<BuildingSummary>());
            }

            var userGrain = _grainFactory.GetGrain<IUserGrain>(email);
            var accessible = (await userGrain.GetAccessibleBuildingIdsAsync()).ToHashSet();
            entries = entries.Where(e => accessible.Contains(e.Id)).ToList();
        }

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

            }
        }

        return Ok(summaries);
    }

    [HttpGet("{buildingId}")]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<ActionResult<BuildingSummary>> GetBuilding(string buildingId)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var summary = await grain.GetSummaryAsync();
        return Ok(summary);
    }

    [HttpPost("{buildingId}/initialize")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
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

        var email = User.FindFirst("email")?.Value;
        if (!string.IsNullOrEmpty(email))
        {
            await _grainFactory.GetGrain<IUserGrain>(email).GrantBuildingAccessAsync(buildingId);
        }

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpGet("{buildingId}/units")]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<ActionResult<IEnumerable<UnitInfo>>> GetUnits(string buildingId)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var units = await grain.GetAllUnitsAsync();
        return Ok(units);
    }

    [HttpPost("{buildingId}/units")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
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
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<ActionResult<IEnumerable<ResidentInfo>>> GetResidents(string buildingId)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var residents = await grain.GetResidentsAsync();
        return Ok(residents);
    }

    [HttpPost("{buildingId}/residents")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
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
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<ActionResult<IEnumerable<KeyInfo>>> GetKeys(string buildingId)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var keys = await grain.GetAllKeysAsync();
        return Ok(keys);
    }

    [HttpPost("{buildingId}/keys")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
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
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<ActionResult<KeyInfo>> CheckoutKey(string buildingId, Guid keyId, [FromBody] KeyCheckoutRequest request)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var result = await grain.CheckoutKeyAsync(keyId, request);
        return Ok(result);
    }

    [HttpPost("{buildingId}/keys/{keyId}/checkin")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<ActionResult<KeyInfo>> CheckinKey(string buildingId, Guid keyId, [FromBody] KeyCheckinRequest request)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var result = await grain.CheckinKeyAsync(keyId, request.Condition, request.Notes);
        return Ok(result);
    }

    [HttpGet("{buildingId}/workorders")]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<ActionResult<IEnumerable<WorkOrderInfo>>> GetWorkOrders(string buildingId, [FromQuery] WorkOrderStatus? status)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var workOrders = await grain.GetWorkOrdersAsync(status);
        return Ok(workOrders);
    }

    [HttpPost("{buildingId}/workorders")]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<ActionResult<WorkOrderInfo>> CreateWorkOrder(string buildingId, [FromBody] CreateWorkOrderRequest request)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var result = await grain.CreateWorkOrderAsync(request);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPatch("{buildingId}/workorders/{workOrderId}/status")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<ActionResult<WorkOrderInfo>> UpdateWorkOrderStatus(
        string buildingId,
        Guid workOrderId,
        [FromBody] UpdateWorkOrderStatusRequest request)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var before = (await grain.GetWorkOrdersAsync()).FirstOrDefault(w => w.Id == workOrderId)
            ?? throw new KeyNotFoundException($"Work order '{workOrderId}' not found");
        var result = await grain.UpdateWorkOrderStatusAsync(workOrderId, request.Status, request.Notes);

        if (before.Status != result.Status)
        {
            var reporter = (await grain.GetResidentsAsync())
                .FirstOrDefault(r => r.Id == result.ReportedByResidentId);
            if (reporter?.Email is { Length: > 0 } email)
            {
                await _notifications.NotifyEmailAsync(email, NotificationType.WorkOrder,
                    $"Work order {result.ReferenceNumber}: {result.Status}",
                    string.IsNullOrWhiteSpace(request.Notes) ? result.Title : request.Notes.Trim(),
                    "/workorders", buildingId, User.FindFirst("email")?.Value);
            }
        }

        return Ok(result);
    }

    [HttpGet("{buildingId}/announcements")]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<ActionResult<IEnumerable<AnnouncementInfo>>> GetAnnouncements(string buildingId)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var announcements = await grain.GetAnnouncementsAsync();
        return Ok(announcements);
    }

    [HttpPost("{buildingId}/announcements")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<ActionResult<AnnouncementInfo>> CreateAnnouncement(string buildingId, [FromBody] CreateAnnouncementRequest request)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var announcement = new AnnouncementInfo
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content,
            Type = request.Type,
            CreatedAt = DateTime.UtcNow,
            ScheduledFor = request.ScheduledFor,
            ExpiresAt = request.ExpiresAt,
            IsPinned = request.IsPinned,

            CreatedBy = Guid.TryParse(User.FindFirst("sub")?.Value, out var userId) ? userId : Guid.Empty,
            CreatedByName = User.FindFirst("name")?.Value
        };
        await grain.CreateAnnouncementAsync(announcement);

        if (announcement.ScheduledFor is not { } scheduledFor || scheduledFor <= DateTime.UtcNow)
        {
            await _notifications.NotifyBuildingMembersAsync(buildingId, NotificationType.Announcement,
                $"Announcement: {request.Title.Trim()}", Preview(request.Content), "/announcements",
                User.FindFirst("email")?.Value);
        }

        return StatusCode(StatusCodes.Status201Created, announcement);
    }

        private static string? Preview(string content)
    {
        var trimmed = content.Trim();
        return trimmed.Length <= 150 ? trimmed : trimmed[..147] + "…";
    }

        [HttpGet("{buildingId}/announcements/history")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<ActionResult<IEnumerable<AnnouncementInfo>>> GetAnnouncementHistory(string buildingId) =>
        Ok(await _grainFactory.GetGrain<IBuildingGrain>(buildingId).GetAnnouncementHistoryAsync());

    [HttpPut("{buildingId}/announcements/{announcementId:guid}/pin")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<IActionResult> SetAnnouncementPinned(string buildingId, Guid announcementId, [FromBody] PinRequest request)
    {
        await _grainFactory.GetGrain<IBuildingGrain>(buildingId).SetAnnouncementPinnedAsync(announcementId, request.Pinned);
        return Ok();
    }

        [HttpPost("{buildingId}/announcements/{announcementId:guid}/archive")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<IActionResult> ArchiveAnnouncement(string buildingId, Guid announcementId)
    {
        await _grainFactory.GetGrain<IBuildingGrain>(buildingId).ArchiveAnnouncementAsync(announcementId);
        return Ok();
    }

    [HttpGet("{buildingId}/finance/summary")]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<ActionResult<FinancialSummary>> GetFinancialSummary(string buildingId)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var summary = await grain.GetFinancialSummaryAsync();
        return Ok(summary);
    }

    [HttpPost("{buildingId}/finance/payments")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<ActionResult> RecordPayment(string buildingId, [FromBody] PaymentInfoRequest request)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var unitNumber = request.UnitNumber.Trim();
        await grain.GetUnitAsync(unitNumber);

        var payment = new PaymentInfo
        {
            Id = IdOrNew(request.Id),
            UnitNumber = unitNumber,
            Amount = request.Amount,
            PaymentDate = EntryDate(request.PaymentDate, "payment"),
            Month = request.Month,
            Year = request.Year,
            Reference = request.Reference,
            Method = request.Method,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };
        await grain.RecordPaymentAsync(payment);
        return StatusCode(StatusCodes.Status201Created);
    }

        [HttpGet("{buildingId}/finance/status")]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<ActionResult<MonthPaymentStatus>> GetPaymentStatus(string buildingId, [FromQuery] int? year, [FromQuery] int? month) =>
        Ok(await PaymentStatusForCallerAsync(buildingId, year, month));

        [HttpGet("{buildingId}/finance/status/pdf")]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<IActionResult> GetPaymentStatusPdf(string buildingId, [FromQuery] int? year, [FromQuery] int? month)
    {
        var (status, building) = await StatusAndBuildingAsync(buildingId, year, month);
        return File(_financeReports.FeeStatusPdf(building, status), "application/pdf",
            $"fee-status-{status.Year}-{status.Month:D2}.pdf");
    }

        [HttpGet("{buildingId}/finance/status/xlsx")]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<IActionResult> GetPaymentStatusExcel(string buildingId, [FromQuery] int? year, [FromQuery] int? month)
    {
        var (status, building) = await StatusAndBuildingAsync(buildingId, year, month);
        return File(_financeReports.FeeStatusExcel(building, status),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"fee-status-{status.Year}-{status.Month:D2}.xlsx");
    }

    private async Task<MonthPaymentStatus> PaymentStatusForCallerAsync(string buildingId, int? year, int? month)
    {
        var now = DateTime.UtcNow;
        var status = await _grainFactory.GetGrain<IBuildingGrain>(buildingId)
            .GetPaymentStatusAsync(year ?? now.Year, month ?? now.Month);

        if (IsManager())
            return status;

        var ownUnits = await OwnUnitsAsync(buildingId);
        return status with { Units = status.Units.Where(u => ownUnits.Contains(u.UnitNumber)).ToList() };
    }

    private async Task<(MonthPaymentStatus Status, BuildingSummary Building)> StatusAndBuildingAsync(
        string buildingId, int? year, int? month)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        return (await PaymentStatusForCallerAsync(buildingId, year, month), await grain.GetSummaryAsync());
    }

        [HttpGet("{buildingId}/finance/ledger")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<ActionResult<MonthLedger>> GetLedger(string buildingId, [FromQuery] int? year, [FromQuery] int? month)
    {
        var now = DateTime.UtcNow;
        return Ok(await _grainFactory.GetGrain<IBuildingGrain>(buildingId)
            .GetFinanceLedgerAsync(year ?? now.Year, month ?? now.Month));
    }

        [HttpGet("{buildingId}/finance/payments")]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<ActionResult<IEnumerable<PaymentInfo>>> GetUnitPayments(string buildingId, [FromQuery, Required] string unit)
    {
        unit = unit.Trim();
        if (!IsManager() && !(await OwnUnitsAsync(buildingId)).Contains(unit))
            return Forbid();

        return Ok(await _grainFactory.GetGrain<IBuildingGrain>(buildingId).GetUnitPaymentHistoryAsync(unit));
    }

    [HttpPost("{buildingId}/finance/expenses")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<ActionResult> RecordExpense(string buildingId, [FromBody] ExpenseInfoRequest request)
    {
        var grain = _grainFactory.GetGrain<IBuildingGrain>(buildingId);
        var expense = new ExpenseInfo
        {
            Id = IdOrNew(request.Id),
            Category = request.Category,
            Amount = request.Amount,
            Description = request.Description ?? string.Empty,
            ExpenseDate = EntryDate(request.ExpenseDate, "expense"),
            Vendor = request.Vendor
        };
        await grain.RecordExpenseAsync(expense);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("{buildingId}/finance/fee-structure")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
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

        [HttpGet("{buildingId}/access")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<ActionResult<IEnumerable<BuildingMember>>> GetAccess(string buildingId)
    {
        var emails = await _grainFactory.GetGrain<IBuildingAccessGrain>(buildingId).GetMemberEmailsAsync();

        var members = new List<BuildingMember>();
        foreach (var email in emails)
        {
            try
            {
                var profile = await _grainFactory.GetGrain<IUserGrain>(email).GetProfileAsync();
                members.Add(new BuildingMember
                {
                    Email = profile.Email,
                    DisplayName = profile.DisplayName,
                    Role = profile.Role
                });
            }
            catch (KeyNotFoundException)
            {

            }
        }

        return Ok(members);
    }

        [HttpPut("{buildingId}/access/{email}")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<IActionResult> GrantAccess(string buildingId, string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var userGrain = _grainFactory.GetGrain<IUserGrain>(normalizedEmail);
        await userGrain.GetProfileAsync();
        await userGrain.GrantBuildingAccessAsync(buildingId);
        return Ok();
    }

        private static Guid IdOrNew(Guid? clientId) =>
        clientId is { } id && id != Guid.Empty ? id : Guid.NewGuid();

        private static DateTime EntryDate(DateTime? requested, string what)
    {
        var now = DateTime.UtcNow;
        if (requested is not { } date)
            return now;

        var utc = date.Kind == DateTimeKind.Local ? date.ToUniversalTime() : DateTime.SpecifyKind(date, DateTimeKind.Utc);
        if (utc > now.AddDays(1))
            throw new ArgumentException($"The {what} date cannot be in the future.");
        if (utc.Year < 2000)
            throw new ArgumentException($"The {what} date is not valid.");
        return utc;
    }

    private bool IsManager() => AuthPolicies.ManageRoles.Any(User.IsInRole);

        private async Task<HashSet<string>> OwnUnitsAsync(string buildingId)
    {
        var email = User.FindFirst("email")?.Value;
        if (string.IsNullOrEmpty(email))
            return new HashSet<string>();

        var residents = await _grainFactory.GetGrain<IBuildingGrain>(buildingId).GetResidentsAsync();
        return residents
            .Where(r => r.UnitNumber is not null && string.Equals(r.Email?.Trim(), email, StringComparison.OrdinalIgnoreCase))
            .Select(r => r.UnitNumber!)
            .ToHashSet();
    }

        [HttpDelete("{buildingId}/access/{email}")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    [Authorize(Policy = AuthPolicies.BuildingAccess)]
    public async Task<IActionResult> RevokeAccess(string buildingId, string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        await _grainFactory.GetGrain<IUserGrain>(normalizedEmail).RevokeBuildingAccessAsync(buildingId);
        return Ok();
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

    public AnnouncementType Type { get; init; } = AnnouncementType.General;

        public DateTime? ScheduledFor { get; init; }

        public DateTime? ExpiresAt { get; init; }

    public bool IsPinned { get; init; }
}

public record PinRequest
{
    public bool Pinned { get; init; }
}

public record PaymentInfoRequest
{
        public Guid? Id { get; init; }

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

        public DateTime? PaymentDate { get; init; }

    public PaymentMethod? Method { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }
}

public record ExpenseInfoRequest
{
        public Guid? Id { get; init; }

    public ExpenseCategory Category { get; init; }

    [Range(0.01, 100_000_000)]
    public decimal Amount { get; init; }

    [Required, MaxLength(500)]
    public string? Description { get; init; }

    [MaxLength(200)]
    public string? Vendor { get; init; }

        public DateTime? ExpenseDate { get; init; }
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
