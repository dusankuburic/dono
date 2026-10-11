using System.Net.Http;
using System.Net.Http.Json;
using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingManager.Blazor.Services;

/// <summary>Building announcements. Failures throw InvalidOperationException with the API's message.</summary>
public interface IAnnouncementService
{
    /// <summary>The residents' feed: published, not expired, pinned first.</summary>
    Task<List<AnnouncementInfo>> GetFeedAsync(string buildingId);

    /// <summary>Everything kept, including scheduled and expired (managers only).</summary>
    Task<List<AnnouncementInfo>> GetHistoryAsync(string buildingId);

    Task<AnnouncementInfo> CreateAsync(string buildingId, string title, string content, AnnouncementType type,
        DateTime? scheduledForUtc, DateTime? expiresAtUtc, bool isPinned);

    Task SetPinnedAsync(string buildingId, Guid announcementId, bool pinned);
    Task ArchiveAsync(string buildingId, Guid announcementId);
}

public class AnnouncementService : IAnnouncementService
{
    private readonly HttpClient _httpClient;

    public AnnouncementService([FromKeyedServices("ApiHttpClient")] HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private static string Base(string buildingId) => $"/api/buildings/{Uri.EscapeDataString(buildingId)}/announcements";

    public Task<List<AnnouncementInfo>> GetFeedAsync(string buildingId) =>
        GetListAsync(Base(buildingId), "Could not load the announcements.");

    public Task<List<AnnouncementInfo>> GetHistoryAsync(string buildingId) =>
        GetListAsync($"{Base(buildingId)}/history", "Could not load the announcement history.");

    public async Task<AnnouncementInfo> CreateAsync(string buildingId, string title, string content, AnnouncementType type,
        DateTime? scheduledForUtc, DateTime? expiresAtUtc, bool isPinned)
    {
        var response = await _httpClient.PostAsJsonAsync(Base(buildingId), new
        {
            Title = title,
            Content = content,
            Type = type,
            ScheduledFor = scheduledForUtc,
            ExpiresAt = expiresAtUtc,
            IsPinned = isPinned
        });
        await ApiErrors.EnsureSuccessAsync(response, "Could not publish the announcement.");
        return await response.Content.ReadFromJsonAsync<AnnouncementInfo>()
            ?? throw new InvalidOperationException("Could not publish the announcement.");
    }

    public async Task SetPinnedAsync(string buildingId, Guid announcementId, bool pinned)
    {
        var response = await _httpClient.PutAsJsonAsync($"{Base(buildingId)}/{announcementId}/pin", new { Pinned = pinned });
        await ApiErrors.EnsureSuccessAsync(response, "Could not change the pin.");
    }

    public async Task ArchiveAsync(string buildingId, Guid announcementId)
    {
        var response = await _httpClient.PostAsync($"{Base(buildingId)}/{announcementId}/archive", null);
        await ApiErrors.EnsureSuccessAsync(response, "Could not archive the announcement.");
    }

    private async Task<List<AnnouncementInfo>> GetListAsync(string url, string fallbackMessage)
    {
        var response = await _httpClient.GetAsync(url);
        await ApiErrors.EnsureSuccessAsync(response, fallbackMessage);
        return await response.Content.ReadFromJsonAsync<List<AnnouncementInfo>>() ?? new();
    }
}
