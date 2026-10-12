using BuildingManager.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace BuildingManager.Orleans.Interfaces;

public interface IBuildingGrain : IGrainWithStringKey
{
    Task<BuildingSummary> GetSummaryAsync();
    Task InitializeAsync(BuildingInfo info);
    Task UpdateInfoAsync(BuildingInfo info);
    
    Task<UnitInfo> GetUnitAsync(string number);
    Task<IEnumerable<UnitInfo>> GetAllUnitsAsync();
    Task AddOrUpdateUnitAsync(UnitInfo unit);
    Task RemoveUnitAsync(string number);
    
    Task<IEnumerable<ResidentInfo>> GetResidentsAsync();
    Task AddResidentAsync(ResidentInfo resident);
    Task RemoveResidentAsync(Guid residentId);
    
    Task<IEnumerable<KeyInfo>> GetAllKeysAsync();
    Task<KeyInfo> CheckoutKeyAsync(Guid keyId, KeyCheckoutRequest checkout);
    Task<KeyInfo> CheckinKeyAsync(Guid keyId, string? condition, string? notes);
    Task<IEnumerable<KeyInfo>> GetOverdueKeysAsync();
    Task AddKeyAsync(KeyInfo key);
    
    Task<IEnumerable<WorkOrderInfo>> GetWorkOrdersAsync(WorkOrderStatus? status = null);
    Task<WorkOrderInfo> CreateWorkOrderAsync(CreateWorkOrderRequest request);
    Task<WorkOrderInfo> UpdateWorkOrderStatusAsync(Guid workOrderId, WorkOrderStatus status, string? notes);
    
    /// <summary>
    /// Publishes an announcement now or at ScheduledFor (not in the past),
    /// optionally until ExpiresAt. Throws ArgumentException for invalid input.
    /// </summary>
    Task CreateAnnouncementAsync(AnnouncementInfo announcement);

    /// <summary>The residents' feed: published and not expired, pinned first.</summary>
    Task<IEnumerable<AnnouncementInfo>> GetAnnouncementsAsync(int limit = 20);

    /// <summary>Every announcement kept (the last 12 months, plus pinned ones), including scheduled and expired.</summary>
    Task<IEnumerable<AnnouncementInfo>> GetAnnouncementHistoryAsync();

    Task SetAnnouncementPinnedAsync(Guid announcementId, bool pinned);

    /// <summary>Takes an announcement out of the feed by expiring it now; it stays in the history.</summary>
    Task ArchiveAnnouncementAsync(Guid announcementId);

    /// <summary>All document records, newest first. The caller filters by access level.</summary>
    Task<IEnumerable<DocumentInfo>> GetDocumentsAsync();

    Task<DocumentInfo> GetDocumentAsync(Guid documentId);

    /// <summary>
    /// Stores a document's record. Throws ArgumentException for invalid input
    /// and InvalidOperationException when the building's quota is full.
    /// </summary>
    Task AddDocumentAsync(DocumentInfo document);

    /// <summary>Deletes the document's record and returns it; the caller removes the blob content.</summary>
    Task<DocumentInfo> RemoveDocumentAsync(Guid documentId);

    Task<FinancialSummary> GetFinancialSummaryAsync(DateTime? from = null, DateTime? to = null);
    Task RecordPaymentAsync(PaymentInfo payment);
    Task RecordExpenseAsync(ExpenseInfo expense);

    /// <summary>Per-unit fee status for a billing month, judged against the fee in force by the end of it.</summary>
    Task<MonthPaymentStatus> GetPaymentStatusAsync(int year, int month);

    /// <summary>Payments received and expenses incurred in a month, plus payments received elsewhere for it.</summary>
    Task<MonthLedger> GetFinanceLedgerAsync(int year, int month);

    /// <summary>A unit's payments received in the last <paramref name="months"/> months, newest first.</summary>
    Task<List<PaymentInfo>> GetUnitPaymentHistoryAsync(string unitNumber, int months = 12);

    /// <summary>
    /// Activates a new fee structure and deactivates the previously active one.
    /// </summary>
    Task SetFeeStructureAsync(FeeStructureInfo fee);
}

[GenerateSerializer]
public record BuildingSummary
{
    [Id(0)] public string Id { get; init; } = string.Empty;
    [Id(1)] public string Name { get; init; } = string.Empty;
    [Id(2)] public string Street { get; init; } = string.Empty;
    [Id(3)] public string StreetNumber { get; init; } = string.Empty;
    [Id(4)] public string City { get; init; } = string.Empty;
    [Id(5)] public int UnitCount { get; init; }
    [Id(6)] public int ResidentCount { get; init; }
    [Id(7)] public int KeysCheckedOut { get; init; }
    [Id(8)] public int OpenWorkOrders { get; init; }
}

[GenerateSerializer]
public record BuildingInfo
{
    [Id(0)] public string Name { get; init; } = string.Empty;
    [Id(1)] public string Street { get; init; } = string.Empty;
    [Id(2)] public string StreetNumber { get; init; } = string.Empty;
    [Id(3)] public string City { get; init; } = string.Empty;
    [Id(4)] public string? PostalCode { get; init; }
    [Id(5)] public int? YearBuilt { get; init; }
    [Id(6)] public int TotalFloors { get; init; }
    [Id(7)] public bool HasElevator { get; init; }
    [Id(8)] public bool HasParking { get; init; }
    [Id(9)] public decimal TotalAreaM2 { get; init; }
}

[GenerateSerializer]
public record UnitInfo
{
    [Id(0)] public Guid Id { get; init; }
    [Id(1)] public string Number { get; init; } = string.Empty;
    [Id(2)] public int Floor { get; init; }
    [Id(3)] public string? Entrance { get; init; }
    [Id(4)] public decimal AreaM2 { get; init; }
    [Id(5)] public decimal OwnershipShare { get; init; }
    [Id(6)] public UnitType Type { get; init; }
    [Id(7)] public UnitStatus Status { get; init; }
}

[GenerateSerializer]
public record ResidentInfo
{
    [Id(0)] public Guid Id { get; init; }
    [Id(1)] public string FirstName { get; init; } = string.Empty;
    [Id(2)] public string LastName { get; init; } = string.Empty;
    [Id(3)] public string? Phone { get; init; }
    [Id(4)] public string? Email { get; init; }
    [Id(5)] public ResidentRole Role { get; init; }
    [Id(6)] public string? UnitNumber { get; init; }
    [Id(7)] public decimal? OwnershipShare { get; init; }
}

[GenerateSerializer]
public record KeyInfo
{
    [Id(0)] public Guid Id { get; init; }
    [Id(1)] public KeyType Type { get; init; }
    [Id(2)] public string? UnitNumber { get; init; }
    [Id(3)] public string Identifier { get; init; } = string.Empty;
    [Id(4)] public KeyStatus Status { get; init; }
    [Id(5)] public string? Description { get; init; }
    [Id(6)] public KeyCheckoutInfo? CurrentCheckout { get; init; }
}

[GenerateSerializer]
public record KeyCheckoutInfo
{
    [Id(0)] public string RecipientName { get; init; } = string.Empty;
    [Id(1)] public string? RecipientPhone { get; init; }
    [Id(2)] public RecipientType RecipientType { get; init; }
    [Id(3)] public string Purpose { get; init; } = string.Empty;
    [Id(4)] public DateTime CheckedOutAt { get; init; }
    [Id(5)] public DateTime ExpectedReturn { get; init; }
}

[GenerateSerializer]
public record KeyCheckoutRequest
{
    [Id(0)][Required, MaxLength(200)]
    public string RecipientName { get; init; } = string.Empty;
    [Id(1)] public string? RecipientPhone { get; init; }
    [Id(2)] public Guid? RecipientResidentId { get; init; }
    [Id(3)] public RecipientType RecipientType { get; init; }
    [Id(4)][Required, MaxLength(500)]
    public string Purpose { get; init; } = string.Empty;
    [Id(5)] public DateTime ExpectedReturn { get; init; }
}

[GenerateSerializer]
public record WorkOrderInfo
{
    [Id(0)] public Guid Id { get; init; }
    [Id(1)] public string ReferenceNumber { get; init; } = string.Empty;
    [Id(2)] public string Title { get; init; } = string.Empty;
    [Id(3)] public string Description { get; init; } = string.Empty;
    [Id(4)] public WorkOrderCategory Category { get; init; }
    [Id(5)] public Priority Priority { get; init; }
    [Id(6)] public WorkOrderStatus Status { get; init; }
    [Id(7)] public string? UnitNumber { get; init; }
    [Id(8)] public Guid ReportedByResidentId { get; init; }
    [Id(9)] public bool IsEmergency { get; init; }
    [Id(10)] public DateTime CreatedAt { get; init; }
}

[GenerateSerializer]
public record CreateWorkOrderRequest
{
    [Id(0)][Required, MaxLength(200)]
    public string Title { get; init; } = string.Empty;
    [Id(1)][MaxLength(2000)]
    public string Description { get; init; } = string.Empty;
    [Id(2)] public WorkOrderCategory Category { get; init; }
    [Id(3)] public Priority Priority { get; init; }
    [Id(4)][MaxLength(20)]
    public string? UnitNumber { get; init; }
    [Id(5)] public Guid ReportedByResidentId { get; init; }
    [Id(6)] public bool IsEmergency { get; init; }
}

[GenerateSerializer]
public record AnnouncementInfo
{
    [Id(0)] public Guid Id { get; init; }
    [Id(1)] public string Title { get; init; } = string.Empty;
    [Id(2)] public string Content { get; init; } = string.Empty;
    [Id(3)] public AnnouncementType Type { get; init; }
    [Id(4)] public DateTime CreatedAt { get; init; }
    [Id(5)] public DateTime? ScheduledFor { get; init; }
    [Id(6)] public DateTime? SentAt { get; init; }
    [Id(7)] public Guid CreatedBy { get; init; }
    [Id(8)] public bool IsPinned { get; init; }
    [Id(9)] public DateTime? ExpiresAt { get; init; }
    [Id(10)] public string? CreatedByName { get; init; }
}

/// <summary>One document may be at most this big; a building may keep this much in total (NFR: 1 GB).</summary>
public static class BuildingDocumentLimits
{
    public const long MaxDocumentSizeBytes = 25L * 1024 * 1024;
    public const long BuildingQuotaBytes = 1024L * 1024 * 1024;
}

/// <summary>A building document's record; the file content lives in blob storage.</summary>
[GenerateSerializer]
public record DocumentInfo
{
    [Id(0)] public Guid Id { get; init; }
    [Id(1)][MaxLength(200)]
    public string FileName { get; init; } = string.Empty;
    [Id(2)] public string ContentType { get; init; } = string.Empty;
    [Id(3)] public long SizeBytes { get; init; }
    [Id(4)] public DocumentFolder Folder { get; init; }
    [Id(5)] public DocumentAccessLevel AccessLevel { get; init; }
    [Id(6)][MaxLength(500)]
    public string? Description { get; init; }
    [Id(7)] public Guid UploadedBy { get; init; }
    [Id(8)] public string? UploadedByName { get; init; }
    [Id(9)] public DateTime UploadedAt { get; init; }
}

[GenerateSerializer]
public record FinancialSummary
{
    [Id(0)] public decimal TotalIncome { get; init; }
    [Id(1)] public decimal TotalExpenses { get; init; }
    [Id(2)] public decimal Balance { get; init; }
    [Id(3)] public decimal CollectionRate { get; init; }
    [Id(4)] public decimal OutstandingReceivables { get; init; }
}

[GenerateSerializer]
public record FeeStructureInfo
{
    [Id(0)][Required, MaxLength(100)]
    public string Name { get; init; } = string.Empty;
    [Id(1)] public decimal AmountPerM2 { get; init; }
    [Id(2)] public decimal? FixedAmount { get; init; }
    [Id(3)] public DateTime EffectiveFrom { get; init; }
}

[GenerateSerializer]
public record PaymentInfo
{
    [Id(0)] public Guid Id { get; init; }
    [Id(1)] public string UnitNumber { get; init; } = string.Empty;
    [Id(2)] public decimal Amount { get; init; }
    [Id(3)] public DateTime PaymentDate { get; init; }
    [Id(4)] public int? Month { get; init; }
    [Id(5)] public int? Year { get; init; }
    [Id(6)] public string? Reference { get; init; }
    [Id(7)] public PaymentMethod? Method { get; init; }
    [Id(8)] public string? Notes { get; init; }
}

/// <summary>
/// How a unit stands with its monthly fee (UC-008 colours): paid this month
/// (green), only last month paid (yellow), or two or more months unpaid (red).
/// </summary>
public enum PaymentStanding
{
    /// <summary>No fee structure in force, so nothing is due.</summary>
    NoFee = 0,
    Paid = 1,
    OneMonthBehind = 2,
    TwoOrMoreMonthsBehind = 3
}

/// <summary>Fee status of every unit for one billing month.</summary>
[GenerateSerializer]
public record MonthPaymentStatus
{
    [Id(0)] public int Year { get; init; }
    [Id(1)] public int Month { get; init; }
    /// <summary>Name of the fee structure in force that month, if any.</summary>
    [Id(2)] public string? FeeName { get; init; }
    [Id(3)] public List<UnitPaymentStatus> Units { get; init; } = new();
}

[GenerateSerializer]
public record UnitPaymentStatus
{
    [Id(0)] public string UnitNumber { get; init; } = string.Empty;
    [Id(1)] public decimal AreaM2 { get; init; }
    /// <summary>Fee due for the month (fixed amount + amount per m² × area).</summary>
    [Id(2)] public decimal Expected { get; init; }
    /// <summary>Paid for this billing month, wherever it was received (partial and overpayments included).</summary>
    [Id(3)] public decimal Paid { get; init; }
    [Id(4)] public PaymentStanding Standing { get; init; }
}

[GenerateSerializer]
public record ExpenseInfo
{
    [Id(0)] public Guid Id { get; init; }
    [Id(1)] public decimal Amount { get; init; }
    [Id(2)] public DateTime ExpenseDate { get; init; }
    [Id(3)] public ExpenseCategory Category { get; init; }
    [Id(4)] public string Description { get; init; } = string.Empty;
    [Id(5)] public string? Vendor { get; init; }
}
