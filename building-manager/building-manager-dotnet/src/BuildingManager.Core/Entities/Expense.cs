using BuildingManager.Core.Enums;

namespace BuildingManager.Core.Entities;

public class Expense : BaseEntity
{
    public Guid BuildingId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime ExpenseDate { get; private set; }
    public ExpenseCategory Category { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? Vendor { get; private set; }
    public string? InvoiceNumber { get; private set; }
    public string? ReceiptUrl { get; private set; }
    public Guid? WorkOrderId { get; private set; }
    public Guid RecordedBy { get; private set; }
    public bool IsApproved { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }

    private Expense() { }

    public static Expense Create(Guid buildingId, decimal amount, DateTime expenseDate,
        ExpenseCategory category, string description, Guid recordedBy,
        string? vendor = null, string? invoiceNumber = null, Guid? workOrderId = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));
        
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required", nameof(description));

        return new Expense
        {
            BuildingId = buildingId,
            Amount = amount,
            ExpenseDate = expenseDate,
            Category = category,
            Description = description,
            Vendor = vendor,
            InvoiceNumber = invoiceNumber,
            WorkOrderId = workOrderId,
            RecordedBy = recordedBy,
            IsApproved = false
        };
    }

    public void Approve(Guid approvedBy)
    {
        if (IsApproved)
            throw new InvalidOperationException("Expense is already approved");

        IsApproved = true;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void AttachReceipt(string receiptUrl)
    {
        ReceiptUrl = receiptUrl;
        MarkAsUpdated();
    }

    public void UpdateDetails(decimal amount, ExpenseCategory category, string description)
    {
        if (IsApproved)
            throw new InvalidOperationException("Cannot update approved expense");

        Amount = amount;
        Category = category;
        Description = description;
        MarkAsUpdated();
    }
}
