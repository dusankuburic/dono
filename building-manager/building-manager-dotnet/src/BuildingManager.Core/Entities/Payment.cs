using BuildingManager.Core.Enums;

namespace BuildingManager.Core.Entities;

public class Payment : BaseEntity
{
    public Guid BuildingId { get; private set; }
    public string UnitNumber { get; private set; } = string.Empty;
    public Guid? ResidentId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime PaymentDate { get; private set; }
    public int? Month { get; private set; }
    public int? Year { get; private set; }
    public PaymentMethod Method { get; private set; } = PaymentMethod.BankTransfer;
    public PaymentStatus Status { get; private set; } = PaymentStatus.Completed;
    public string? Reference { get; private set; }
    public string? Notes { get; private set; }
    public Guid RecordedBy { get; private set; }

    private Payment() { }

    public static Payment Create(Guid buildingId, string unitNumber, decimal amount, 
        DateTime paymentDate, Guid recordedBy, PaymentMethod method = PaymentMethod.BankTransfer,
        Guid? residentId = null, string? reference = null, int? month = null, int? year = null)
    {
        if (string.IsNullOrWhiteSpace(unitNumber))
            throw new ArgumentException("Unit number is required", nameof(unitNumber));
        
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        return new Payment
        {
            BuildingId = buildingId,
            UnitNumber = unitNumber,
            ResidentId = residentId,
            Amount = amount,
            PaymentDate = paymentDate,
            Method = method,
            Status = PaymentStatus.Completed,
            Reference = reference,
            Month = month ?? paymentDate.Month,
            Year = year ?? paymentDate.Year,
            RecordedBy = recordedBy
        };
    }

    public void MarkAsFailed(string? reason = null)
    {
        Status = PaymentStatus.Failed;
        Notes = reason;
        MarkAsUpdated();
    }

    public void MarkAsRefunded(string? reason = null)
    {
        Status = PaymentStatus.Refunded;
        Notes = reason;
        MarkAsUpdated();
    }

    public void AddNotes(string notes)
    {
        Notes = notes;
        MarkAsUpdated();
    }
}
