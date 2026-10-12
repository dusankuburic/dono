using Orleans;

namespace BuildingManager.Orleans.Interfaces;

public interface IFinanceGrain : IGrainWithStringKey
{
        Task RecordPaymentAsync(PaymentInfo payment);

        Task RecordPeriodPaymentAsync(PaymentInfo payment);

        Task RecordExpenseAsync(ExpenseInfo expense);

    Task<MonthlyTotals> GetMonthlyTotalsAsync();

    Task<MonthLedger> GetLedgerAsync();
}

[GenerateSerializer]
public record MonthLedger
{
        [Id(0)] public List<PaymentInfo> Payments { get; init; } = new();
    [Id(1)] public List<ExpenseInfo> Expenses { get; init; } = new();
        [Id(2)] public List<PaymentInfo> PaymentsForThisMonthReceivedElsewhere { get; init; } = new();
}

[GenerateSerializer]
public record MonthlyTotals
{
        [Id(0)] public decimal TotalIncome { get; init; }

        [Id(1)] public decimal TotalExpenses { get; init; }

        [Id(2)] public List<string> PayingUnits { get; init; } = new();

        [Id(3)] public Dictionary<string, decimal> PaidAmountsByUnit { get; init; } = new();
}
