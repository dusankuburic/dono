using System.Globalization;
using BuildingManager.Orleans.Interfaces;
using Orleans;
using Orleans.Runtime;

namespace BuildingManager.Orleans.Grains;

/// <summary>
/// Holds one building's payments and expenses for a single calendar month.
/// The key format is "{buildingId}|{yyyy-MM}" and is produced by
/// BuildingGrain.GetFinanceGrain.
/// </summary>
public class FinanceGrain : Grain, IFinanceGrain
{
    private readonly IPersistentState<FinanceGrainState> _state;
    private DateTime? _month;

    public FinanceGrain(
        [PersistentState("finance", "buildingStorage")]
        IPersistentState<FinanceGrainState> state)
    {
        _state = state;
    }

    /// <summary>
    /// The fee month a payment pays for: its Month/Year when given, otherwise
    /// the month it was received. Receivables are computed per billing month.
    /// </summary>
    public static DateTime BillingPeriodOf(PaymentInfo payment) =>
        payment.Year is int year && payment.Month is int month and >= 1 and <= 12
            ? new DateTime(year, month, 1)
            : new DateTime(payment.PaymentDate.Year, payment.PaymentDate.Month, 1);

    /// <summary>First day of the month this grain holds, parsed from its key.</summary>
    private DateTime Month => _month ??= ParseMonth(this.GetPrimaryKeyString());

    public async Task RecordPaymentAsync(PaymentInfo payment)
    {
        if (payment.Amount <= 0)
            throw new ArgumentException("Payment amount must be positive", nameof(payment));

        // Ids make retries safe: a repeated call must not count twice.
        if (_state.State.Payments.Any(p => p.Id == payment.Id))
            return;

        _state.State.Payments.Add(payment);
        await _state.WriteStateAsync();
    }

    public async Task RecordPeriodPaymentAsync(PaymentInfo payment)
    {
        if (payment.Amount <= 0)
            throw new ArgumentException("Payment amount must be positive", nameof(payment));

        if (_state.State.PeriodPayments.Any(p => p.Id == payment.Id))
            return;

        _state.State.PeriodPayments.Add(payment);
        await _state.WriteStateAsync();
    }

    public async Task RecordExpenseAsync(ExpenseInfo expense)
    {
        if (expense.Amount <= 0)
            throw new ArgumentException("Expense amount must be positive", nameof(expense));

        if (_state.State.Expenses.Any(e => e.Id == expense.Id))
            return;

        _state.State.Expenses.Add(expense);
        await _state.WriteStateAsync();
    }

    public Task<MonthlyTotals> GetMonthlyTotalsAsync()
    {
        // Received here and for this month, plus credits received elsewhere.
        var paidForThisMonth = _state.State.Payments
            .Where(p => BillingPeriodOf(p) == Month)
            .Concat(_state.State.PeriodPayments);

        return Task.FromResult(new MonthlyTotals
        {
            TotalIncome = _state.State.Payments.Sum(p => p.Amount),
            TotalExpenses = _state.State.Expenses.Sum(e => e.Amount),
            PayingUnits = _state.State.Payments
                .Select(p => p.UnitNumber)
                .Distinct()
                .ToList(),
            PaidAmountsByUnit = paidForThisMonth
                .GroupBy(p => p.UnitNumber)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount))
        });
    }

    public Task<MonthLedger> GetLedgerAsync()
    {
        return Task.FromResult(new MonthLedger
        {
            Payments = _state.State.Payments.OrderByDescending(p => p.PaymentDate).ToList(),
            Expenses = _state.State.Expenses.OrderByDescending(e => e.ExpenseDate).ToList(),
            PaymentsForThisMonthReceivedElsewhere = _state.State.PeriodPayments.OrderByDescending(p => p.PaymentDate).ToList()
        });
    }

    private static DateTime ParseMonth(string key)
    {
        var separator = key.LastIndexOf('|');
        if (separator < 0 || !DateTime.TryParseExact(key[(separator + 1)..], "yyyy-MM",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var month))
        {
            throw new InvalidOperationException($"Finance grain key '{key}' is not '{{buildingId}}|{{yyyy-MM}}'.");
        }

        return month;
    }
}

[GenerateSerializer]
public class FinanceGrainState
{
    /// <summary>Payments received this month (income, collection rate).</summary>
    [Id(0)] public List<PaymentInfo> Payments { get; set; } = new();
    [Id(1)] public List<ExpenseInfo> Expenses { get; set; } = new();

    /// <summary>Payments received in other months that pay this month's fee.</summary>
    [Id(2)] public List<PaymentInfo> PeriodPayments { get; set; } = new();
}
