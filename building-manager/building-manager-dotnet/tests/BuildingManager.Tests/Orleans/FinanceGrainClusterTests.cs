using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;
using FluentAssertions;
using Orleans;
using Xunit;

namespace BuildingManager.Tests.Orleans;

/// <summary>
/// Per-month finance grains: month isolation, totals, and unit payment
/// aggregation. Keys use the same "{buildingId}|{yyyy-MM}" format as
/// BuildingGrain.
/// </summary>
public class FinanceGrainClusterTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fixture;

    public FinanceGrainClusterTests(ClusterFixture fixture)
    {
        _fixture = fixture;
    }

    private IGrainFactory GrainFactory => _fixture.Cluster.GrainFactory;

    // Unique per test: grain state persists in the shared cluster fixture.
    private string NewBuildingKey() => $"finance-test-{Guid.NewGuid():N}";

    private IFinanceGrain GetFinanceGrain(string buildingKey, DateTime month) =>
        GrainFactory.GetGrain<IFinanceGrain>($"{buildingKey}|{month:yyyy-MM}");

    private static PaymentInfo Payment(string unit, decimal amount, DateTime date) => new()
    {
        Id = Guid.NewGuid(),
        UnitNumber = unit,
        Amount = amount,
        PaymentDate = date,
        Month = date.Month,
        Year = date.Year
    };

    private static ExpenseInfo Expense(decimal amount, DateTime date) => new()
    {
        Id = Guid.NewGuid(),
        Amount = amount,
        ExpenseDate = date,
        Category = ExpenseCategory.Cleaning,
        Description = "Cleaning service"
    };

    [Fact]
    public async Task Totals_Aggregate_Payments_And_Expenses()
    {
        var month = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 15);
        var grain = GetFinanceGrain(NewBuildingKey(), month);

        await grain.RecordPaymentAsync(Payment("1", 3000, month));
        await grain.RecordPaymentAsync(Payment("1", 500, month));
        await grain.RecordPaymentAsync(Payment("2", 2000, month));
        await grain.RecordExpenseAsync(Expense(1500, month));

        var totals = await grain.GetMonthlyTotalsAsync();

        totals.TotalIncome.Should().Be(5500);
        totals.TotalExpenses.Should().Be(1500);
        totals.PayingUnits.Should().BeEquivalentTo(new[] { "1", "2" });
        totals.PaidAmountsByUnit.Should().BeEquivalentTo(new Dictionary<string, decimal>
        {
            ["1"] = 3500,
            ["2"] = 2000
        });
    }

    [Fact]
    public async Task Months_Are_Isolated_From_Each_Other()
    {
        var thisMonth = DateTime.UtcNow;
        var lastMonth = DateTime.UtcNow.AddMonths(-1);
        var buildingKey = NewBuildingKey();

        var current = GetFinanceGrain(buildingKey, thisMonth);
        var previous = GetFinanceGrain(buildingKey, lastMonth);

        await current.RecordPaymentAsync(Payment("1", 100, thisMonth));
        await previous.RecordPaymentAsync(Payment("1", 200, lastMonth));
        await previous.RecordPaymentAsync(Payment("3", 300, lastMonth));

        var currentTotals = await current.GetMonthlyTotalsAsync();
        var previousTotals = await previous.GetMonthlyTotalsAsync();

        currentTotals.TotalIncome.Should().Be(100);
        previousTotals.TotalIncome.Should().Be(500);
        currentTotals.PayingUnits.Should().BeEquivalentTo(new[] { "1" });
        previousTotals.PayingUnits.Should().BeEquivalentTo(new[] { "1", "3" });
    }

    [Fact]
    public async Task NonPositive_Amounts_Are_Rejected()
    {
        var grain = GetFinanceGrain(NewBuildingKey(), DateTime.UtcNow);

        var badPayment = () => grain.RecordPaymentAsync(Payment("1", 0, DateTime.UtcNow));
        var badExpense = () => grain.RecordExpenseAsync(Expense(-5, DateTime.UtcNow));

        await badPayment.Should().ThrowAsync<ArgumentException>();
        await badExpense.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Empty_Month_Returns_Zero_Totals()
    {
        var grain = GetFinanceGrain(NewBuildingKey(), DateTime.UtcNow.AddMonths(5));

        var totals = await grain.GetMonthlyTotalsAsync();

        totals.TotalIncome.Should().Be(0);
        totals.TotalExpenses.Should().Be(0);
        totals.PayingUnits.Should().BeEmpty();
        totals.PaidAmountsByUnit.Should().BeEmpty();
    }
}

public class BuildingFinanceSummaryClusterTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fixture;

    public BuildingFinanceSummaryClusterTests(ClusterFixture fixture)
    {
        _fixture = fixture;
    }

    private IGrainFactory GrainFactory => _fixture.Cluster.GrainFactory;

    private IBuildingGrain GetBuildingGrain() =>
        GrainFactory.GetGrain<IBuildingGrain>($"finance-summary-{Guid.NewGuid():N}");

    [Fact]
    public async Task Summary_Spans_Monthly_Grains_And_Respects_Ranges()
    {
        var grain = GetBuildingGrain();
        await grain.InitializeAsync(new BuildingInfo
        {
            Name = "Finance Building",
            Street = "Nemanjina",
            StreetNumber = "4",
            City = "Beograd",
            TotalFloors = 3,
            TotalAreaM2 = 500
        });

        var now = DateTime.UtcNow;
        var twoMonthsAgo = now.AddMonths(-2);

        await grain.RecordPaymentAsync(new PaymentInfo
        {
            Id = Guid.NewGuid(), UnitNumber = "1", Amount = 1000,
            PaymentDate = now, Month = now.Month, Year = now.Year
        });
        await grain.RecordPaymentAsync(new PaymentInfo
        {
            Id = Guid.NewGuid(), UnitNumber = "1", Amount = 2000,
            PaymentDate = twoMonthsAgo, Month = twoMonthsAgo.Month, Year = twoMonthsAgo.Year
        });

        // Default 12-month window includes both months.
        var yearly = await grain.GetFinancialSummaryAsync();
        yearly.TotalIncome.Should().Be(3000);

        // A range covering only the current month excludes the older payment.
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var currentMonthOnly = await grain.GetFinancialSummaryAsync(
            from: monthStart, to: monthStart.AddMonths(1).AddSeconds(-1));
        currentMonthOnly.TotalIncome.Should().Be(1000);
    }

    [Fact]
    public async Task Old_Unpinned_Announcements_Are_Pruned_On_Create()
    {
        var grain = GetBuildingGrain();
        await grain.InitializeAsync(new BuildingInfo
        {
            Name = "Announcement Building", Street = "N", StreetNumber = "1",
            City = "Beograd", TotalFloors = 1, TotalAreaM2 = 100
        });

        // An announcement over a year old (readable: no expiry set).
        await grain.CreateAnnouncementAsync(new AnnouncementInfo
        {
            Id = Guid.NewGuid(),
            Title = "Ancient notice",
            Content = "Old",
            Type = AnnouncementType.General,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddYears(-2)
        });

        // Creating a fresh announcement triggers pruning of the stale one.
        var freshId = Guid.NewGuid();
        await grain.CreateAnnouncementAsync(new AnnouncementInfo
        {
            Id = freshId,
            Title = "Fresh notice",
            Content = "New",
            Type = AnnouncementType.General,
            CreatedBy = Guid.NewGuid()
        });

        var visible = await grain.GetAnnouncementsAsync();
        visible.Should().ContainSingle(a => a.Id == freshId);
    }
}
