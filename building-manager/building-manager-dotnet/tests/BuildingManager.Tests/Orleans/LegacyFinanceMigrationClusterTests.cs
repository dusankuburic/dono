using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Grains;
using BuildingManager.Orleans.Grains.State;
using BuildingManager.Orleans.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;
using Orleans.TestingHost;
using Xunit;

namespace BuildingManager.Tests.Orleans;

public class LegacyFinanceMigrationClusterTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fixture;

    public LegacyFinanceMigrationClusterTests(ClusterFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Legacy_Payments_And_Expenses_Move_To_Finance_Grains_On_Activation()
    {
        var buildingId = $"legacy-{Guid.NewGuid():N}";
        var now = DateTime.UtcNow;
        var lastMonth = now.AddMonths(-1);

        var legacy = new BuildingAggregateState { Id = buildingId, Name = "Legacy Building", CreatedAt = now.AddYears(-1) };
        var thisMonthPayment = Guid.NewGuid();
        var lastMonthPayment = Guid.NewGuid();
        legacy.Payments[thisMonthPayment] = new PaymentState
        {
            Id = thisMonthPayment, UnitNumber = "1", Amount = 1000m,
            PaymentDate = now, Month = now.Month, Year = now.Year
        };
        legacy.Payments[lastMonthPayment] = new PaymentState
        {
            Id = lastMonthPayment, UnitNumber = "2", Amount = 500m,
            PaymentDate = lastMonth, Month = lastMonth.Month, Year = lastMonth.Year
        };
        var expenseId = Guid.NewGuid();
        legacy.Expenses[expenseId] = new ExpenseState
        {
            Id = expenseId, Amount = 300m, ExpenseDate = lastMonth,
            Category = ExpenseCategory.Cleaning, Description = "Stairwell cleaning"
        };

        var grain = _fixture.Cluster.GrainFactory.GetGrain<IBuildingGrain>(buildingId);
        var grainId = grain.GetGrainId();
        var storage = ((InProcessSiloHandle)_fixture.Cluster.Primary).SiloHost.Services
            .GetRequiredKeyedService<IGrainStorage>("buildingStorage");
        await storage.WriteStateAsync("building", grainId, new GrainState<BuildingAggregateState>(legacy));

        var summary = await grain.GetFinancialSummaryAsync();

        summary.TotalIncome.Should().Be(1500m);
        summary.TotalExpenses.Should().Be(300m);

        var stored = new GrainState<BuildingAggregateState>();
        await storage.ReadStateAsync("building", grainId, stored);
        stored.State.Payments.Should().BeEmpty();
        stored.State.Expenses.Should().BeEmpty();
        stored.State.Name.Should().Be("Legacy Building");
    }
}
