using System.ComponentModel.DataAnnotations;
using BuildingManager.Core.Enums;
using Orleans;

namespace BuildingManager.Orleans.Interfaces;

public interface INotificationGrain : IGrainWithStringKey
{
        Task AddAsync(NotificationInfo notification);

        Task<List<NotificationInfo>> GetRecentAsync(int limit = 30);

    Task<int> GetUnreadCountAsync();

        Task MarkReadAsync(Guid notificationId);

    Task MarkAllReadAsync();
}

[GenerateSerializer]
public record NotificationInfo
{
    [Id(0)] public Guid Id { get; init; }
    [Id(1)] public NotificationType Type { get; init; }
        [Id(2)] public string BuildingId { get; init; } = string.Empty;
    [Id(3)][MaxLength(200)]
    public string Title { get; init; } = string.Empty;
    [Id(4)] public string? Body { get; init; }
        [Id(5)] public string? Link { get; init; }
    [Id(6)] public DateTime CreatedAt { get; init; }
    [Id(7)] public bool IsRead { get; init; }
}
