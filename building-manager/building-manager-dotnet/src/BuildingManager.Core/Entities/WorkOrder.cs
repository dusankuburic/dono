using BuildingManager.Core.Enums;

namespace BuildingManager.Core.Entities;

public class WorkOrder : BaseEntity
{
    public Guid BuildingId { get; private set; }
    public string ReferenceNumber { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public WorkOrderCategory Category { get; private set; }
    public Priority Priority { get; private set; } = Priority.Normal;
    public WorkOrderStatus Status { get; private set; } = WorkOrderStatus.New;
    public string? UnitNumber { get; private set; }
    public Guid ReportedByResidentId { get; private set; }
    public Guid? AssignedToId { get; private set; }
    public bool IsEmergency { get; private set; }
    public DateTime? EmergencyStartedAt { get; private set; }
    public DateTime? EmergencyDeadline { get; private set; }
    public decimal? EstimatedCost { get; private set; }
    public decimal? ActualCost { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? TargetCompletionDate { get; private set; }
    
    private readonly List<WorkOrderNote> _notes = new();
    public IReadOnlyCollection<WorkOrderNote> Notes => _notes.AsReadOnly();

    private WorkOrder() { }

    /// <summary>
    /// Completed and cancelled work orders can no longer be modified.
    /// </summary>
    public bool IsInTerminalStatus => Status is WorkOrderStatus.Completed or WorkOrderStatus.Cancelled;

    private void EnsureNotTerminal(string action)
    {
        if (IsInTerminalStatus)
            throw new InvalidOperationException($"Cannot {action} a work order in terminal status {Status}");
    }

    public static WorkOrder Create(Guid buildingId, string title, string description, 
        WorkOrderCategory category, Priority priority, Guid reportedByResidentId,
        string? unitNumber = null, bool isEmergency = false)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required", nameof(title));
        
        var referenceNumber = GenerateReferenceNumber(buildingId);
        
        var workOrder = new WorkOrder
        {
            BuildingId = buildingId,
            ReferenceNumber = referenceNumber,
            Title = title,
            Description = description ?? string.Empty,
            Category = category,
            Priority = priority,
            ReportedByResidentId = reportedByResidentId,
            UnitNumber = unitNumber,
            IsEmergency = isEmergency,
            Status = WorkOrderStatus.New
        };

        if (isEmergency)
        {
            workOrder.EmergencyStartedAt = DateTime.UtcNow;
            workOrder.EmergencyDeadline = DateTime.UtcNow.AddHours(48);
        }

        return workOrder;
    }

    private static string GenerateReferenceNumber(Guid buildingId)
    {
        var date = DateTime.UtcNow;
        return $"WO-{date:yyyyMMdd}-{buildingId.ToString()[..8].ToUpper()}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
    }

    public void Acknowledge(Guid acknowledgedBy)
    {
        if (Status != WorkOrderStatus.New)
            throw new InvalidOperationException("Can only acknowledge new work orders");
        
        Status = WorkOrderStatus.Acknowledged;
        AddNote(acknowledgedBy, "Work order acknowledged");
        MarkAsUpdated();
    }

    public void StartWork(Guid startedBy, Guid? assignedTo = null)
    {
        if (Status != WorkOrderStatus.New && Status != WorkOrderStatus.Acknowledged)
            throw new InvalidOperationException("Can only start acknowledged or new work orders");
        
        AssignedToId = assignedTo ?? startedBy;
        Status = WorkOrderStatus.InProgress;
        AddNote(startedBy, "Work started");
        MarkAsUpdated();
    }

    public void SetWaitingParts(Guid updatedBy, string? note = null)
    {
        EnsureNotTerminal("set waiting-for-parts on");
        Status = WorkOrderStatus.WaitingParts;
        AddNote(updatedBy, note ?? "Waiting for parts");
        MarkAsUpdated();
    }

    public void SetWaitingDecision(Guid updatedBy, string? note = null)
    {
        EnsureNotTerminal("set waiting-for-decision on");
        Status = WorkOrderStatus.WaitingDecision;
        AddNote(updatedBy, note ?? "Waiting for assembly decision");
        MarkAsUpdated();
    }

    public void Complete(Guid completedBy, decimal? actualCost = null, string? notes = null)
    {
        EnsureNotTerminal("complete");
        Status = WorkOrderStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        ActualCost = actualCost;
        AddNote(completedBy, notes ?? "Work completed");
        MarkAsUpdated();
    }

    public void Cancel(Guid cancelledBy, string reason)
    {
        EnsureNotTerminal("cancel");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Cancellation reason is required", nameof(reason));

        Status = WorkOrderStatus.Cancelled;
        AddNote(cancelledBy, $"Cancelled: {reason}");
        MarkAsUpdated();
    }

    public void UpdatePriority(Priority newPriority, Guid updatedBy)
    {
        EnsureNotTerminal("change the priority of");
        Priority = newPriority;
        AddNote(updatedBy, $"Priority changed to {newPriority}");
        MarkAsUpdated();
    }

    public void SetEstimatedCost(decimal cost, Guid updatedBy)
    {
        EnsureNotTerminal("set the estimated cost of");

        if (cost < 0)
            throw new ArgumentException("Cost cannot be negative", nameof(cost));

        EstimatedCost = cost;
        AddNote(updatedBy, $"Estimated cost set to {cost:C}");
        MarkAsUpdated();
    }

    public void SetTargetCompletionDate(DateTime date, Guid updatedBy)
    {
        EnsureNotTerminal("set the target completion date of");
        TargetCompletionDate = date;
        AddNote(updatedBy, $"Target completion date set to {date:d}");
        MarkAsUpdated();
    }

    public void AddNote(Guid createdBy, string content)
    {
        var note = WorkOrderNote.Create(Id, createdBy, content);
        _notes.Add(note);
        MarkAsUpdated();
    }

    public bool IsEmergencyOverdue => IsEmergency && EmergencyDeadline.HasValue && DateTime.UtcNow > EmergencyDeadline.Value;
    public TimeSpan? EmergencyTimeRemaining => IsEmergency && EmergencyDeadline.HasValue 
        ? EmergencyDeadline.Value - DateTime.UtcNow 
        : null;
}
