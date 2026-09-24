using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Grains.State;
using BuildingManager.Orleans.Interfaces;
using Orleans;
using Orleans.Runtime;

namespace BuildingManager.Orleans.Grains;

public class BuildingGrain : Grain, IBuildingGrain
{
    private readonly IPersistentState<BuildingAggregateState> _state;

    public BuildingGrain(
        [PersistentState("building", "buildingStorage")]
        IPersistentState<BuildingAggregateState> state)
    {
        _state = state;
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

        // Make the building discoverable through the cluster-wide registry.
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

    public async Task<UnitInfo> GetUnitAsync(string number)
    {
        if (_state.State.Units.TryGetValue(number, out var unit))
        {
            return new UnitInfo
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
        }
        throw new KeyNotFoundException($"Unit {number} not found");
    }

    public Task<IEnumerable<UnitInfo>> GetAllUnitsAsync()
    {
        // Materialize with ToArray: Orleans must copy the result across the
        // wire and cannot serialize lazy LINQ iterators.
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
        var state = new AnnouncementState
        {
            Id = announcement.Id,
            Title = announcement.Title,
            Content = announcement.Content,
            Type = announcement.Type,
            CreatedAt = DateTime.UtcNow,
            ScheduledFor = announcement.ScheduledFor,
            CreatedBy = announcement.CreatedBy,
            IsPinned = false,
            ExpiresAt = announcement.ExpiresAt
        };
        
        _state.State.Announcements[state.Id] = state;
        _state.State.UpdatedAt = DateTime.UtcNow;
        await _state.WriteStateAsync();
    }

    public Task<IEnumerable<AnnouncementInfo>> GetAnnouncementsAsync(int limit = 20)
    {
        var now = DateTime.UtcNow;
        var announcements = _state.State.Announcements.Values
            .Where(a => (a.ExpiresAt == null || a.ExpiresAt > now) &&
                       (a.ScheduledFor == null || a.ScheduledFor <= now))
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new AnnouncementInfo
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content,
                Type = a.Type,
                CreatedAt = a.CreatedAt,
                ScheduledFor = a.ScheduledFor,
                SentAt = a.SentAt,
                CreatedBy = a.CreatedBy,
                IsPinned = a.IsPinned,
                ExpiresAt = a.ExpiresAt
            }).ToArray();
        return Task.FromResult<IEnumerable<AnnouncementInfo>>(announcements);
    }

    public Task<FinancialSummary> GetFinancialSummaryAsync(DateTime? from = null, DateTime? to = null)
    {
        var startDate = from ?? DateTime.UtcNow.AddMonths(-12);
        var endDate = to ?? DateTime.UtcNow;

        var totalIncome = _state.State.Payments.Values
            .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
            .Sum(p => p.Amount);

        var totalExpenses = _state.State.Expenses.Values
            .Where(e => e.ExpenseDate >= startDate && e.ExpenseDate <= endDate)
            .Sum(e => e.Amount);

        var totalUnits = _state.State.Units.Count;
        var paidUnits = _state.State.Payments.Values
            .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
            .Select(p => p.UnitNumber)
            .Distinct()
            .Count();

        return Task.FromResult(new FinancialSummary
        {
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            Balance = totalIncome - totalExpenses,
            CollectionRate = totalUnits > 0 ? (decimal)paidUnits / totalUnits * 100 : 0,
            OutstandingReceivables = CalculateOutstandingReceivables()
        });
    }

    /// <summary>
    /// For the current month: sum over units of (expected fee - payments made),
    /// floored at zero per unit. Mirrors FeeStructure.CalculateFee from the domain
    /// (fixed amount + per-m2 amount).
    /// </summary>
    private decimal CalculateOutstandingReceivables()
    {
        var now = DateTime.UtcNow;
        var activeFee = _state.State.FeeStructures
            .Where(f => f.IsActive && f.EffectiveFrom <= now)
            .OrderByDescending(f => f.EffectiveFrom)
            .FirstOrDefault();

        if (activeFee is null)
        {
            return 0;
        }

        decimal outstanding = 0;
        foreach (var unit in _state.State.Units.Values)
        {
            var expected = (activeFee.FixedAmount ?? 0m) + activeFee.AmountPerM2 * unit.AreaM2;
            var paid = _state.State.Payments.Values
                .Where(p => p.Year == now.Year && p.Month == now.Month && p.UnitNumber == unit.Number)
                .Sum(p => p.Amount);
            outstanding += Math.Max(0, expected - paid);
        }

        return outstanding;
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

    public async Task RecordPaymentAsync(PaymentInfo payment)
    {
        var state = new PaymentState
        {
            Id = payment.Id,
            UnitNumber = payment.UnitNumber,
            Amount = payment.Amount,
            PaymentDate = payment.PaymentDate,
            Month = payment.Month,
            Year = payment.Year,
            Reference = payment.Reference
        };
        
        _state.State.Payments[payment.Id] = state;
        _state.State.UpdatedAt = DateTime.UtcNow;
        await _state.WriteStateAsync();
    }

    public async Task RecordExpenseAsync(ExpenseInfo expense)
    {
        var state = new ExpenseState
        {
            Id = expense.Id,
            Amount = expense.Amount,
            ExpenseDate = expense.ExpenseDate,
            Category = expense.Category,
            Description = expense.Description,
            Vendor = expense.Vendor,
            IsApproved = false
        };
        
        _state.State.Expenses[expense.Id] = state;
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
}
