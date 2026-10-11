using System.Net.Http;
using System.Net.Http.Json;
using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingManager.Blazor.Services;

/// <summary>Fee status, payments and expenses. Failures throw InvalidOperationException with the API's message.</summary>
public interface IFinanceService
{
    /// <summary>Fee status per unit for a billing month; owners get only their own units.</summary>
    Task<MonthPaymentStatus> GetPaymentStatusAsync(string buildingId, int year, int month);

    /// <summary>Payments received and expenses incurred in a month (managers only).</summary>
    Task<MonthLedger> GetLedgerAsync(string buildingId, int year, int month);

    /// <summary>A unit's payments of the last 12 months, newest first.</summary>
    Task<List<PaymentInfo>> GetUnitPaymentsAsync(string buildingId, string unitNumber);

    Task RecordPaymentAsync(string buildingId, PaymentDraft payment);
    Task RecordExpenseAsync(string buildingId, ExpenseDraft expense);
}

/// <summary>A payment as entered by the manager. The id makes a retried submit count once.</summary>
public record PaymentDraft(
    Guid Id, string UnitNumber, decimal Amount, int Year, int Month, DateTime ReceivedAtUtc,
    PaymentMethod? Method, string? Reference, string? Notes);

public record ExpenseDraft(
    Guid Id, ExpenseCategory Category, decimal Amount, string Description, string? Vendor, DateTime IncurredAtUtc);

public class FinanceService : IFinanceService
{
    private readonly HttpClient _httpClient;

    public FinanceService([FromKeyedServices("ApiHttpClient")] HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private static string Base(string buildingId) => $"/api/buildings/{Uri.EscapeDataString(buildingId)}/finance";

    public async Task<MonthPaymentStatus> GetPaymentStatusAsync(string buildingId, int year, int month)
    {
        var response = await _httpClient.GetAsync($"{Base(buildingId)}/status?year={year}&month={month}");
        await ApiErrors.EnsureSuccessAsync(response, "Could not load the fee status.");
        return await response.Content.ReadFromJsonAsync<MonthPaymentStatus>()
            ?? throw new InvalidOperationException("Could not load the fee status.");
    }

    public async Task<MonthLedger> GetLedgerAsync(string buildingId, int year, int month)
    {
        var response = await _httpClient.GetAsync($"{Base(buildingId)}/ledger?year={year}&month={month}");
        await ApiErrors.EnsureSuccessAsync(response, "Could not load payments and expenses.");
        return await response.Content.ReadFromJsonAsync<MonthLedger>() ?? new MonthLedger();
    }

    public async Task<List<PaymentInfo>> GetUnitPaymentsAsync(string buildingId, string unitNumber)
    {
        var response = await _httpClient.GetAsync($"{Base(buildingId)}/payments?unit={Uri.EscapeDataString(unitNumber)}");
        await ApiErrors.EnsureSuccessAsync(response, "Could not load the payment history.");
        return await response.Content.ReadFromJsonAsync<List<PaymentInfo>>() ?? new();
    }

    public async Task RecordPaymentAsync(string buildingId, PaymentDraft payment)
    {
        var response = await _httpClient.PostAsJsonAsync($"{Base(buildingId)}/payments", new
        {
            payment.Id,
            payment.UnitNumber,
            payment.Amount,
            payment.Year,
            payment.Month,
            PaymentDate = payment.ReceivedAtUtc,
            payment.Method,
            payment.Reference,
            payment.Notes
        });
        await ApiErrors.EnsureSuccessAsync(response, "Could not record the payment.");
    }

    public async Task RecordExpenseAsync(string buildingId, ExpenseDraft expense)
    {
        var response = await _httpClient.PostAsJsonAsync($"{Base(buildingId)}/expenses", new
        {
            expense.Id,
            expense.Category,
            expense.Amount,
            expense.Description,
            expense.Vendor,
            ExpenseDate = expense.IncurredAtUtc
        });
        await ApiErrors.EnsureSuccessAsync(response, "Could not record the expense.");
    }
}
