using System.ComponentModel.DataAnnotations;
using BuildingManager.Core.Enums;
using Orleans;

namespace BuildingManager.Orleans.Interfaces;

/// <summary>
/// One user's in-app notifications, keyed by normalized (lowercased) email —
/// the same key as IUserGrain and building membership, so any code that knows
/// a member's email can notify them.
/// </summary>
public interface INotificationGrain : IGrainWithStringKey
{
    /// <summary>Stores the notification as unread. Throws ArgumentException without a title.</summary>
    Task AddAsync(NotificationInfo notification);

    /// <summary>The newest notifications, most recent first.</summary>
    Task<List<NotificationInfo>> GetRecentAsync(int limit = 30);

    Task<int> GetUnreadCountAsync();

    /// <summary>Marking an unknown or already-read notification is a no-op.</summary>
    Task MarkReadAsync(Guid notificationId);

    Task MarkAllReadAsync();
}

[GenerateSerializer]
public record NotificationInfo
{
    [Id(0)] public Guid Id { get; init; }
    [Id(1)] public NotificationType Type { get; init; }
    /// <summary>The building the notification is about (for context, not enforcement).</summary>
    [Id(2)] public string BuildingId { get; init; } = string.Empty;
    [Id(3)][MaxLength(200)]
    public string Title { get; init; } = string.Empty;
    [Id(4)] public string? Body { get; init; }
    /// <summary>Page to open when the notification is tapped (e.g. "/announcements").</summary>
    [Id(5)] public string? Link { get; init; }
    [Id(6)] public DateTime CreatedAt { get; init; }
    [Id(7)] public bool IsRead { get; init; }
}
