using BuildingManager.Core.Enums;
using MudBlazor;

namespace BuildingManager.Blazor.Services;

public static class BuildingLabels
{
    public static string UnitType(UnitType type) => type switch
    {
        Core.Enums.UnitType.Apartment => "Apartment",
        Core.Enums.UnitType.Studio => "Studio",
        Core.Enums.UnitType.Commercial => "Commercial space",
        Core.Enums.UnitType.Garage => "Garage",
        Core.Enums.UnitType.Storage => "Storage",
        Core.Enums.UnitType.Office => "Office",
        _ => type.ToString()
    };

    public static string UnitStatus(UnitStatus status) => status switch
    {
        Core.Enums.UnitStatus.OccupiedByOwner => "Owner lives here",
        Core.Enums.UnitStatus.OccupiedByFamily => "Owner's family",
        Core.Enums.UnitStatus.OccupiedByTenant => "Rented",
        Core.Enums.UnitStatus.Vacant => "Vacant",
        Core.Enums.UnitStatus.UnderRenovation => "Under renovation",
        _ => status.ToString()
    };

    public static string ResidentRole(ResidentRole role) => role switch
    {
        Core.Enums.ResidentRole.Owner => "Owner",
        Core.Enums.ResidentRole.CoOwner => "Co-owner",
        Core.Enums.ResidentRole.Tenant => "Tenant",
        Core.Enums.ResidentRole.FamilyMember => "Family member",
        _ => role.ToString()
    };

    public static string KeyType(KeyType type) => type switch
    {
        Core.Enums.KeyType.EntranceMain => "Main entrance",
        Core.Enums.KeyType.EntranceSide => "Side entrance",
        Core.Enums.KeyType.Unit => "Unit",
        Core.Enums.KeyType.Storage => "Storage",
        Core.Enums.KeyType.Garage => "Garage",
        Core.Enums.KeyType.CommonRoom => "Common room",
        Core.Enums.KeyType.Basement => "Basement",
        Core.Enums.KeyType.Roof => "Roof",
        _ => type.ToString()
    };

    public static (string Text, Color Color) KeyStatus(KeyStatus status) => status switch
    {
        Core.Enums.KeyStatus.Available => ("Available", Color.Success),
        Core.Enums.KeyStatus.CheckedOut => ("Checked out", Color.Warning),
        Core.Enums.KeyStatus.Lost => ("Lost", Color.Error),
        Core.Enums.KeyStatus.Damaged => ("Damaged", Color.Error),
        Core.Enums.KeyStatus.InRepair => ("In repair", Color.Info),
        _ => (status.ToString(), Color.Default)
    };

    public static string RecipientType(RecipientType type) => type switch
    {
        Core.Enums.RecipientType.Resident => "Resident",
        Core.Enums.RecipientType.Contractor => "Contractor",
        Core.Enums.RecipientType.Viewer => "Viewing (buyer/tenant)",
        Core.Enums.RecipientType.NewResident => "New resident",
        Core.Enums.RecipientType.Other => "Other",
        _ => type.ToString()
    };

    public static string Category(WorkOrderCategory category) => category switch
    {
        WorkOrderCategory.CommonAreas => "Common areas",
        WorkOrderCategory.FacadeRoof => "Facade / roof",
        WorkOrderCategory.EntranceIntercom => "Entrance / intercom",
        WorkOrderCategory.FireSafety => "Fire safety",
        _ => category.ToString()
    };

    public static (string Text, Color Color) Priority(Priority priority) => priority switch
    {
        Core.Enums.Priority.Low => ("Low", Color.Default),
        Core.Enums.Priority.Normal => ("Normal", Color.Info),
        Core.Enums.Priority.High => ("High", Color.Warning),
        Core.Enums.Priority.Emergency => ("Emergency", Color.Error),
        _ => (priority.ToString(), Color.Default)
    };

    public static (string Text, Color Color) WorkOrderStatus(WorkOrderStatus status) => status switch
    {
        Core.Enums.WorkOrderStatus.New => ("New", Color.Warning),
        Core.Enums.WorkOrderStatus.Acknowledged => ("Acknowledged", Color.Info),
        Core.Enums.WorkOrderStatus.InProgress => ("In progress", Color.Info),
        Core.Enums.WorkOrderStatus.WaitingParts => ("Waiting for parts", Color.Secondary),
        Core.Enums.WorkOrderStatus.WaitingDecision => ("Waiting for decision", Color.Secondary),
        Core.Enums.WorkOrderStatus.Completed => ("Completed", Color.Success),
        Core.Enums.WorkOrderStatus.Cancelled => ("Cancelled", Color.Default),
        _ => (status.ToString(), Color.Default)
    };

    public static bool IsOpen(WorkOrderStatus status) =>
        status is not (Core.Enums.WorkOrderStatus.Completed or Core.Enums.WorkOrderStatus.Cancelled);

    public static string Relative(DateTime utc)
    {
        var span = utc - DateTime.UtcNow;
        var past = span < TimeSpan.Zero;
        var abs = past ? -span : span;
        var text = abs.TotalHours < 1 ? $"{Math.Max(1, (int)abs.TotalMinutes)} min"
            : abs.TotalHours < 48 ? $"{(int)abs.TotalHours} h"
            : $"{(int)abs.TotalDays} days";
        return past ? $"{text} ago" : $"in {text}";
    }
}
