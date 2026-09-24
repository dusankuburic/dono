using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;
using FluentAssertions;
using Orleans;
using Xunit;

namespace BuildingManager.Tests.Orleans;

/// <summary>
/// Grain-level integration tests running against an in-process Orleans cluster.
/// These exercise the full invocation pipeline (serialization, activation,
/// state round-trips) rather than just the domain logic.
/// </summary>
public class BuildingGrainClusterTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fixture;

    public BuildingGrainClusterTests(ClusterFixture fixture)
    {
        _fixture = fixture;
    }

    private IGrainFactory GrainFactory => _fixture.Cluster.GrainFactory;

    private IBuildingGrain GetBuildingGrain() =>
        GrainFactory.GetGrain<IBuildingGrain>($"test-{Guid.NewGuid():N}");

    private static BuildingInfo BuildingInfo(string name = "Test Building") => new()
    {
        Name = name,
        Street = "Nemanjina",
        StreetNumber = "4",
        City = "Beograd",
        TotalFloors = 5,
        TotalAreaM2 = 1200
    };

    private static UnitInfo UnitInfo(string number, decimal areaM2, decimal share) => new()
    {
        Id = Guid.NewGuid(),
        Number = number,
        Floor = 1,
        AreaM2 = areaM2,
        OwnershipShare = share,
        Type = UnitType.Apartment,
        Status = UnitStatus.Vacant
    };

    [Fact]
    public async Task Initialize_Building_Registers_In_Registry_And_Returns_Summary()
    {
        var grain = GetBuildingGrain();
        var key = grain.GetPrimaryKeyString();

        await grain.InitializeAsync(BuildingInfo("Registry Test"));

        var registry = GrainFactory.GetGrain<IBuildingRegistryGrain>("global");
        var all = await registry.GetAllAsync();
        all.Should().Contain(e => e.Id == key && e.Name == "Registry Test");

        var summary = await grain.GetSummaryAsync();
        summary.Id.Should().Be(key);
        summary.Name.Should().Be("Registry Test");
    }

    [Fact]
    public async Task GetSummary_Of_Uninitialized_Building_Throws_KeyNotFound()
    {
        var grain = GetBuildingGrain();

        var act = () => grain.GetSummaryAsync();

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ReInitialize_Is_Rejected()
    {
        var grain = GetBuildingGrain();
        await grain.InitializeAsync(BuildingInfo());

        var act = () => grain.InitializeAsync(BuildingInfo("Second attempt"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task List_Endpoints_Return_Materialized_Collections()
    {
        // Regression test: returning lazy LINQ iterators from grain methods
        // fails with CodecNotFoundException at invocation time.
        var grain = GetBuildingGrain();
        await grain.InitializeAsync(BuildingInfo());
        await grain.AddOrUpdateUnitAsync(UnitInfo("1", 50m, 5m));
        await grain.AddOrUpdateUnitAsync(UnitInfo("2", 60m, 6m));

        var units = await grain.GetAllUnitsAsync();
        units.Should().HaveCount(2);

        var residents = await grain.GetResidentsAsync();
        residents.Should().BeEmpty();

        var keys = await grain.GetAllKeysAsync();
        keys.Should().BeEmpty();
    }

    [Fact]
    public async Task Key_Checkout_Checkin_Roundtrip()
    {
        var grain = GetBuildingGrain();
        await grain.InitializeAsync(BuildingInfo());
        var keyId = Guid.NewGuid();
        await grain.AddKeyAsync(new KeyInfo
        {
            Id = keyId,
            Type = KeyType.EntranceMain,
            Identifier = "K-001",
            Status = KeyStatus.Available
        });

        var checkedOut = await grain.CheckoutKeyAsync(keyId, new KeyCheckoutRequest
        {
            RecipientName = "Petar Petrovic",
            RecipientType = RecipientType.Resident,
            Purpose = "Viewing",
            ExpectedReturn = DateTime.UtcNow.AddDays(1)
        });
        checkedOut.Status.Should().Be(KeyStatus.CheckedOut);
        checkedOut.CurrentCheckout.Should().NotBeNull();

        var doubleCheckout = () => grain.CheckoutKeyAsync(keyId, new KeyCheckoutRequest
        {
            RecipientName = "Other",
            RecipientType = RecipientType.Viewer,
            Purpose = "X",
            ExpectedReturn = DateTime.UtcNow.AddDays(1)
        });
        await doubleCheckout.Should().ThrowAsync<InvalidOperationException>();

        var checkedIn = await grain.CheckinKeyAsync(keyId, "Good", "On time");
        checkedIn.Status.Should().Be(KeyStatus.Available);
        checkedIn.CurrentCheckout.Should().BeNull();
    }

    [Fact]
    public async Task Key_Checkout_Rejects_Past_Expected_Return()
    {
        var grain = GetBuildingGrain();
        await grain.InitializeAsync(BuildingInfo());
        var keyId = Guid.NewGuid();
        await grain.AddKeyAsync(new KeyInfo { Id = keyId, Type = KeyType.Unit, Identifier = "K-2", Status = KeyStatus.Available });

        var act = () => grain.CheckoutKeyAsync(keyId, new KeyCheckoutRequest
        {
            RecipientName = "Ana",
            RecipientType = RecipientType.Resident,
            Purpose = "X",
            ExpectedReturn = DateTime.UtcNow.AddDays(-1)
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task WorkOrder_Status_Cannot_Be_Changed_After_Completion()
    {
        var grain = GetBuildingGrain();
        await grain.InitializeAsync(BuildingInfo());

        var created = await grain.CreateWorkOrderAsync(new CreateWorkOrderRequest
        {
            Title = "Leak",
            Description = "Bathroom",
            Category = WorkOrderCategory.Plumbing,
            Priority = Priority.High,
            ReportedByResidentId = Guid.NewGuid()
        });

        var completed = await grain.UpdateWorkOrderStatusAsync(created.Id, WorkOrderStatus.Completed, "done");
        completed.Status.Should().Be(WorkOrderStatus.Completed);

        var act = () => grain.UpdateWorkOrderStatusAsync(created.Id, WorkOrderStatus.InProgress, "reopen");
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task FinancialSummary_Tracks_Income_Expenses_And_Balance()
    {
        var grain = GetBuildingGrain();
        await grain.InitializeAsync(BuildingInfo());
        var now = DateTime.UtcNow;

        await grain.RecordPaymentAsync(new PaymentInfo
        {
            Id = Guid.NewGuid(),
            UnitNumber = "1",
            Amount = 3000,
            PaymentDate = now,
            Month = now.Month,
            Year = now.Year
        });
        await grain.RecordExpenseAsync(new ExpenseInfo
        {
            Id = Guid.NewGuid(),
            Amount = 1500,
            ExpenseDate = now,
            Category = ExpenseCategory.Cleaning,
            Description = "Cleaning service"
        });

        var summary = await grain.GetFinancialSummaryAsync();

        summary.TotalIncome.Should().Be(3000);
        summary.TotalExpenses.Should().Be(1500);
        summary.Balance.Should().Be(1500);
    }

    [Fact]
    public async Task OutstandingReceivables_Reflects_Fee_Structure_And_Payments()
    {
        var grain = GetBuildingGrain();
        await grain.InitializeAsync(BuildingInfo());
        var now = DateTime.UtcNow;

        // No fee structure yet: nothing outstanding.
        (await grain.GetFinancialSummaryAsync()).OutstandingReceivables.Should().Be(0);

        await grain.AddOrUpdateUnitAsync(UnitInfo("1", 100m, 10m));
        await grain.AddOrUpdateUnitAsync(UnitInfo("2", 50m, 5m));
        await grain.SetFeeStructureAsync(new FeeStructureInfo
        {
            Name = "Standard",
            AmountPerM2 = 10m,
            FixedAmount = 200m,
            EffectiveFrom = now
        });

        // Expected: (200 + 10*100) + (200 + 10*50) = 1200 + 700 = 1900.
        var beforePayment = await grain.GetFinancialSummaryAsync();
        beforePayment.OutstandingReceivables.Should().Be(1900m);

        // Unit 1 pays its full 1200 this month: only unit 2 remains outstanding.
        await grain.RecordPaymentAsync(new PaymentInfo
        {
            Id = Guid.NewGuid(),
            UnitNumber = "1",
            Amount = 1200m,
            PaymentDate = now,
            Month = now.Month,
            Year = now.Year
        });

        var afterPayment = await grain.GetFinancialSummaryAsync();
        afterPayment.OutstandingReceivables.Should().Be(700m);
    }

    [Fact]
    public async Task FeeStructure_Validation()
    {
        var grain = GetBuildingGrain();
        await grain.InitializeAsync(BuildingInfo());

        var emptyName = () => grain.SetFeeStructureAsync(new FeeStructureInfo
        {
            Name = "  ",
            AmountPerM2 = 10m,
            EffectiveFrom = DateTime.UtcNow
        });
        await emptyName.Should().ThrowAsync<ArgumentException>();

        var negativeAmount = () => grain.SetFeeStructureAsync(new FeeStructureInfo
        {
            Name = "Bad",
            AmountPerM2 = -1m,
            EffectiveFrom = DateTime.UtcNow
        });
        await negativeAmount.Should().ThrowAsync<ArgumentException>();
    }
}
