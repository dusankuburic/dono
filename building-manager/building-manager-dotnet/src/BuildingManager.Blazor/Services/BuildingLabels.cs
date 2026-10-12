using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;
using MudBlazor;

namespace BuildingManager.Blazor.Services;

public static class BuildingLabels
{
    public static string KeyTypeLabel(KeyType type) => type switch
    {
        KeyType.EntranceMain => "Entrance (main)",
        KeyType.EntranceSide => "Entrance (side)",
        KeyType.Unit => "Unit",
        KeyType.Storage => "Storage",
        KeyType.Garage => "Garage",
        KeyType.CommonRoom => "Common room",
        KeyType.Basement => "Basement",
        KeyType.Roof => "Roof",
        _ => type.ToString()
    };

    public static (string Text, Color Color) KeyStatusLabel(KeyStatus status) => status switch
    {
        KeyStatus.Available => ("Available", Color.Success),
        KeyStatus.CheckedOut => ("Checked out", Color.Warning),
        KeyStatus.Lost => ("Lost", Color.Error),
        KeyStatus.Damaged => ("Damaged", Color.Error),
        KeyStatus.InRepair => ("In repair", Color.Info),
        _ => (status.ToString(), Color.Default)
    };

    public static string ResidentRoleLabel(ResidentRole role) => role switch
    {
        ResidentRole.Owner => "Owner",
        ResidentRole.CoOwner => "Co-owner",
        ResidentRole.Tenant => "Tenant",
        ResidentRole.FamilyMember => "Family member",
        _ => role.ToString()
    };

    public static string RecipientTypeLabel(RecipientType type) => type switch
    {
        RecipientType.Resident => "Resident",
        RecipientType.Contractor => "Contractor",
        RecipientType.Viewer => "Viewer",
        RecipientType.NewResident => "New resident",
        _ => "Other"
    };

    public static (string Text, Color Color) WorkOrderStatusLabel(WorkOrderStatus status) => status switch
    {
        WorkOrderStatus.New => ("New", Color.Warning),
        WorkOrderStatus.Acknowledged => ("Acknowledged", Color.Info),
        WorkOrderStatus.InProgress => ("In progress", Color.Info),
        WorkOrderStatus.WaitingParts => ("Waiting for parts", Color.Secondary),
        WorkOrderStatus.WaitingDecision => ("Waiting for decision", Color.Secondary),
        WorkOrderStatus.Completed => ("Completed", Color.Success),
        WorkOrderStatus.Cancelled => ("Cancelled", Color.Error),
        _ => (status.ToString(), Color.Default)
    };

    public static (string Text, Color Color) PriorityLabel(Priority priority) => priority switch
    {
        Priority.Low => ("Low", Color.Info),
        Priority.Normal => ("Normal", Color.Default),
        Priority.High => ("High", Color.Warning),
        Priority.Emergency => ("Emergency", Color.Error),
        _ => (priority.ToString(), Color.Default)
    };

    public static string CategoryLabel(WorkOrderCategory category) => category switch
    {
        WorkOrderCategory.Plumbing => "Plumbing",
        WorkOrderCategory.Electrical => "Electrical",
        WorkOrderCategory.Elevator => "Elevator",
        WorkOrderCategory.CommonAreas => "Common areas",
        WorkOrderCategory.FacadeRoof => "Facade / roof",
        WorkOrderCategory.EntranceIntercom => "Entrance / intercom",
        WorkOrderCategory.Heating => "Heating",
        WorkOrderCategory.Cooling => "Cooling",
        WorkOrderCategory.Parking => "Parking",
        WorkOrderCategory.Garden => "Garden",
        WorkOrderCategory.Security => "Security",
        WorkOrderCategory.Cleaning => "Cleaning",
        WorkOrderCategory.FireSafety => "Fire safety",
        _ => "Other"
    };

    public static string Resident(ResidentInfo resident) =>
        $"{resident.FirstName} {resident.LastName}{(resident.UnitNumber is null ? "" : $" ({resident.UnitNumber})")}";
}
