using System.Globalization;
using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;
using MudBlazor;

namespace BuildingManager.Blazor.Services;

/// <summary>Display text and colours for finance and announcement enums.</summary>
public static class FinanceLabels
{
    public static string Money(decimal amount) => $"{amount:N2} RSD";

    public static string MonthName(int year, int month) =>
        new DateTime(year, month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>UC-008 colours: green paid, yellow one month behind, red two or more.</summary>
    public static (string Text, Color Color) Standing(PaymentStanding standing) => standing switch
    {
        PaymentStanding.Paid => ("Paid", Color.Success),
        PaymentStanding.OneMonthBehind => ("Not paid this month", Color.Warning),
        PaymentStanding.TwoOrMoreMonthsBehind => ("2+ months unpaid", Color.Error),
        _ => ("No fee", Color.Default)
    };

    public static string Method(PaymentMethod? method) => method switch
    {
        PaymentMethod.Cash => "Cash",
        PaymentMethod.BankTransfer => "Bank transfer",
        PaymentMethod.Card => "Card",
        PaymentMethod.Check => "Cheque",
        _ => "-"
    };

    public static string Category(ExpenseCategory category) => category switch
    {
        ExpenseCategory.UtilitiesElectricity => "Electricity",
        ExpenseCategory.UtilitiesWater => "Water",
        ExpenseCategory.UtilitiesHeating => "Heating",
        ExpenseCategory.UtilitiesGas => "Gas",
        _ => category.ToString()
    };

    public static (string Text, Color Color, string Icon) Announcement(AnnouncementType type) => type switch
    {
        AnnouncementType.Emergency => ("Emergency", Color.Error, Icons.Material.Filled.Warning),
        AnnouncementType.Maintenance => ("Maintenance", Color.Warning, Icons.Material.Filled.Build),
        AnnouncementType.Meeting => ("Assembly meeting", Color.Info, Icons.Material.Filled.HowToVote),
        AnnouncementType.PaymentReminder => ("Payment reminder", Color.Secondary, Icons.Material.Filled.Payments),
        _ => ("General", Color.Default, Icons.Material.Filled.Campaign)
    };
}
