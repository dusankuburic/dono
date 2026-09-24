namespace BuildingManager.Core.Entities;

public class WorkOrderNote : BaseEntity
{
    public Guid WorkOrderId { get; private set; }
    public Guid CreatedBy { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public bool IsInternal { get; private set; }

    private WorkOrderNote() { }

    internal static WorkOrderNote Create(Guid workOrderId, Guid createdBy, string content, bool isInternal = false)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Note content is required", nameof(content));

        return new WorkOrderNote
        {
            WorkOrderId = workOrderId,
            CreatedBy = createdBy,
            Content = content,
            IsInternal = isInternal
        };
    }
}
