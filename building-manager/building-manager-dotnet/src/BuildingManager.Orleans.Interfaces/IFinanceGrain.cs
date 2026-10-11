using Orleans;

namespace BuildingManager.Orleans.Interfaces;

/// <summary>
/// Financial history for one building and one calendar month, keyed as
/// "{buildingId}|{yyyy-MM}". Partitioning by month keeps each grain's state
/// bounded: recording a payment rewrites a single month instead of the whole
/// building aggregate.
/// </summary>
public interface IFinanceGrain : IGrainWithStringKey
{
    /// <summary>Records a payment received this month. Idempotent by payment id.</summary>
    Task RecordPaymentAsync(PaymentInfo payment);

    /// <summary>
    /// Credits a payment received in another month (late or in advance) to
    /// this month's fee receivables. Does not count as income here. Idempotent by payment id.
    /// </summary>
    Task RecordPeriodPaymentAsync(PaymentInfo payment);

    /// <summary>Records an expense incurred this month. Idempotent by expense id.</summary>
    Task RecordExpenseAsync(ExpenseInfo expense);

    Task<MonthlyTotals> GetMonthlyTotalsAsync();

    Task<MonthLedger> GetLedgerAsync();
}

/// <summary>The entries of one month, for listing them.</summary>
[GenerateSerializer]
public record MonthLedger
{
    /// <summary>Payments received this month, whatever month they pay for.</summary>
    [Id(0)] public List<PaymentInfo> Payments { get; init; } = new();
    [Id(1)] public List<ExpenseInfo> Expenses { get; init; } = new();
    /// <summary>Payments received in other months that pay this month's fee.</summary>
    [Id(2)] public List<PaymentInfo> PaymentsForThisMonthReceivedElsewhere { get; init; } = new();
}

/// <summary>Aggregated figures for one month, used by building summaries.</summary>
[GenerateSerializer]
public record MonthlyTotals
{
    /// <summary>Sum of payment amounts recorded for the month.</summary>
    [Id(0)] public decimal TotalIncome { get; init; }

    /// <summary>Sum of expense amounts recorded for the month.</summary>
    [Id(1)] public decimal TotalExpenses { get; init; }

    /// <summary>Distinct unit numbers with at least one payment this month.</summary>
    [Id(2)] public List<string> PayingUnits { get; init; } = new();

    /// <summary>
    /// Total paid per unit number FOR this month's fee (billing period from the
    /// payment's Month/Year, else its date), wherever it was received.
    /// </summary>
    [Id(3)] public Dictionary<string, decimal> PaidAmountsByUnit { get; init; } = new();
}
