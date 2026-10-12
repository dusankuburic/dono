using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Grains.State;
using BuildingManager.Orleans.Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace BuildingManager.Orleans.Grains;

public class BuildingGrain : Grain, IBuildingGrain
{
    private readonly IPersistentState<BuildingAggregateState> _state;
    private readonly ILogger<BuildingGrain> _logger;

    public BuildingGrain(
        [PersistentState("building", "buildingStorage")]
        IPersistentState<BuildingAggregateState> state,
        ILogger<BuildingGrain> logger)
    {
        _state = state;
        _logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await MigrateLegacyFinanceAsync();
        await base.OnActivateAsync(cancellationToken);
    }

        private async Task MigrateLegacyFinanceAsync()
    {
        if (_state.State.Payments.Count == 0 && _state.State.Expenses.Count == 0)
        {
            return;
        }

        try
        {
            foreach (var payment in _state.State.Payments.Values)
            {
                if (payment.Amount <= 0)
                {
                    _logger.LogWarning("Skipping legacy payment {PaymentId} with non-positive amount {Amount}",
                        payment.Id, payment.Amount);
                    continue;
                }

                await RecordPaymentAsync(new PaymentInfo
                {
                    Id = payment.Id,
                    UnitNumber = payment.UnitNumber,
                    Amount = payment.Amount,
                    PaymentDate = payment.PaymentDate,
                    Month = payment.Month,
                    Year = payment.Year,
                    Reference = payment.Reference
                });
            }

            foreach (var expense in _state.State.Expenses.Values)
            {
                if (expense.Amount <= 0)
                {
                    _logger.LogWarning("Skipping legacy expense {ExpenseId} with non-positive amount {Amount}",
                        expense.Id, expense.Amount);
                    continue;
                }

                await RecordExpenseAsync(new ExpenseInfo
                {
                    Id = expense.Id,
                    Amount = expense.Amount,
                    ExpenseDate = expense.ExpenseDate,
                    Category = expense.Category,
                    Description = expense.Description,
                    Vendor = expense.Vendor
                });
            }

            _logger.LogInformation("Migrated {Payments} payments and {Expenses} expenses of building {BuildingId} to finance grains",
                _state.State.Payments.Count, _state.State.Expenses.Count, this.GetPrimaryKeyString());

            _state.State.Payments.Clear();
            _state.State.Expenses.Clear();
            await _state.WriteStateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migrating legacy finance data of building {BuildingId} failed; will retry on next activation",
                this.GetPrimaryKeyString());
        }
    }

    public Task<BuildingSummary> GetSummaryAsync()
    {
        if (string.IsNullOrEmpty(_state.State.Name))
            throw new KeyNotFoundException($"Building '{this.GetPrimaryKeyString()}' not found");

        return Task.FromResult(new BuildingSummary
        {
            Id = this.GetPrimaryKeyString(),
            Name = _state.State.Name,
            Street = _state.State.Street,
            StreetNumber = _state.State.StreetNumber,
            City = _state.State.City,
            UnitCount = _state.State.Units.Count,
            ResidentCount = _state.State.Residents.Values.Count(r => r.IsActive),
            KeysCheckedOut = _state.State.Keys.Values.Count(k => k.Status == KeyStatus.CheckedOut),
            OpenWorkOrders = _state.State.WorkOrders.Values.Count(w =>
                w.Status != WorkOrderStatus.Completed && w.Status != WorkOrderStatus.Cancelled)
        });
    }

    public async Task InitializeAsync(BuildingInfo info)
    {
        if (!string.IsNullOrEmpty(_state.State.Name))
            throw new InvalidOperationException(
                $"Building '{this.GetPrimaryKeyString()}' is already initialized; use UpdateInfoAsync to change its details.");

        if (string.IsNullOrWhiteSpace(info.Name))
            throw new ArgumentException("Building name is required", nameof(info));

        _state.State.Id = this.GetPrimaryKeyString();
        _state.State.Name = info.Name;
        _state.State.Street = info.Street;
        _state.State.StreetNumber = info.StreetNumber;
        _state.State.City = info.City;
        _state.State.PostalCode = info.PostalCode;
        _state.State.YearBuilt = info.YearBuilt;
        _state.State.TotalFloors = info.TotalFloors;
        _state.State.HasElevator = info.HasElevator;
        _state.State.HasParking = info.HasParking;
        _state.State.TotalAreaM2 = info.TotalAreaM2;
        _state.State.CreatedAt = DateTime.UtcNow;
        _state.State.UpdatedAt = DateTime.UtcNow;

        await _state.WriteStateAsync();

        var registry = GrainFactory.GetGrain<IBuildingRegistryGrain>("global");
        await registry.RegisterAsync(_state.State.Id, info.Name);
    }

    public async Task UpdateInfoAsync(BuildingInfo info)
    {
        _state.State.Name = info.Name;
        _state.State.Street = info.Street;
        _state.State.StreetNumber = info.StreetNumber;
        _state.State.City = info.City;
        _state.State.PostalCode = info.PostalCode;
        _state.State.YearBuilt = info.YearBuilt;
        _state.State.HasElevator = info.HasElevator;
        _state.State.HasParking = info.HasParking;
        _state.State.UpdatedAt = DateTime.UtcNow;

        await _state.WriteStateAsync();
    }

    public Task<UnitInfo> GetUnitAsync(string number)
    {
        if (_state.State.Units.TryGetValue(number, out var unit))
        {
            return Task.FromResult(new UnitInfo
            {
                Id = unit.Id,
                Number = unit.Number,
                Floor = unit.Floor,
                Entrance = unit.Entrance,
                AreaM2 = unit.AreaM2,
                OwnershipShare = unit.OwnershipShare,
                Type = unit.Type,
                Status = unit.Status
            });
        }
        throw new KeyNotFoundException($"Unit {number} not found");
    }

    public Task<IEnumerable<UnitInfo>> GetAllUnitsAsync()
    {

        var units = _state.State.Units.Values.Select(u => new UnitInfo
        {
            Id = u.Id,
            Number = u.Number,
            Floor = u.Floor,
            Entrance = u.Entrance,
            AreaM2 = u.AreaM2,
            OwnershipShare = u.OwnershipShare,
            Type = u.Type,
            Status = u.Status
        }).ToArray();
        return Task.FromResult<IEnumerable<UnitInfo>>(units);
    }

    public async Task AddOrUpdateUnitAsync(UnitInfo unit)
    {
        var state = new UnitState
        {
            Id = unit.Id,
            Number = unit.Number,
            Floor = unit.Floor,
            Entrance = unit.Entrance,
            AreaM2 = unit.AreaM2,
            OwnershipShare = unit.OwnershipShare,
            Type = unit.Type,
            Status = unit.Status
        };

        _state.State.Units[unit.Number] = state;
        _state.State.UpdatedAt = DateTime.UtcNow;
        await _state.WriteStateAsync();
    }

    public async Task RemoveUnitAsync(string number)
    {
        _state.State.Units.Remove(number);
        _state.State.UpdatedAt = DateTime.UtcNow;
        await _state.WriteStateAsync();
    }

    public Task<IEnumerable<ResidentInfo>> GetResidentsAsync()
    {
        var residents = _state.State.Residents.Values
            .Where(r => r.IsActive)
            .Select(r => new ResidentInfo
            {
                Id = r.Id,
                FirstName = r.FirstName,
                LastName = r.LastName,
                Phone = r.Phone,
                Email = r.Email,
                Role = r.Role,
                UnitNumber = r.UnitNumber,
                OwnershipShare = r.OwnershipShare
            }).ToArray();
        return Task.FromResult<IEnumerable<ResidentInfo>>(residents);
    }

    public async Task AddResidentAsync(ResidentInfo resident)
    {
        var state = new ResidentState
        {
            Id = resident.Id,
            FirstName = resident.FirstName,
            LastName = resident.LastName,
            Phone = resident.Phone,
            Email = resident.Email,
            Role = resident.Role,
            UnitNumber = resident.UnitNumber,
            OwnershipShare = resident.OwnershipShare,
            IsActive = true
        };

        _state.State.Residents[resident.Id] = state;
        _state.State.UpdatedAt = DateTime.UtcNow;
        await _state.WriteStateAsync();
    }

    public async Task RemoveResidentAsync(Guid residentId)
    {
        if (_state.State.Residents.TryGetValue(residentId, out var resident))
        {
            resident.IsActive = false;
            _state.State.UpdatedAt = DateTime.UtcNow;
            await _state.WriteStateAsync();
        }
    }

    public Task<IEnumerable<KeyInfo>> GetAllKeysAsync()
    {
        var keys = _state.State.Keys.Values.Select(MapKeyToInfo).ToArray();
        return Task.FromResult<IEnumerable<KeyInfo>>(keys);
    }

    public async Task<KeyInfo> CheckoutKeyAsync(Guid keyId, KeyCheckoutRequest request)
    {
        if (!_state.State.Keys.TryGetValue(keyId, out var key))
            throw new KeyNotFoundException($"Key {keyId} not found");

        if (key.Status != KeyStatus.Available)
            throw new InvalidOperationException($"Key {keyId} is not available");

        if (string.IsNullOrWhiteSpace(request.RecipientName))
            throw new ArgumentException("Recipient name is required", nameof(request));

        if (request.ExpectedReturn <= DateTime.UtcNow)
            throw new ArgumentException("Expected return date must be in the future", nameof(request));

        key.Status = KeyStatus.CheckedOut;
        key.CurrentCheckout = new KeyCheckoutState
        {
            TransactionId = Guid.NewGuid(),
            RecipientName = request.RecipientName,
            RecipientPhone = request.RecipientPhone,
            RecipientResidentId = request.RecipientResidentId,
            RecipientType = request.RecipientType,
            Purpose = request.Purpose,
            CheckedOutAt = DateTime.UtcNow,
            ExpectedReturn = request.ExpectedReturn
        };

        _state.State.UpdatedAt = DateTime.UtcNow;
        await _state.WriteStateAsync();

        return MapKeyToInfo(key);
    }

    public async Task<KeyInfo> CheckinKeyAsync(Guid keyId, string? condition, string? notes)
    {
        if (!_state.State.Keys.TryGetValue(keyId, out var key))
            throw new KeyNotFoundException($"Key {keyId} not found");

        if (key.Status != KeyStatus.CheckedOut)
            throw new InvalidOperationException($"Key {keyId} is not checked out");

        if (key.CurrentCheckout is { } checkout)
        {
            checkout.ReturnedAt = DateTime.UtcNow;
            checkout.ReturnCondition = condition;
            checkout.ReturnNotes = notes;
            key.CheckoutHistory.Add(checkout);
            key.CurrentCheckout = null;
        }

        key.Status = KeyStatus.Available;

        _state.State.UpdatedAt = DateTime.UtcNow;
        await _state.WriteStateAsync();

        return MapKeyToInfo(key);
    }

    public Task<IEnumerable<KeyInfo>> GetOverdueKeysAsync()
    {
        var now = DateTime.UtcNow;
        var overdue = _state.State.Keys.Values
            .Where(k => k.Status == KeyStatus.CheckedOut &&
                       k.CurrentCheckout != null &&
                       k.CurrentCheckout.ExpectedReturn < now)
            .Select(MapKeyToInfo)
            .ToArray();
        return Task.FromResult<IEnumerable<KeyInfo>>(overdue);
    }

    public async Task AddKeyAsync(KeyInfo key)
    {
        var state = new KeyState
        {
            Id = key.Id,
            Type = key.Type,
            UnitNumber = key.UnitNumber,
            Identifier = key.Identifier,
            Status = key.Status,
            Description = key.Description
        };

        _state.State.Keys[key.Id] = state;
        _state.State.UpdatedAt = DateTime.UtcNow;
        await _state.WriteStateAsync();
    }

    public Task<IEnumerable<WorkOrderInfo>> GetWorkOrdersAsync(WorkOrderStatus? status = null)
    {
        var workOrders = _state.State.WorkOrders.Values
            .Where(w => status == null || w.Status == status)
            .Select(w => new WorkOrderInfo
            {
                Id = w.Id,
                ReferenceNumber = w.ReferenceNumber,
                Title = w.Title,
                Description = w.Description,
                Category = w.Category,
                Priority = w.Priority,
                Status = w.Status,
                UnitNumber = w.UnitNumber,
                ReportedByResidentId = w.ReportedByResidentId,
                IsEmergency = w.IsEmergency,
                CreatedAt = w.CreatedAt
            }).ToArray();
        return Task.FromResult<IEnumerable<WorkOrderInfo>>(workOrders);
    }

    public async Task<WorkOrderInfo> CreateWorkOrderAsync(CreateWorkOrderRequest request)
    {
        var reference = $"WO-{DateTime.UtcNow:yyyyMMdd}-{this.GetPrimaryKeyString()[..8]}-{Guid.NewGuid().ToString()[..4]}";

        var workOrder = new WorkOrderState
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = reference,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Priority = request.Priority,
            Status = WorkOrderStatus.New,
            UnitNumber = request.UnitNumber,
            ReportedByResidentId = request.ReportedByResidentId,
            IsEmergency = request.IsEmergency,
            EmergencyDeadline = request.IsEmergency ? DateTime.UtcNow.AddHours(48) : null,
            CreatedAt = DateTime.UtcNow
        };

        _state.State.WorkOrders[workOrder.Id] = workOrder;
        _state.State.UpdatedAt = DateTime.UtcNow;
        await _state.WriteStateAsync();

        return MapWorkOrderToInfo(workOrder);
    }

    public async Task<WorkOrderInfo> UpdateWorkOrderStatusAsync(Guid workOrderId, WorkOrderStatus status, string? notes)
    {
        if (!_state.State.WorkOrders.TryGetValue(workOrderId, out var workOrder))
            throw new KeyNotFoundException($"Work order {workOrderId} not found");

        if (workOrder.Status is WorkOrderStatus.Completed or WorkOrderStatus.Cancelled)
            throw new InvalidOperationException(
                $"Work order {workOrderId} is already {workOrder.Status} and can no longer be changed");

        workOrder.Status = status;
        if (status == WorkOrderStatus.Completed)
            workOrder.CompletedAt = DateTime.UtcNow;

        _state.State.UpdatedAt = DateTime.UtcNow;
        await _state.WriteStateAsync();

        return MapWorkOrderToInfo(workOrder);
    }

    public async Task CreateAnnouncementAsync(AnnouncementInfo announcement)
    {
        if (string.IsNullOrWhiteSpace(announcement.Title))
            throw new ArgumentException("An announcement needs a title.", nameof(announcement));
        if (string.IsNullOrWhiteSpace(announcement.Content))
            throw new ArgumentException("An announcement needs a message.", nameof(announcement));
        if (!Enum.IsDefined(announcement.Type))
            throw new ArgumentException("Unknown announcement type.", nameof(announcement));

        var now = DateTime.UtcNow;

        if (announcement.ScheduledFor is { } scheduledFor && scheduledFor < now.AddMinutes(-1))
            throw new ArgumentException("The scheduled time is in the past.", nameof(announcement));
        var publishedAt = announcement.ScheduledFor is { } at && at > now ? at : now;
        if (announcement.ExpiresAt is { } expiresAt && expiresAt <= publishedAt)
            throw new ArgumentException("The announcement must expire after it is published.", nameof(announcement));

        var staleKeys = _state.State.Announcements
            .Where(kv => kv.Value.CreatedAt < now.AddYears(-1) && !kv.Value.IsPinned)
            .Select(kv => kv.Key)
            .ToList();
        foreach (var key in staleKeys)
        {
            _state.State.Announcements.Remove(key);
        }

        var state = new AnnouncementState
        {
            Id = announcement.Id,
            Title = announcement.Title.Trim(),
            Content = announcement.Content.Trim(),
            Type = announcement.Type,
            CreatedAt = announcement.CreatedAt == default ? now : announcement.CreatedAt,
            ScheduledFor = announcement.ScheduledFor,
            CreatedBy = announcement.CreatedBy,
            CreatedByName = announcement.CreatedByName,
            IsPinned = announcement.IsPinned,
            ExpiresAt = announcement.ExpiresAt
        };

        _state.State.Announcements[state.Id] = state;
        _state.State.UpdatedAt = now;
        await _state.WriteStateAsync();
    }

    public Task<IEnumerable<AnnouncementInfo>> GetAnnouncementsAsync(int limit = 20)
    {
        var now = DateTime.UtcNow;
        var announcements = _state.State.Announcements.Values
            .Where(a => (a.ExpiresAt == null || a.ExpiresAt > now) &&
                       (a.ScheduledFor == null || a.ScheduledFor <= now))
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.ScheduledFor ?? a.CreatedAt)
            .Take(limit)
            .Select(MapAnnouncement)
            .ToArray();
        return Task.FromResult<IEnumerable<AnnouncementInfo>>(announcements);
    }

    public Task<IEnumerable<AnnouncementInfo>> GetAnnouncementHistoryAsync()
    {
        var announcements = _state.State.Announcements.Values
            .OrderByDescending(a => a.ScheduledFor ?? a.CreatedAt)
            .Select(MapAnnouncement)
            .ToArray();
        return Task.FromResult<IEnumerable<AnnouncementInfo>>(announcements);
    }

    public async Task SetAnnouncementPinnedAsync(Guid announcementId, bool pinned)
    {
        var announcement = FindAnnouncement(announcementId);
        if (announcement.IsPinned == pinned)
            return;

        announcement.IsPinned = pinned;
        _state.State.UpdatedAt = DateTime.UtcNow;
        await _state.WriteStateAsync();
    }

    public async Task ArchiveAnnouncementAsync(Guid announcementId)
    {
        var announcement = FindAnnouncement(announcementId);
        var now = DateTime.UtcNow;
        if (announcement.ExpiresAt is { } expiresAt && expiresAt <= now)
            return;

        announcement.ExpiresAt = now;
        announcement.IsPinned = false;
        _state.State.UpdatedAt = now;
        await _state.WriteStateAsync();
    }

    public async Task<List<AnnouncementInfo>> PublishDueAnnouncementsAsync()
    {
        var now = DateTime.UtcNow;
        var due = _state.State.Announcements.Values
            .Where(a => a.ScheduledFor is { } scheduledFor && scheduledFor <= now && a.SentAt is null)
            .OrderBy(a => a.ScheduledFor)
            .ToList();
        if (due.Count == 0)
            return new List<AnnouncementInfo>();

        foreach (var announcement in due)
            announcement.SentAt = now;
        _state.State.UpdatedAt = now;
        await _state.WriteStateAsync();

        return due
            .Where(a => a.ExpiresAt is not { } expiresAt || expiresAt > now)
            .Select(MapAnnouncement)
            .ToList();
    }

    private AnnouncementState FindAnnouncement(Guid announcementId) =>
        _state.State.Announcements.TryGetValue(announcementId, out var announcement)
            ? announcement
            : throw new KeyNotFoundException($"Announcement '{announcementId}' not found");

    public Task<IEnumerable<DocumentInfo>> GetDocumentsAsync()
    {
        var documents = _state.State.Documents.Values
            .OrderByDescending(d => d.UploadedAt)
            .Select(MapDocument)
            .ToArray();
        return Task.FromResult<IEnumerable<DocumentInfo>>(documents);
    }

    public Task<DocumentInfo> GetDocumentAsync(Guid documentId) =>
        Task.FromResult(MapDocument(FindDocument(documentId)));

    public async Task AddDocumentAsync(DocumentInfo document)
    {
        if (string.IsNullOrWhiteSpace(document.FileName))
            throw new ArgumentException("The document needs a file name.", nameof(document));
        if (document.FileName.Trim().Length > 200)
            throw new ArgumentException("The file name is too long (200 characters at most).", nameof(document));
        if (string.IsNullOrWhiteSpace(document.ContentType))
            throw new ArgumentException("The document needs a content type.", nameof(document));
        if (!Enum.IsDefined(document.Folder) || !Enum.IsDefined(document.AccessLevel))
            throw new ArgumentException("Unknown document folder or access level.", nameof(document));
        if (document.SizeBytes <= 0)
            throw new ArgumentException("The document is empty.", nameof(document));
        if (document.SizeBytes > BuildingDocumentLimits.MaxDocumentSizeBytes)
            throw new ArgumentException($"A document may be at most {BuildingDocumentLimits.MaxDocumentSizeBytes / (1024 * 1024)} MB.", nameof(document));

        var used = _state.State.Documents.Values.Sum(d => d.SizeBytes);
        if (used + document.SizeBytes > BuildingDocumentLimits.BuildingQuotaBytes)
            throw new InvalidOperationException(
                $"The building's document storage is full ({BuildingDocumentLimits.BuildingQuotaBytes / (1024L * 1024 * 1024)} GB); delete some documents first.");

        _state.State.Documents[document.Id] = new DocumentState
        {
            Id = document.Id,
            FileName = document.FileName.Trim(),
            ContentType = document.ContentType.Trim(),
            SizeBytes = document.SizeBytes,
            Folder = document.Folder,
            AccessLevel = document.AccessLevel,
            Description = string.IsNullOrWhiteSpace(document.Description) ? null : document.Description.Trim(),
            UploadedBy = document.UploadedBy,
            UploadedByName = document.UploadedByName,
            UploadedAt = document.UploadedAt == default ? DateTime.UtcNow : document.UploadedAt
        };
        _state.State.UpdatedAt = DateTime.UtcNow;
        await _state.WriteStateAsync();
    }

    public async Task<DocumentInfo> RemoveDocumentAsync(Guid documentId)
    {
        var document = FindDocument(documentId);
        _state.State.Documents.Remove(documentId);
        _state.State.UpdatedAt = DateTime.UtcNow;
        await _state.WriteStateAsync();
        return MapDocument(document);
    }

    private DocumentState FindDocument(Guid documentId) =>
        _state.State.Documents.TryGetValue(documentId, out var document)
            ? document
            : throw new KeyNotFoundException($"Document '{documentId}' not found");

    private static DocumentInfo MapDocument(DocumentState d) => new()
    {
        Id = d.Id,
        FileName = d.FileName,
        ContentType = d.ContentType,
        SizeBytes = d.SizeBytes,
        Folder = d.Folder,
        AccessLevel = d.AccessLevel,
        Description = d.Description,
        UploadedBy = d.UploadedBy,
        UploadedByName = d.UploadedByName,
        UploadedAt = d.UploadedAt
    };

    private static AnnouncementInfo MapAnnouncement(AnnouncementState a) => new()
    {
        Id = a.Id,
        Title = a.Title,
        Content = a.Content,
        Type = a.Type,
        CreatedAt = a.CreatedAt,
        ScheduledFor = a.ScheduledFor,
        SentAt = a.SentAt,
        CreatedBy = a.CreatedBy,
        CreatedByName = a.CreatedByName,
        IsPinned = a.IsPinned,
        ExpiresAt = a.ExpiresAt
    };

    public Task<FinancialSummary> GetFinancialSummaryAsync(DateTime? from = null, DateTime? to = null)
    {
        return SummarizeFinanceAsync(from, to);
    }

    private async Task<FinancialSummary> SummarizeFinanceAsync(DateTime? from, DateTime? to)
    {
        var now = DateTime.UtcNow;
        var startDate = from ?? now.AddMonths(-12);
        var endDate = to ?? now;

        decimal totalIncome = 0, totalExpenses = 0;
        var payingUnits = new HashSet<string>();
        MonthlyTotals? currentMonthTotals = null;
        var currentMonth = new DateTime(now.Year, now.Month, 1);

        foreach (var month in EnumerateMonths(startDate, endDate))
        {
            var totals = await GetFinanceGrain(month).GetMonthlyTotalsAsync();
            totalIncome += totals.TotalIncome;
            totalExpenses += totals.TotalExpenses;
            payingUnits.UnionWith(totals.PayingUnits);

            if (month == currentMonth)
            {
                currentMonthTotals = totals;
            }
        }

        currentMonthTotals ??= await GetFinanceGrain(currentMonth).GetMonthlyTotalsAsync();

        var totalUnits = _state.State.Units.Count;

        return new FinancialSummary
        {
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            Balance = totalIncome - totalExpenses,
            CollectionRate = totalUnits > 0 ? (decimal)payingUnits.Count / totalUnits * 100 : 0,
            OutstandingReceivables = CalculateOutstandingReceivables(currentMonthTotals)
        };
    }

        private decimal CalculateOutstandingReceivables(MonthlyTotals currentMonth)
    {
        var now = DateTime.UtcNow;
        var fee = FeeInForce(new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc));

        decimal outstanding = 0;
        foreach (var unit in _state.State.Units.Values)
        {
            currentMonth.PaidAmountsByUnit.TryGetValue(unit.Number, out var paid);
            outstanding += Math.Max(0, ExpectedFee(unit, fee) - paid);
        }

        return outstanding;
    }

        private FeeStructureState? FeeInForce(DateTime monthStart)
    {
        var monthEnd = monthStart.AddMonths(1);
        return _state.State.FeeStructures
            .Where(f => f.EffectiveFrom < monthEnd)
            .OrderByDescending(f => f.EffectiveFrom)
            .FirstOrDefault();
    }

    private static decimal ExpectedFee(UnitState unit, FeeStructureState? fee) =>
        fee is null ? 0m : (fee.FixedAmount ?? 0m) + fee.AmountPerM2 * unit.AreaM2;

    private static DateTime MonthStart(int year, int month)
    {
        if (month is < 1 or > 12 || year is < 2000 or > 2200)
            throw new ArgumentException($"'{year}-{month}' is not a valid month.", nameof(month));
        return new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    public async Task<MonthPaymentStatus> GetPaymentStatusAsync(int year, int month)
    {
        var period = MonthStart(year, month);
        var previous = period.AddMonths(-1);
        var paidThisMonth = (await GetFinanceGrain(period).GetMonthlyTotalsAsync()).PaidAmountsByUnit;
        var paidLastMonth = (await GetFinanceGrain(previous).GetMonthlyTotalsAsync()).PaidAmountsByUnit;
        var fee = FeeInForce(period);
        var previousFee = FeeInForce(previous);

        var units = _state.State.Units.Values
            .OrderBy(u => u.Number.Length).ThenBy(u => u.Number)
            .Select(unit =>
            {
                var expected = ExpectedFee(unit, fee);
                paidThisMonth.TryGetValue(unit.Number, out var paid);
                paidLastMonth.TryGetValue(unit.Number, out var paidBefore);

                var standing = expected <= 0 ? PaymentStanding.NoFee
                    : paid >= expected ? PaymentStanding.Paid
                    : paidBefore >= ExpectedFee(unit, previousFee) ? PaymentStanding.OneMonthBehind
                    : PaymentStanding.TwoOrMoreMonthsBehind;

                return new UnitPaymentStatus
                {
                    UnitNumber = unit.Number,
                    AreaM2 = unit.AreaM2,
                    Expected = expected,
                    Paid = paid,
                    Standing = standing
                };
            })
            .ToList();

        return new MonthPaymentStatus { Year = year, Month = month, FeeName = fee?.Name, Units = units };
    }

    public Task<MonthLedger> GetFinanceLedgerAsync(int year, int month) =>
        GetFinanceGrain(MonthStart(year, month)).GetLedgerAsync();

    public async Task<List<PaymentInfo>> GetUnitPaymentHistoryAsync(string unitNumber, int months = 12)
    {
        months = Math.Clamp(months, 1, 60);
        var now = DateTime.UtcNow;
        var month = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var payments = new List<PaymentInfo>();
        for (var i = 0; i < months; i++, month = month.AddMonths(-1))
        {
            var ledger = await GetFinanceGrain(month).GetLedgerAsync();
            payments.AddRange(ledger.Payments.Where(p => p.UnitNumber == unitNumber));
        }

        return payments.OrderByDescending(p => p.PaymentDate).ToList();
    }

    public async Task RecordPaymentAsync(PaymentInfo payment)
    {

        await GetFinanceGrain(payment.PaymentDate).RecordPaymentAsync(payment);

        var billingPeriod = FinanceGrain.BillingPeriodOf(payment);
        if (billingPeriod != new DateTime(payment.PaymentDate.Year, payment.PaymentDate.Month, 1))
        {
            await GetFinanceGrain(billingPeriod).RecordPeriodPaymentAsync(payment);
        }
    }

    public Task RecordExpenseAsync(ExpenseInfo expense)
    {
        return GetFinanceGrain(expense.ExpenseDate).RecordExpenseAsync(expense);
    }

    private IFinanceGrain GetFinanceGrain(DateTime date) =>
        GrainFactory.GetGrain<IFinanceGrain>($"{this.GetPrimaryKeyString()}|{date:yyyy-MM}");

        private static IEnumerable<DateTime> EnumerateMonths(DateTime start, DateTime end)
    {
        var current = new DateTime(start.Year, start.Month, 1);
        var last = new DateTime(end.Year, end.Month, 1);
        var guard = 0;
        while (current <= last && guard++ < 1200)
        {
            yield return current;
            current = current.AddMonths(1);
        }
    }

    public async Task SetFeeStructureAsync(FeeStructureInfo fee)
    {
        if (string.IsNullOrWhiteSpace(fee.Name))
            throw new ArgumentException("Fee name is required", nameof(fee));

        if (fee.AmountPerM2 < 0)
            throw new ArgumentException("Amount per m² cannot be negative", nameof(fee));

        if (fee.FixedAmount < 0)
            throw new ArgumentException("Fixed amount cannot be negative", nameof(fee));

        foreach (var existing in _state.State.FeeStructures)
        {
            existing.IsActive = false;
        }

        _state.State.FeeStructures.Add(new FeeStructureState
        {
            Name = fee.Name,
            AmountPerM2 = fee.AmountPerM2,
            FixedAmount = fee.FixedAmount,
            EffectiveFrom = fee.EffectiveFrom == default ? DateTime.UtcNow : fee.EffectiveFrom,
            IsActive = true
        });

        _state.State.UpdatedAt = DateTime.UtcNow;
        await _state.WriteStateAsync();
    }

    private static KeyInfo MapKeyToInfo(KeyState k) => new()
    {
        Id = k.Id,
        Type = k.Type,
        UnitNumber = k.UnitNumber,
        Identifier = k.Identifier,
        Status = k.Status,
        Description = k.Description,
        CurrentCheckout = k.CurrentCheckout != null ? new KeyCheckoutInfo
        {
            RecipientName = k.CurrentCheckout.RecipientName,
            RecipientPhone = k.CurrentCheckout.RecipientPhone,
            RecipientType = k.CurrentCheckout.RecipientType,
            Purpose = k.CurrentCheckout.Purpose,
            CheckedOutAt = k.CurrentCheckout.CheckedOutAt,
            ExpectedReturn = k.CurrentCheckout.ExpectedReturn
        } : null
    };

    private static WorkOrderInfo MapWorkOrderToInfo(WorkOrderState w) => new()
    {
        Id = w.Id,
        ReferenceNumber = w.ReferenceNumber,
        Title = w.Title,
        Description = w.Description,
        Category = w.Category,
        Priority = w.Priority,
        Status = w.Status,
        UnitNumber = w.UnitNumber,
        ReportedByResidentId = w.ReportedByResidentId,
        IsEmergency = w.IsEmergency,
        CreatedAt = w.CreatedAt
    };
}

[GenerateSerializer]
public class BuildingAggregateState
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string Name { get; set; } = string.Empty;
    [Id(2)] public string Street { get; set; } = string.Empty;
    [Id(3)] public string StreetNumber { get; set; } = string.Empty;
    [Id(4)] public string City { get; set; } = string.Empty;
    [Id(5)] public string? PostalCode { get; set; }
    [Id(6)] public int? YearBuilt { get; set; }
    [Id(7)] public int TotalFloors { get; set; }
    [Id(8)] public bool HasElevator { get; set; }
    [Id(9)] public bool HasParking { get; set; }
    [Id(10)] public decimal TotalAreaM2 { get; set; }
    [Id(11)] public DateTime CreatedAt { get; set; }
    [Id(12)] public DateTime UpdatedAt { get; set; }

    [Id(13)] public Dictionary<string, UnitState> Units { get; set; } = new();
    [Id(14)] public Dictionary<Guid, ResidentState> Residents { get; set; } = new();
    [Id(15)] public Dictionary<Guid, KeyState> Keys { get; set; } = new();
    [Id(16)] public Dictionary<Guid, WorkOrderState> WorkOrders { get; set; } = new();
    [Id(17)] public Dictionary<Guid, AnnouncementState> Announcements { get; set; } = new();

    [Id(18)] public Dictionary<Guid, PaymentState> Payments { get; set; } = new();
    [Id(19)] public Dictionary<Guid, ExpenseState> Expenses { get; set; } = new();
    [Id(20)] public List<FeeStructureState> FeeStructures { get; set; } = new();

    [Id(21)] public Dictionary<Guid, DocumentState> Documents { get; set; } = new();
}
