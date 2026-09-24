using BuildingManager.Core.Enums;

namespace BuildingManager.Core.Entities;

public class KeyTransaction : BaseEntity
{
    public Guid KeyId { get; private set; }
    public string RecipientName { get; private set; } = string.Empty;
    public string? RecipientPhone { get; private set; }
    public Guid? RecipientResidentId { get; private set; }
    public RecipientType RecipientType { get; private set; }
    public string Purpose { get; private set; } = string.Empty;
    public DateTime CheckedOutAt { get; private set; }
    public DateTime ExpectedReturn { get; private set; }
    public DateTime? ReturnedAt { get; private set; }
    public string? ReturnCondition { get; private set; }
    public string? Notes { get; private set; }

    private KeyTransaction() { }

    internal static KeyTransaction Create(Guid keyId, string recipientName, RecipientType recipientType,
        string purpose, DateTime expectedReturn, string? recipientPhone = null, Guid? recipientResidentId = null)
    {
        if (string.IsNullOrWhiteSpace(recipientName))
            throw new ArgumentException("Recipient name is required", nameof(recipientName));

        return new KeyTransaction
        {
            KeyId = keyId,
            RecipientName = recipientName,
            RecipientType = recipientType,
            Purpose = purpose,
            ExpectedReturn = expectedReturn,
            RecipientPhone = recipientPhone,
            RecipientResidentId = recipientResidentId,
            CheckedOutAt = DateTime.UtcNow
        };
    }

    internal void Return(string? condition = null, string? notes = null)
    {
        ReturnedAt = DateTime.UtcNow;
        ReturnCondition = condition;
        Notes = notes;
        MarkAsUpdated();
    }

    public bool IsReturned => ReturnedAt.HasValue;
    public bool IsOverdue => !IsReturned && DateTime.UtcNow > ExpectedReturn;
    public TimeSpan? OverdueDuration => IsOverdue ? DateTime.UtcNow - ExpectedReturn : null;
}
