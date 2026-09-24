using BuildingManager.Core.Enums;

namespace BuildingManager.Core.Entities;

public class Announcement : BaseEntity
{
    public Guid BuildingId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public AnnouncementType Type { get; private set; } = AnnouncementType.General;
    public DateTime? ScheduledFor { get; private set; }
    public DateTime? SentAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public bool IsPinned { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    private Announcement() { }

    public static Announcement Create(Guid buildingId, string title, string content, 
        AnnouncementType type, Guid createdBy, DateTime? scheduledFor = null, DateTime? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required", nameof(title));
        
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content is required", nameof(content));

        return new Announcement
        {
            BuildingId = buildingId,
            Title = title,
            Content = content,
            Type = type,
            CreatedBy = createdBy,
            ScheduledFor = scheduledFor,
            ExpiresAt = expiresAt
        };
    }

    public void MarkAsSent()
    {
        SentAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void Pin()
    {
        IsPinned = true;
        MarkAsUpdated();
    }

    public void Unpin()
    {
        IsPinned = false;
        MarkAsUpdated();
    }

    public void Update(string title, string content, AnnouncementType type)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required", nameof(title));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content is required", nameof(content));

        Title = title;
        Content = content;
        Type = type;
        MarkAsUpdated();
    }

    public void SetExpiration(DateTime expiresAt)
    {
        ExpiresAt = expiresAt;
        MarkAsUpdated();
    }

    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    public bool IsScheduled => ScheduledFor.HasValue && ScheduledFor.Value > DateTime.UtcNow;
    public bool ShouldBeSent => !SentAt.HasValue && (!ScheduledFor.HasValue || ScheduledFor.Value <= DateTime.UtcNow);
}
