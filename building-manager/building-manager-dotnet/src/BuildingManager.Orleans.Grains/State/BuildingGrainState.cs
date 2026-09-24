using BuildingManager.Core.Enums;

namespace BuildingManager.Orleans.Grains.State;

[GenerateSerializer]
public class UnitState
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string Number { get; set; } = string.Empty;
    [Id(2)] public int Floor { get; set; }
    [Id(3)] public string? Entrance { get; set; }
    [Id(4)] public decimal AreaM2 { get; set; }
    [Id(5)] public decimal OwnershipShare { get; set; }
    [Id(6)] public UnitType Type { get; set; } = UnitType.Apartment;
    [Id(7)] public UnitStatus Status { get; set; } = UnitStatus.Vacant;
}

[GenerateSerializer]
public class ResidentState
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string? UnitNumber { get; set; }
    [Id(2)] public string FirstName { get; set; } = string.Empty;
    [Id(3)] public string LastName { get; set; } = string.Empty;
    [Id(4)] public string? Phone { get; set; }
    [Id(5)] public string? Email { get; set; }
    [Id(6)] public ResidentRole Role { get; set; } = ResidentRole.Owner;
    [Id(7)] public decimal? OwnershipShare { get; set; }
    [Id(8)] public bool IsActive { get; set; } = true;
}

[GenerateSerializer]
public class KeyState
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public KeyType Type { get; set; }
    [Id(2)] public string? UnitNumber { get; set; }
    [Id(3)] public string Identifier { get; set; } = string.Empty;
    [Id(4)] public KeyStatus Status { get; set; } = KeyStatus.Available;
    [Id(5)] public string? Description { get; set; }
    [Id(6)] public KeyCheckoutState? CurrentCheckout { get; set; }
    [Id(7)] public List<KeyCheckoutState> CheckoutHistory { get; set; } = new();
}

[GenerateSerializer]
public class KeyCheckoutState
{
    [Id(0)] public Guid TransactionId { get; set; }
    [Id(1)] public string RecipientName { get; set; } = string.Empty;
    [Id(2)] public string? RecipientPhone { get; set; }
    [Id(3)] public Guid? RecipientResidentId { get; set; }
    [Id(4)] public RecipientType RecipientType { get; set; }
    [Id(5)] public string Purpose { get; set; } = string.Empty;
    [Id(6)] public DateTime CheckedOutAt { get; set; }
    [Id(7)] public DateTime ExpectedReturn { get; set; }
    [Id(8)] public DateTime? ReturnedAt { get; set; }
    [Id(9)] public string? ReturnCondition { get; set; }
    [Id(10)] public string? ReturnNotes { get; set; }
}

[GenerateSerializer]
public class WorkOrderState
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string ReferenceNumber { get; set; } = string.Empty;
    [Id(2)] public string Title { get; set; } = string.Empty;
    [Id(3)] public string Description { get; set; } = string.Empty;
    [Id(4)] public WorkOrderCategory Category { get; set; }
    [Id(5)] public Priority Priority { get; set; } = Priority.Normal;
    [Id(6)] public WorkOrderStatus Status { get; set; } = WorkOrderStatus.New;
    [Id(7)] public string? UnitNumber { get; set; }
    [Id(8)] public Guid ReportedByResidentId { get; set; }
    [Id(9)] public Guid? AssignedToId { get; set; }
    [Id(10)] public bool IsEmergency { get; set; }
    [Id(11)] public DateTime? EmergencyDeadline { get; set; }
    [Id(12)] public decimal? EstimatedCost { get; set; }
    [Id(13)] public decimal? ActualCost { get; set; }
    [Id(14)] public DateTime CreatedAt { get; set; }
    [Id(15)] public DateTime? CompletedAt { get; set; }
}

[GenerateSerializer]
public class AnnouncementState
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string Title { get; set; } = string.Empty;
    [Id(2)] public string Content { get; set; } = string.Empty;
    [Id(3)] public AnnouncementType Type { get; set; } = AnnouncementType.General;
    [Id(4)] public DateTime CreatedAt { get; set; }
    [Id(5)] public DateTime? ScheduledFor { get; set; }
    [Id(6)] public DateTime? SentAt { get; set; }
    [Id(7)] public Guid CreatedBy { get; set; }
    [Id(8)] public bool IsPinned { get; set; }
    [Id(9)] public DateTime? ExpiresAt { get; set; }
}

[GenerateSerializer]
public class PaymentState
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string UnitNumber { get; set; } = string.Empty;
    [Id(2)] public decimal Amount { get; set; }
    [Id(3)] public DateTime PaymentDate { get; set; }
    [Id(4)] public int? Month { get; set; }
    [Id(5)] public int? Year { get; set; }
    [Id(6)] public string? Reference { get; set; }
}

[GenerateSerializer]
public class ExpenseState
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public decimal Amount { get; set; }
    [Id(2)] public DateTime ExpenseDate { get; set; }
    [Id(3)] public ExpenseCategory Category { get; set; }
    [Id(4)] public string Description { get; set; } = string.Empty;
    [Id(5)] public string? Vendor { get; set; }
    [Id(6)] public bool IsApproved { get; set; }
}

[GenerateSerializer]
public class FeeStructureState
{
    [Id(0)] public string Name { get; set; } = string.Empty;
    [Id(1)] public decimal AmountPerM2 { get; set; }
    [Id(2)] public decimal? FixedAmount { get; set; }
    [Id(3)] public DateTime EffectiveFrom { get; set; }
    [Id(4)] public bool IsActive { get; set; } = true;
}
