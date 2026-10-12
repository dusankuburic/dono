using BuildingManager.Core.Enums;
using MudBlazor;

namespace BuildingManager.Blazor.Services;

/// <summary>Display icons and colours for notification types.</summary>
public static class NotificationLabels
{
    public static (string Icon, Color Color) Icon(NotificationType type) => type switch
    {
        NotificationType.Announcement => (Icons.Material.Filled.Campaign, Color.Primary),
        NotificationType.WorkOrder => (Icons.Material.Filled.Build, Color.Warning),
        NotificationType.Assembly => (Icons.Material.Filled.HowToVote, Color.Info),
        NotificationType.Payment => (Icons.Material.Filled.Payments, Color.Success),
        NotificationType.Document => (Icons.Material.Filled.FolderOpen, Color.Secondary),
        _ => (Icons.Material.Filled.Info, Color.Default)
    };
}
