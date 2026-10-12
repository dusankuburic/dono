using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;
using Orleans;
using Orleans.Runtime;

namespace BuildingManager.Orleans.Grains;

public class NotificationGrain : Grain, INotificationGrain
{
    public const int KeepLimit = 100;

    private readonly IPersistentState<NotificationState> _state;

    public NotificationGrain(
        [PersistentState("notifications", "buildingStorage")] IPersistentState<NotificationState> state)
    {
        _state = state;
    }

    public async Task AddAsync(NotificationInfo notification)
    {
        if (string.IsNullOrWhiteSpace(notification.Title))
            throw new ArgumentException("A notification needs a title.", nameof(notification));
        if (!Enum.IsDefined(notification.Type))
            throw new ArgumentException("Unknown notification type.", nameof(notification));

        _state.State.Items.Add(new NotificationItemState
        {
            Id = notification.Id == Guid.Empty ? Guid.NewGuid() : notification.Id,
            Type = notification.Type,
            BuildingId = notification.BuildingId,
            Title = notification.Title.Trim(),
            Body = string.IsNullOrWhiteSpace(notification.Body) ? null : notification.Body.Trim(),
            Link = notification.Link,
            CreatedAt = DateTime.UtcNow
        });
        Prune();
        await _state.WriteStateAsync();
    }

    public Task<List<NotificationInfo>> GetRecentAsync(int limit = 30)
    {
        limit = Math.Clamp(limit, 1, KeepLimit);
        var items = _state.State.Items
            .OrderByDescending(i => i.CreatedAt)
            .Take(limit)
            .Select(Map)
            .ToList();
        return Task.FromResult(items);
    }

    public Task<int> GetUnreadCountAsync() =>
        Task.FromResult(_state.State.Items.Count(i => !i.IsRead));

    public async Task MarkReadAsync(Guid notificationId)
    {
        var item = _state.State.Items.FirstOrDefault(i => i.Id == notificationId);
        if (item is null || item.IsRead)
            return;

        item.IsRead = true;
        await _state.WriteStateAsync();
    }

    public async Task MarkAllReadAsync()
    {
        if (_state.State.Items.All(i => i.IsRead))
            return;

        foreach (var item in _state.State.Items)
            item.IsRead = true;
        await _state.WriteStateAsync();
    }

        private void Prune()
    {
        while (_state.State.Items.Count > KeepLimit)
        {
            var oldest = _state.State.Items
                .OrderBy(i => i.IsRead ? 0 : 1)
                .ThenBy(i => i.CreatedAt)
                .First();
            _state.State.Items.Remove(oldest);
        }
    }

    private static NotificationInfo Map(NotificationItemState i) => new()
    {
        Id = i.Id,
        Type = i.Type,
        BuildingId = i.BuildingId,
        Title = i.Title,
        Body = i.Body,
        Link = i.Link,
        CreatedAt = i.CreatedAt,
        IsRead = i.IsRead
    };
}

[GenerateSerializer]
public class NotificationState
{
    [Id(0)] public List<NotificationItemState> Items { get; set; } = new();
}

[GenerateSerializer]
public class NotificationItemState
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public NotificationType Type { get; set; }
    [Id(2)] public string BuildingId { get; set; } = string.Empty;
    [Id(3)] public string Title { get; set; } = string.Empty;
    [Id(4)] public string? Body { get; set; }
    [Id(5)] public string? Link { get; set; }
    [Id(6)] public DateTime CreatedAt { get; set; }
    [Id(7)] public bool IsRead { get; set; }
}
