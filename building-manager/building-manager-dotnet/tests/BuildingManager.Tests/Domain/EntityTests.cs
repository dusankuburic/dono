using BuildingManager.Core.Entities;
using BuildingManager.Core.Enums;
using BuildingManager.Core.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BuildingManager.Tests.Domain;

public class KeyCheckoutTests
{
    private static Key CreateKey() => Key.Create(Guid.NewGuid(), KeyType.EntranceMain, "K-001");

    [Fact]
    public void Checkout_Marks_Key_As_CheckedOut()
    {
        var key = CreateKey();

        key.Checkout("Petar Petrovic", RecipientType.Resident, "Apartment viewing", DateTime.UtcNow.AddDays(1));

        key.Status.Should().Be(KeyStatus.CheckedOut);
        key.CurrentCheckout.Should().NotBeNull();
        key.CurrentCheckout!.RecipientName.Should().Be("Petar Petrovic");
    }

    [Fact]
    public void Checkout_Rejected_When_Key_Not_Available()
    {
        var key = CreateKey();
        key.Checkout("Petar Petrovic", RecipientType.Resident, "Viewing", DateTime.UtcNow.AddDays(1));

        var act = () => key.Checkout("Marko Markovic", RecipientType.Viewer, "Second viewing", DateTime.UtcNow.AddDays(1));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Checkin_Returns_Key_To_Available_And_Closes_Transaction()
    {
        var key = CreateKey();
        key.Checkout("Petar Petrovic", RecipientType.Resident, "Viewing", DateTime.UtcNow.AddDays(1));

        key.Checkin(condition: "Good", notes: "Returned on time");

        key.Status.Should().Be(KeyStatus.Available);
        key.CurrentCheckout.Should().BeNull();
        key.Transactions.Should().ContainSingle()
            .Which.ReturnedAt.Should().NotBeNull();
    }

    [Fact]
    public void Checkin_Rejected_When_Key_Not_CheckedOut()
    {
        var key = CreateKey();

        var act = () => key.Checkin();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Key_Is_Overdue_When_Past_ExpectedReturn()
    {
        var key = CreateKey();
        key.Checkout("Petar Petrovic", RecipientType.Resident, "Viewing", DateTime.UtcNow.AddHours(-1));

        key.IsOverdue.Should().BeTrue();
    }

    [Fact]
    public void Transaction_Requires_Recipient_Name()
    {
        var key = CreateKey();

        var act = () => key.Checkout("  ", RecipientType.Other, "Purpose", DateTime.UtcNow.AddDays(1));

        act.Should().Throw<ArgumentException>();
    }
}

public class WorkOrderTransitionTests
{
    private static WorkOrder CreateWorkOrder(bool isEmergency = false) =>
        WorkOrder.Create(Guid.NewGuid(), "Leak in bathroom", "Water leaking",
            WorkOrderCategory.Plumbing, Priority.High, Guid.NewGuid(), isEmergency: isEmergency);

    [Fact]
    public void Emergency_WorkOrder_Gets_48_Hour_Deadline()
    {
        var workOrder = CreateWorkOrder(isEmergency: true);

        workOrder.IsEmergency.Should().BeTrue();
        workOrder.EmergencyDeadline.Should().NotBeNull();
        workOrder.EmergencyDeadline!.Value.Should().BeCloseTo(DateTime.UtcNow.AddHours(48), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void Acknowledge_Only_Allowed_On_New_WorkOrders()
    {
        var workOrder = CreateWorkOrder();
        var userId = Guid.NewGuid();
        workOrder.StartWork(userId);

        var act = () => workOrder.Acknowledge(userId);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Complete_Records_Cost_And_Timestamp()
    {
        var workOrder = CreateWorkOrder();
        var userId = Guid.NewGuid();
        workOrder.StartWork(userId);

        workOrder.Complete(userId, actualCost: 5000m);

        workOrder.Status.Should().Be(WorkOrderStatus.Completed);
        workOrder.CompletedAt.Should().NotBeNull();
        workOrder.ActualCost.Should().Be(5000m);
    }

    [Fact]
    public void Completed_WorkOrder_Cannot_Be_Modified()
    {
        var workOrder = CreateWorkOrder();
        var userId = Guid.NewGuid();
        workOrder.StartWork(userId);
        workOrder.Complete(userId);

        var acts = new[]
        {
            () => workOrder.UpdatePriority(Priority.Emergency, userId),
            () => workOrder.SetEstimatedCost(100m, userId),
            () => workOrder.Complete(userId),
            () => workOrder.Cancel(userId, "changed mind")
        };

        acts.Should().AllSatisfy(act => act.Should().Throw<InvalidOperationException>());
    }

    [Fact]
    public void Cancel_Requires_Reason()
    {
        var workOrder = CreateWorkOrder();

        var act = () => workOrder.Cancel(Guid.NewGuid(), "  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Every_Status_Change_Adds_An_Audit_Note()
    {
        var workOrder = CreateWorkOrder();
        var userId = Guid.NewGuid();

        workOrder.Acknowledge(userId);
        workOrder.StartWork(userId);

        workOrder.Notes.Should().HaveCount(2);
        workOrder.Notes.Should().OnlyContain(n => n.CreatedBy == userId);
    }
}

public class BuildingAndUnitTests
{
    [Fact]
    public void AddUnit_Rejects_Duplicate_Numbers()
    {
        var building = Building.Create("Zgrada 1",
            new Address("Nemanjina", "4", "Beograd", "11000"), totalFloors: 5, totalAreaM2: 1200);
        var unit = Unit.Create(building.Id, "12", 1, 55m, 5m, UnitType.Apartment);
        building.AddUnit(unit);
        var duplicate = Unit.Create(building.Id, "12", 1, 60m, 6m, UnitType.Apartment);

        var act = () => building.AddUnit(duplicate);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Unit_Create_Validates_Area_And_Share()
    {
        var buildingId = Guid.NewGuid();

        var invalidArea = () => Unit.Create(buildingId, "1", 1, areaM2: 0, ownershipShare: 5m, UnitType.Apartment);
        var invalidShare = () => Unit.Create(buildingId, "1", 1, areaM2: 50m, ownershipShare: 101m, UnitType.Apartment);

        invalidArea.Should().Throw<ArgumentException>();
        invalidShare.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Unit_UpdateDetails_Enforces_Same_Rules_As_Create()
    {
        var unit = Unit.Create(Guid.NewGuid(), "1", 1, 50m, 5m, UnitType.Apartment);

        var act = () => unit.UpdateDetails(areaM2: -10, ownershipShare: 5m, UnitType.Apartment);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateBasicInfo_Rejects_Empty_Name()
    {
        var building = Building.Create("Zgrada 1",
            new Address("Nemanjina", "4", "Beograd", "11000"), 5, 1200m);

        var act = () => building.UpdateBasicInfo("  ", yearBuilt: 2000, hasElevator: true, hasParking: false);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddResident_Sets_Unit_Status_By_Role()
    {
        var unit = Unit.Create(Guid.NewGuid(), "1", 1, 50m, 5m, UnitType.Apartment);
        var tenant = Resident.Create(Guid.NewGuid(), unit.Id, "Ana", "Anic", ResidentRole.Tenant);

        unit.AddResident(tenant);

        unit.Status.Should().Be(UnitStatus.OccupiedByTenant);
    }

    [Fact]
    public void Resident_Becomes_Unavailable_After_Three_Missed_Assemblies()
    {
        var resident = Resident.Create(Guid.NewGuid(), null, "Mika", "Mikic", ResidentRole.Owner);

        for (var i = 0; i < 3; i++)
        {
            resident.RecordAssemblyAttendance(attended: false);
        }

        resident.IsUnavailableOwner.Should().BeTrue();

        resident.RecordAssemblyAttendance(attended: true);

        resident.IsUnavailableOwner.Should().BeFalse("attending an assembly resets the missed count");
    }
}

public class FinanceTests
{
    [Fact]
    public void FeeStructure_Combines_Fixed_And_Area_Based_Amounts()
    {
        var fee = FeeStructure.Create(Guid.NewGuid(), "Standard", amountPerM2: 0.50m,
            effectiveFrom: new DateTime(2026, 1, 1), fixedAmount: 200m);

        fee.CalculateFee(areaM2: 60m).Should().Be(200m + 60m * 0.50m);
    }

    [Fact]
    public void Expense_Cannot_Be_Approved_Twice()
    {
        var expense = Expense.Create(Guid.NewGuid(), 100m, DateTime.UtcNow,
            ExpenseCategory.Cleaning, "Cleaning service", Guid.NewGuid());

        expense.Approve(Guid.NewGuid());

        var act = () => expense.Approve(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Approved_Expense_Is_Locked()
    {
        var expense = Expense.Create(Guid.NewGuid(), 100m, DateTime.UtcNow,
            ExpenseCategory.Cleaning, "Cleaning service", Guid.NewGuid());
        expense.Approve(Guid.NewGuid());

        var act = () => expense.UpdateDetails(120m, ExpenseCategory.Maintenance, "Updated");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Payment_Requires_Positive_Amount()
    {
        var act = () => Payment.Create(Guid.NewGuid(), "12", amount: 0,
            paymentDate: DateTime.UtcNow, recordedBy: Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Payment_Defaults_Month_And_Year_From_Payment_Date()
    {
        var paymentDate = new DateTime(2026, 3, 15);

        var payment = Payment.Create(Guid.NewGuid(), "12", 3000m, paymentDate, Guid.NewGuid());

        payment.Month.Should().Be(3);
        payment.Year.Should().Be(2026);
    }
}
