using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;
using FluentAssertions;
using Orleans;
using Xunit;

namespace BuildingManager.Tests.Orleans;

public class BuildingSetupClusterTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fixture;

    public BuildingSetupClusterTests(ClusterFixture fixture)
    {
        _fixture = fixture;
    }

    private IGrainFactory GrainFactory => _fixture.Cluster.GrainFactory;

    private async Task<IBuildingGrain> NewBuilding(params string[] units)
    {
        var grain = GrainFactory.GetGrain<IBuildingGrain>($"setup-{Guid.NewGuid():N}");
        await grain.InitializeAsync(new BuildingInfo
        {
            Name = "Setup Building", Street = "Bulevar", StreetNumber = "7", City = "Novi Sad", TotalFloors = 4
        });
        foreach (var number in units)
        {
            await grain.AddOrUpdateUnitAsync(Unit(number, 50m, 10m));
        }
        return grain;
    }

    private static UnitInfo Unit(string number, decimal area, decimal share) => new()
    {
        Number = number, Floor = 1, AreaM2 = area, OwnershipShare = share,
        Type = UnitType.Apartment, Status = UnitStatus.OccupiedByOwner
    };

    private static ResidentInfo Resident(string unit, string first = "Ana") => new()
    {
        FirstName = first, LastName = "Jovanovic", UnitNumber = unit, Role = ResidentRole.Owner,
        Email = $"{first.ToLowerInvariant()}@example.com"
    };

    [Fact]
    public async Task Building_Details_Can_Be_Read_And_Updated_Including_Registry_Name()
    {
        var grain = await NewBuilding();

        await grain.UpdateInfoAsync(new BuildingInfo
        {
            Name = "Renamed", Street = "Bulevar", StreetNumber = "7a", City = "Novi Sad",
            PostalCode = "21000", TotalFloors = 6, TotalAreaM2 = 900, HasElevator = true
        });

        var info = await grain.GetInfoAsync();
        info.Name.Should().Be("Renamed");
        info.StreetNumber.Should().Be("7a");
        info.TotalFloors.Should().Be(6);
        info.TotalAreaM2.Should().Be(900);
        info.HasElevator.Should().BeTrue();

        var registry = await GrainFactory.GetGrain<IBuildingRegistryGrain>("global").GetAllAsync();
        registry.Should().Contain(e => e.Id == grain.GetPrimaryKeyString() && e.Name == "Renamed");
    }

    [Fact]
    public async Task Updating_An_Uninitialized_Building_Is_Not_Found()
    {
        var grain = GrainFactory.GetGrain<IBuildingGrain>($"setup-missing-{Guid.NewGuid():N}");
        var act = () => grain.UpdateInfoAsync(new BuildingInfo { Name = "X" });
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Updating_A_Unit_Keeps_Its_Id_And_Validates_Input()
    {
        var grain = await NewBuilding("1");
        var before = await grain.GetUnitAsync("1");

        await grain.AddOrUpdateUnitAsync(Unit("1", 64m, 12.5m) with { Id = Guid.NewGuid() });

        var after = await grain.GetUnitAsync("1");
        after.Id.Should().Be(before.Id);
        after.AreaM2.Should().Be(64m);
        after.OwnershipShare.Should().Be(12.5m);

        await grain.Invoking(g => g.AddOrUpdateUnitAsync(Unit("2", 50m, 120m)))
            .Should().ThrowAsync<ArgumentException>();
        await grain.Invoking(g => g.AddOrUpdateUnitAsync(Unit(" ", 50m, 10m)))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task A_Unit_With_Residents_Cannot_Be_Removed_Until_They_Move_Out()
    {
        var grain = await NewBuilding("1");
        var resident = Resident("1") with { Id = Guid.NewGuid() };
        await grain.AddResidentAsync(resident);

        await grain.Invoking(g => g.RemoveUnitAsync("1")).Should().ThrowAsync<InvalidOperationException>();

        await grain.RemoveResidentAsync(resident.Id);
        await grain.RemoveUnitAsync("1");

        (await grain.GetAllUnitsAsync()).Should().BeEmpty();
        await grain.Invoking(g => g.RemoveUnitAsync("1")).Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Residents_Must_Live_In_An_Existing_Unit_And_Can_Be_Edited()
    {
        var grain = await NewBuilding("1", "2");

        await grain.Invoking(g => g.AddResidentAsync(Resident("9") with { Id = Guid.NewGuid() }))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*Unit 9*");

        var id = Guid.NewGuid();
        await grain.AddResidentAsync(Resident("1") with { Id = id });
        await grain.UpdateResidentAsync(Resident("2", "Marko") with { Id = id, Phone = " 064 123 " });

        var resident = (await grain.GetResidentsAsync()).Single();
        resident.FirstName.Should().Be("Marko");
        resident.UnitNumber.Should().Be("2");
        resident.Phone.Should().Be("064 123");

        await grain.RemoveResidentAsync(id);
        (await grain.GetResidentsAsync()).Should().BeEmpty();
        await grain.Invoking(g => g.UpdateResidentAsync(Resident("2") with { Id = id }))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Key_Labels_Are_Unique_And_Keys_Belong_To_Existing_Units()
    {
        var grain = await NewBuilding("1");
        await grain.AddKeyAsync(new KeyInfo { Id = Guid.NewGuid(), Type = KeyType.Unit, Identifier = "K-1", UnitNumber = "1" });

        await grain.Invoking(g => g.AddKeyAsync(new KeyInfo { Id = Guid.NewGuid(), Type = KeyType.Roof, Identifier = "k-1" }))
            .Should().ThrowAsync<InvalidOperationException>();
        await grain.Invoking(g => g.AddKeyAsync(new KeyInfo { Id = Guid.NewGuid(), Type = KeyType.Unit, Identifier = "K-2", UnitNumber = "7" }))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Lost_Key_Stays_Unavailable_And_History_Lists_Every_Handout()
    {
        var grain = await NewBuilding();
        var keyId = Guid.NewGuid();
        await grain.AddKeyAsync(new KeyInfo { Id = keyId, Type = KeyType.EntranceMain, Identifier = "M-1" });

        var checkout = new KeyCheckoutRequest
        {
            RecipientName = "Plumber", RecipientType = RecipientType.Contractor, Purpose = "Riser",
            ExpectedReturn = DateTime.UtcNow.AddHours(4)
        };
        await grain.CheckoutKeyAsync(keyId, checkout);
        await grain.CheckinKeyAsync(keyId, "Good", null);
        await grain.CheckoutKeyAsync(keyId, checkout with { RecipientName = "Viewer", RecipientType = RecipientType.Viewer });

        await grain.Invoking(g => g.SetKeyStatusAsync(keyId, KeyStatus.Lost, null))
            .Should().ThrowAsync<InvalidOperationException>();

        var lost = await grain.CheckinKeyAsync(keyId, "Lost", "Not returned");
        lost.Status.Should().Be(KeyStatus.Lost);
        lost.StatusNote.Should().Be("Not returned");
        await grain.Invoking(g => g.CheckoutKeyAsync(keyId, checkout)).Should().ThrowAsync<InvalidOperationException>();

        var history = await grain.GetKeyHistoryAsync(keyId);
        history.Select(h => h.RecipientName).Should().Equal("Viewer", "Plumber");
        history[0].ReturnCondition.Should().Be("Lost");

        var replaced = await grain.SetKeyStatusAsync(keyId, KeyStatus.Available, "Ignored for available keys");
        replaced.Status.Should().Be(KeyStatus.Available);
        replaced.StatusNote.Should().BeNull();
        await grain.Invoking(g => g.SetKeyStatusAsync(keyId, KeyStatus.CheckedOut, null))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Work_Orders_Get_Yearly_Numbers_And_Keep_Notes_With_Status_Changes()
    {
        var grain = await NewBuilding("3");
        var request = new CreateWorkOrderRequest
        {
            Title = "Leak", Description = "Ceiling", Category = WorkOrderCategory.Plumbing,
            Priority = Priority.Normal, UnitNumber = "3", ReportedByName = "Ana"
        };

        var first = await grain.CreateWorkOrderAsync(request);
        var second = await grain.CreateWorkOrderAsync(request with { Title = "Light" });
        var year = DateTime.UtcNow.Year;
        first.ReferenceNumber.Should().Be($"WO-{year}-0001");
        second.ReferenceNumber.Should().Be($"WO-{year}-0002");
        first.ReportedByName.Should().Be("Ana");

        await grain.UpdateWorkOrderStatusAsync(first.Id, WorkOrderStatus.Acknowledged, "Plumber booked", "Manager M");
        await grain.UpdateWorkOrderStatusAsync(first.Id, WorkOrderStatus.Acknowledged, "Comes Thursday", "Manager M");
        var unchanged = await grain.UpdateWorkOrderStatusAsync(first.Id, WorkOrderStatus.Acknowledged, "  ", "Manager M");
        unchanged.Notes.Should().HaveCount(2, "a blank note without a status change records nothing");

        var done = await grain.UpdateWorkOrderStatusAsync(first.Id, WorkOrderStatus.Completed, null, "Manager M");
        done.CompletedAt.Should().NotBeNull();
        done.Notes.Select(n => (n.Status, n.Text)).Should().Equal(
            (WorkOrderStatus.Acknowledged, "Plumber booked"),
            (null, "Comes Thursday"),
            (WorkOrderStatus.Completed, null));
        done.Notes.Should().OnlyContain(n => n.AuthorName == "Manager M");

        (await grain.GetWorkOrdersAsync()).Select(w => w.Id).Should().Equal(second.Id, first.Id);
    }

    [Fact]
    public async Task Emergency_Priority_Starts_The_48_Hour_Window_And_Unknown_Units_Are_Rejected()
    {
        var grain = await NewBuilding("1");

        var emergency = await grain.CreateWorkOrderAsync(new CreateWorkOrderRequest
        {
            Title = "Flooding", Category = WorkOrderCategory.Plumbing, Priority = Priority.Emergency
        });
        emergency.IsEmergency.Should().BeTrue();
        emergency.EmergencyDeadline.Should().BeCloseTo(DateTime.UtcNow.AddHours(48), TimeSpan.FromMinutes(1));

        await grain.Invoking(g => g.CreateWorkOrderAsync(new CreateWorkOrderRequest
        {
            Title = "X", Category = WorkOrderCategory.Other, Priority = Priority.Low, UnitNumber = "42"
        })).Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task A_Scheduled_Fee_Applies_From_Its_Month_And_Rescheduling_Replaces_It()
    {
        var grain = await NewBuilding("1");
        var now = DateTime.UtcNow;
        var nextMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);

        await grain.SetFeeStructureAsync(new FeeStructureInfo { Name = "Now", AmountPerM2 = 10 });
        await grain.SetFeeStructureAsync(new FeeStructureInfo { Name = "Next", AmountPerM2 = 20, EffectiveFrom = nextMonth });
        await grain.SetFeeStructureAsync(new FeeStructureInfo { Name = "Next (fixed)", AmountPerM2 = 30, EffectiveFrom = nextMonth });

        var fees = await grain.GetFeeStructuresAsync();
        fees.Select(f => f.Name).Should().Equal("Next (fixed)", "Now");

        (await grain.GetPaymentStatusAsync(now.Year, now.Month)).Units.Single().Expected.Should().Be(500m);
        (await grain.GetPaymentStatusAsync(nextMonth.Year, nextMonth.Month)).Units.Single().Expected.Should().Be(1500m);
    }
}
