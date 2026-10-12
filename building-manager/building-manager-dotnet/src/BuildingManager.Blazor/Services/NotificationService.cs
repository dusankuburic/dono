using System.Net.Http;
using System.Net.Http.Json;
using BuildingManager.Orleans.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingManager.Blazor.Services;

/// <summary>The signed-in user's in-app notifications. Failures throw InvalidOperationException with the API's message.</summary>
public interface INotificationService
{
    Task<List<NotificationInfo>> GetRecentAsync(int limit = 30);
    Task<int> GetUnreadCountAsync();
    Task MarkReadAsync(Guid notificationId);
    Task MarkAllReadAsync();
}

public class NotificationService : INotificationService
{
    private readonly HttpClient _httpClient;

    public NotificationService([FromKeyedServices("ApiHttpClient")] HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<NotificationInfo>> GetRecentAsync(int limit = 30)
    {
        var response = await _httpClient.GetAsync($"/api/notifications?limit={limit}");
        await ApiErrors.EnsureSuccessAsync(response, "Could not load the notifications.");
        return await response.Content.ReadFromJsonAsync<List<NotificationInfo>>() ?? new();
    }

    public async Task<int> GetUnreadCountAsync()
    {
        var response = await _httpClient.GetAsync("/api/notifications/unread-count");
        await ApiErrors.EnsureSuccessAsync(response, "Could not load the notification count.");
        return await response.Content.ReadFromJsonAsync<int>();
    }

    public async Task MarkReadAsync(Guid notificationId)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/notifications/{notificationId}/read", new { });
        await ApiErrors.EnsureSuccessAsync(response, "Could not mark the notification as read.");
    }

    public async Task MarkAllReadAsync()
    {
        var response = await _httpClient.PutAsJsonAsync("/api/notifications/read-all", new { });
        await ApiErrors.EnsureSuccessAsync(response, "Could not mark the notifications as read.");
    }
}
