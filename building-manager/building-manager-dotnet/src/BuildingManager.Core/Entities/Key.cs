using BuildingManager.Core.Enums;

namespace BuildingManager.Core.Entities;

public class Key : BaseEntity
{
    public Guid BuildingId { get; private set; }
    public KeyType Type { get; private set; }
    public string? UnitNumber { get; private set; }
    public string Identifier { get; private set; } = string.Empty;
    public KeyStatus Status { get; private set; } = KeyStatus.Available;
    public string? Description { get; private set; }
    
    private readonly List<KeyTransaction> _transactions = new();
    public IReadOnlyCollection<KeyTransaction> Transactions => _transactions.AsReadOnly();
    
    public KeyTransaction? CurrentCheckout => _transactions
        .Where(t => t.ReturnedAt == null)
        .OrderByDescending(t => t.CheckedOutAt)
        .FirstOrDefault();

    private Key() { }

    public static Key Create(Guid buildingId, KeyType type, string identifier, string? unitNumber = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Key identifier is required", nameof(identifier));

        return new Key
        {
            BuildingId = buildingId,
            Type = type,
            Identifier = identifier,
            UnitNumber = unitNumber,
            Description = description,
            Status = KeyStatus.Available
        };
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
        MarkAsUpdated();
    }

    public KeyTransaction Checkout(string recipientName, RecipientType recipientType, string purpose, 
        DateTime expectedReturn, string? recipientPhone = null, Guid? recipientResidentId = null)
    {
        if (Status != KeyStatus.Available)
            throw new InvalidOperationException($"Cannot checkout key with status {Status}");

        var transaction = KeyTransaction.Create(
            Id, recipientName, recipientType, purpose, expectedReturn, recipientPhone, recipientResidentId);
        
        _transactions.Add(transaction);
        Status = KeyStatus.CheckedOut;
        MarkAsUpdated();
        
        return transaction;
    }

    public void Checkin(string? condition = null, string? notes = null)
    {
        if (Status != KeyStatus.CheckedOut)
            throw new InvalidOperationException("Key is not checked out");

        var currentCheckout = CurrentCheckout;
        if (currentCheckout != null)
        {
            currentCheckout.Return(condition, notes);
        }

        Status = KeyStatus.Available;
        MarkAsUpdated();
    }

    public void MarkAsLost()
    {
        Status = KeyStatus.Lost;
        MarkAsUpdated();
    }

    public void MarkAsDamaged()
    {
        Status = KeyStatus.Damaged;
        MarkAsUpdated();
    }

    public void MarkAsInRepair()
    {
        Status = KeyStatus.InRepair;
        MarkAsUpdated();
    }

    public void MarkAsAvailable()
    {
        Status = KeyStatus.Available;
        MarkAsUpdated();
    }

    public bool IsOverdue => CurrentCheckout != null && CurrentCheckout.IsOverdue;
}
