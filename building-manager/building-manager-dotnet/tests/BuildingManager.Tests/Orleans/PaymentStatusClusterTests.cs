using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;
using FluentAssertions;
using Orleans;
using Xunit;

namespace BuildingManager.Tests.Orleans;

public class PaymentStatusClusterTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fixture;

    public PaymentStatusClusterTests(ClusterFixture fixture)
    {
        _fixture = fixture;
    }

    private static readonly DateTime ThisMonth = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime LastMonth = ThisMonth.AddMonths(-1);

        private async Task<IBuildingGrain> Building()
    {
        var grain = _fixture.Cluster.GrainFactory.GetGrain<IBuildingGrain>($"fees-{Guid.NewGuid():N}");
        await grain.InitializeAsync(new BuildingInfo
        {
            Name = "Fee Building", Street = "Zmaj Jovina", StreetNumber = "3",
            City = "Novi Sad", TotalFloors = 3, TotalAreaM2 = 230
        });
        foreach (var (number, area) in new[] { ("1", 100m), ("2", 50m), ("3", 80m) })
        {
            await grain.AddOrUpdateUnitAsync(new UnitInfo
            {
                Id = Guid.NewGuid(), Number = number, Floor = 1, AreaM2 = area, OwnershipShare = 10,
                Type = UnitType.Apartment, Status = UnitStatus.OccupiedByOwner
            });
        }
        return grain;
    }

        private static Task SetFee(IBuildingGrain grain, decimal fixedAmount, decimal perM2, DateTime from) =>
        grain.SetFeeStructureAsync(new FeeStructureInfo
        {
            Name = $"{fixedAmount} + {perM2}/m²", FixedAmount = fixedAmount, AmountPerM2 = perM2, EffectiveFrom = from
        });

    private static Task Pay(IBuildingGrain grain, string unit, decimal amount, DateTime forMonth, DateTime receivedAt) =>
        grain.RecordPaymentAsync(new PaymentInfo
        {
            Id = Guid.NewGuid(), UnitNumber = unit, Amount = amount, PaymentDate = receivedAt,
            Month = forMonth.Month, Year = forMonth.Year, Method = PaymentMethod.BankTransfer
        });

    [Fact]
    public async Task Units_Are_Paid_Behind_Or_Overdue()
    {
        var grain = await Building();

        await SetFee(grain, 200m, 10m, LastMonth);
        var now = DateTime.UtcNow;

        await Pay(grain, "1", 1200m, ThisMonth, now);
        await Pay(grain, "2", 700m, LastMonth, LastMonth.AddDays(5));
        await Pay(grain, "2", 300m, ThisMonth, now);

        var status = await grain.GetPaymentStatusAsync(ThisMonth.Year, ThisMonth.Month);

        status.Units.Select(u => (u.UnitNumber, u.Expected, u.Paid, u.Standing)).Should().Equal(
            ("1", 1200m, 1200m, PaymentStanding.Paid),
            ("2", 700m, 300m, PaymentStanding.OneMonthBehind),
            ("3", 1000m, 0m, PaymentStanding.TwoOrMoreMonthsBehind));
    }

    [Fact]
    public async Task Without_A_Fee_Nothing_Is_Due()
    {
        var grain = await Building();

        var status = await grain.GetPaymentStatusAsync(ThisMonth.Year, ThisMonth.Month);

        status.FeeName.Should().BeNull();
        status.Units.Should().OnlyContain(u => u.Standing == PaymentStanding.NoFee && u.Expected == 0);
    }

    [Fact]
    public async Task Each_Month_Is_Judged_By_The_Fee_In_Force_Then()
    {
        var grain = await Building();
        await SetFee(grain, 0m, 10m, LastMonth);
        await SetFee(grain, 0m, 20m, DateTime.UtcNow);

        var last = await grain.GetPaymentStatusAsync(LastMonth.Year, LastMonth.Month);
        var current = await grain.GetPaymentStatusAsync(ThisMonth.Year, ThisMonth.Month);

        last.Units.Single(u => u.UnitNumber == "1").Expected.Should().Be(1000m);
        current.Units.Single(u => u.UnitNumber == "1").Expected.Should().Be(2000m);
        (await grain.GetFinancialSummaryAsync()).OutstandingReceivables.Should().Be(2000m + 1000m + 1600m);
    }

    [Fact]
    public async Task Ledger_And_Unit_History_List_The_Recorded_Entries()
    {
        var grain = await Building();
        var now = DateTime.UtcNow;
        await Pay(grain, "1", 500m, LastMonth, LastMonth.AddDays(3));
        await Pay(grain, "1", 600m, ThisMonth, now);
        await Pay(grain, "2", 700m, ThisMonth, now);
        await grain.RecordExpenseAsync(new ExpenseInfo
        {
            Id = Guid.NewGuid(), Amount = 250m, ExpenseDate = now,
            Category = ExpenseCategory.Cleaning, Description = "Stairwell cleaning"
        });

        var ledger = await grain.GetFinanceLedgerAsync(ThisMonth.Year, ThisMonth.Month);
        ledger.Payments.Select(p => p.Amount).Should().BeEquivalentTo(new[] { 600m, 700m });
        ledger.Payments.Should().OnlyContain(p => p.Method == PaymentMethod.BankTransfer);
        ledger.Expenses.Should().ContainSingle(e => e.Description == "Stairwell cleaning");

        var history = await grain.GetUnitPaymentHistoryAsync("1");
        history.Select(p => p.Amount).Should().Equal(600m, 500m);

        var invalidMonth = () => grain.GetPaymentStatusAsync(2026, 13);
        await invalidMonth.Should().ThrowAsync<ArgumentException>();
    }
}
