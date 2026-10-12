using BuildingManager.Core.Enums;
using MudBlazor;

namespace BuildingManager.Blazor.Services;

/// <summary>Display text, icons and size formatting for building documents.</summary>
public static class DocumentLabels
{
    public static (string Text, string Icon) Folder(DocumentFolder folder) => folder switch
    {
        DocumentFolder.Registration => ("Registration", Icons.Material.Filled.Gavel),
        DocumentFolder.AssemblyMinutes => ("Assembly minutes", Icons.Material.Filled.HowToVote),
        DocumentFolder.FinancialReports => ("Financial reports", Icons.Material.Filled.AttachMoney),
        DocumentFolder.Contracts => ("Contracts", Icons.Material.Filled.Handshake),
        DocumentFolder.Permits => ("Permits", Icons.Material.Filled.Verified),
        DocumentFolder.Insurance => ("Insurance", Icons.Material.Filled.HealthAndSafety),
        _ => ("Other", Icons.Material.Filled.Folder)
    };

    public static string Access(DocumentAccessLevel access) => access switch
    {
        DocumentAccessLevel.AllOwners => "All owners",
        _ => "Managers only"
    };

    public static string Size(long bytes) =>
        bytes >= 1024 * 1024 ? $"{bytes / (1024 * 1024.0):0.#} MB"
        : bytes >= 1024 ? $"{bytes / 1024.0:0.#} KB"
        : $"{bytes} B";
}
