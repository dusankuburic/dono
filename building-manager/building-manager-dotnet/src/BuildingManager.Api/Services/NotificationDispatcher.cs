using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;
using Orleans;

namespace BuildingManager.Api.Services;

public interface INotificationDispatcher
{
        Task NotifyBuildingMembersAsync(string buildingId, NotificationType type,
        string title, string? body, string? link, string? actorEmail);

        Task NotifyEmailAsync(string email, NotificationType type,
        string title, string? body, string? link, string buildingId = "", string? actorEmail = null);
}

public class NotificationDispatcher : INotificationDispatcher
{
    private readonly IGrainFactory _grainFactory;

    public NotificationDispatcher(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public async Task NotifyBuildingMembersAsync(string buildingId, NotificationType type,
        string title, string? body, string? link, string? actorEmail)
    {
        var emails = await _grainFactory.GetGrain<IBuildingAccessGrain>(buildingId).GetMemberEmailsAsync();
        foreach (var email in emails)
        {
            await AddAsync(email, buildingId, type, title, body, link, actorEmail);
        }
    }

    public Task NotifyEmailAsync(string email, NotificationType type,
        string title, string? body, string? link, string buildingId = "", string? actorEmail = null) =>
        AddAsync(email, buildingId, type, title, body, link, actorEmail);

    private async Task AddAsync(string email, string buildingId, NotificationType type,
        string title, string? body, string? link, string? actorEmail)
    {
        var normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length == 0 || string.Equals(normalized, actorEmail?.Trim().ToLowerInvariant()))
            return;

        await _grainFactory.GetGrain<INotificationGrain>(normalized).AddAsync(new NotificationInfo
        {
            Id = Guid.NewGuid(),
            Type = type,
            BuildingId = buildingId,
            Title = title,
            Body = body,
            Link = link,
            CreatedAt = DateTime.UtcNow
        });
    }
}
