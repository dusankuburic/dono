using System.Net.Http;
using System.Net.Http.Json;
using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingManager.Blazor.Services;

public class BuildingService : IBuildingService
{
    private readonly HttpClient _httpClient;

    public BuildingService([FromKeyedServices("ApiHttpClient")] HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<BuildingSummary>> GetBuildingsAsync()
    {
        var response = await _httpClient.GetAsync("/api/buildings");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<BuildingSummary>>()
            ?? throw new InvalidOperationException("The API returned an empty building list response.");
    }

    public async Task<BuildingSummary?> GetBuildingAsync(string buildingId)
    {
        var response = await _httpClient.GetAsync($"/api/buildings/{buildingId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BuildingSummary>();
    }

    public async Task InitializeBuildingAsync(string buildingId, BuildingInfoRequest info)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/buildings/{buildingId}/initialize", info);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IEnumerable<UnitInfo>> GetUnitsAsync(string buildingId)
    {
        var response = await _httpClient.GetAsync($"/api/buildings/{buildingId}/units");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IEnumerable<UnitInfo>>() ?? Enumerable.Empty<UnitInfo>();
    }

    public async Task AddUnitAsync(string buildingId, UnitInfoRequest unit)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/buildings/{buildingId}/units", unit);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveUnitAsync(string buildingId, string unitNumber)
    {
        var response = await _httpClient.DeleteAsync($"/api/buildings/{buildingId}/units/{Uri.EscapeDataString(unitNumber)}");
        await ApiErrors.EnsureSuccessAsync(response, "Could not remove the unit.");
    }

    public async Task<IEnumerable<ResidentInfo>> GetResidentsAsync(string buildingId)
    {
        var response = await _httpClient.GetAsync($"/api/buildings/{buildingId}/residents");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IEnumerable<ResidentInfo>>() ?? Enumerable.Empty<ResidentInfo>();
    }

    public async Task AddResidentAsync(string buildingId, ResidentInfoRequest resident)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/buildings/{buildingId}/residents", resident);
        await ApiErrors.EnsureSuccessAsync(response, "Could not add the resident.");
    }

    public async Task UpdateResidentAsync(string buildingId, Guid residentId, ResidentInfoRequest resident)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/buildings/{buildingId}/residents/{residentId}", resident);
        await ApiErrors.EnsureSuccessAsync(response, "Could not save the resident.");
    }

    public async Task RemoveResidentAsync(string buildingId, Guid residentId)
    {
        var response = await _httpClient.DeleteAsync($"/api/buildings/{buildingId}/residents/{residentId}");
        await ApiErrors.EnsureSuccessAsync(response, "Could not remove the resident.");
    }

    public async Task<IEnumerable<KeyInfo>> GetKeysAsync(string buildingId)
    {
        var response = await _httpClient.GetAsync($"/api/buildings/{buildingId}/keys");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IEnumerable<KeyInfo>>() ?? Enumerable.Empty<KeyInfo>();
    }

    public async Task<List<KeyInfo>> GetOverdueKeysAsync(string buildingId)
    {
        var response = await _httpClient.GetAsync($"/api/buildings/{buildingId}/keys/overdue");
        await ApiErrors.EnsureSuccessAsync(response, "Could not load the overdue keys.");
        return await response.Content.ReadFromJsonAsync<List<KeyInfo>>() ?? new List<KeyInfo>();
    }

    public async Task<KeyInfo> AddKeyAsync(string buildingId, KeyType type, string? identifier, string? description)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/buildings/{buildingId}/keys", new
        {
            KeyType = type,
            Identifier = identifier,
            Description = description
        });
        await ApiErrors.EnsureSuccessAsync(response, "Could not add the key.");
        return new KeyInfo { Id = Guid.Empty, Type = type, Identifier = identifier ?? "", Description = description };
    }

    public async Task<KeyInfo> CheckoutKeyAsync(string buildingId, Guid keyId, string recipientName,
        RecipientType recipientType, string purpose, DateTime expectedReturnUtc)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/buildings/{buildingId}/keys/{keyId}/checkout", new
        {
            RecipientName = recipientName,
            RecipientType = recipientType,
            Purpose = purpose,
            ExpectedReturn = expectedReturnUtc
        });
        await ApiErrors.EnsureSuccessAsync(response, "Could not check out the key.");
        return await response.Content.ReadFromJsonAsync<KeyInfo>()
            ?? throw new InvalidOperationException("Could not check out the key.");
    }

    public async Task<KeyInfo> CheckinKeyAsync(string buildingId, Guid keyId, string condition, string? notes)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/buildings/{buildingId}/keys/{keyId}/checkin", new
        {
            Condition = condition,
            Notes = notes
        });
        await ApiErrors.EnsureSuccessAsync(response, "Could not check in the key.");
        return await response.Content.ReadFromJsonAsync<KeyInfo>()
            ?? throw new InvalidOperationException("Could not check in the key.");
    }

    public async Task<WorkOrderInfo> UpdateWorkOrderStatusAsync(string buildingId, Guid workOrderId,
        WorkOrderStatus status, string? notes)
    {
        var response = await _httpClient.PatchAsJsonAsync($"/api/buildings/{buildingId}/workorders/{workOrderId}/status",
            new { Status = status, Notes = notes });
        await ApiErrors.EnsureSuccessAsync(response, "Could not update the work order.");
        return await response.Content.ReadFromJsonAsync<WorkOrderInfo>()
            ?? throw new InvalidOperationException("Could not update the work order.");
    }

    public async Task<IEnumerable<WorkOrderInfo>> GetWorkOrdersAsync(string buildingId, WorkOrderStatus? status = null)
    {
        var url = $"/api/buildings/{buildingId}/workorders";
        if (status.HasValue)
        {
            url += $"?status={status.Value}";
        }
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IEnumerable<WorkOrderInfo>>() ?? Enumerable.Empty<WorkOrderInfo>();
    }

    public async Task<WorkOrderInfo> CreateWorkOrderAsync(string buildingId, CreateWorkOrderRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/buildings/{buildingId}/workorders", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkOrderInfo>()
            ?? throw new InvalidOperationException("The API returned an empty work order response.");
    }

    public async Task<FinancialSummary> GetFinancialSummaryAsync(string buildingId)
    {
        var response = await _httpClient.GetAsync($"/api/buildings/{buildingId}/finance/summary");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FinancialSummary>()
            ?? throw new InvalidOperationException("The API returned an empty financial summary response.");
    }

    public async Task SetFeeStructureAsync(string buildingId, string name, decimal amountPerM2, decimal? fixedAmount)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/buildings/{buildingId}/finance/fee-structure",
            new { Name = name, AmountPerM2 = amountPerM2, FixedAmount = fixedAmount });
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<BuildingMember>> GetMembersAsync(string buildingId)
    {
        var response = await _httpClient.GetAsync($"/api/buildings/{buildingId}/access");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<BuildingMember>>() ?? new List<BuildingMember>();
    }

    public async Task GrantAccessAsync(string buildingId, string email)
    {
        var response = await _httpClient.PutAsync(
            $"/api/buildings/{buildingId}/access/{Uri.EscapeDataString(email)}", content: null);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException($"There is no account registered with {email}.");
        }
        response.EnsureSuccessStatusCode();
    }

    public async Task RevokeAccessAsync(string buildingId, string email)
    {
        var response = await _httpClient.DeleteAsync(
            $"/api/buildings/{buildingId}/access/{Uri.EscapeDataString(email)}");
        response.EnsureSuccessStatusCode();
    }
}
