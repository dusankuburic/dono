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

    public async Task<BuildingInfo> GetBuildingInfoAsync(string buildingId)
    {
        var response = await _httpClient.GetAsync($"/api/buildings/{buildingId}/info");
        await ApiErrors.EnsureSuccessAsync(response, "Could not load the building's details.");
        return await response.Content.ReadFromJsonAsync<BuildingInfo>()
            ?? throw new InvalidOperationException("The API returned empty building details.");
    }

    public async Task UpdateBuildingAsync(string buildingId, BuildingInfoRequest info)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/buildings/{buildingId}", info);
        await ApiErrors.EnsureSuccessAsync(response, "Could not save the building's details.");
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
        await ApiErrors.EnsureSuccessAsync(response, "Could not save the unit.");
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

    public async Task MoveOutResidentAsync(string buildingId, Guid residentId)
    {
        var response = await _httpClient.DeleteAsync($"/api/buildings/{buildingId}/residents/{residentId}");
        await ApiErrors.EnsureSuccessAsync(response, "Could not move the resident out.");
    }

    public async Task<IEnumerable<KeyInfo>> GetKeysAsync(string buildingId)
    {
        var response = await _httpClient.GetAsync($"/api/buildings/{buildingId}/keys");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IEnumerable<KeyInfo>>() ?? Enumerable.Empty<KeyInfo>();
    }

    public async Task AddKeyAsync(string buildingId, KeyInfoRequest key)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/buildings/{buildingId}/keys", key);
        await ApiErrors.EnsureSuccessAsync(response, "Could not add the key.");
    }

    public async Task CheckoutKeyAsync(string buildingId, Guid keyId, KeyCheckoutRequest checkout)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/buildings/{buildingId}/keys/{keyId}/checkout", checkout);
        await ApiErrors.EnsureSuccessAsync(response, "Could not check the key out.");
    }

    public async Task CheckinKeyAsync(string buildingId, Guid keyId, string? condition, string? notes)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/buildings/{buildingId}/keys/{keyId}/checkin",
            new { Condition = condition, Notes = notes });
        await ApiErrors.EnsureSuccessAsync(response, "Could not check the key in.");
    }

    public async Task SetKeyStatusAsync(string buildingId, Guid keyId, KeyStatus status, string? note)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/buildings/{buildingId}/keys/{keyId}/status",
            new { Status = status, Note = note });
        await ApiErrors.EnsureSuccessAsync(response, "Could not change the key's status.");
    }

    public async Task<List<KeyCheckoutRecord>> GetKeyHistoryAsync(string buildingId, Guid keyId)
    {
        var response = await _httpClient.GetAsync($"/api/buildings/{buildingId}/keys/{keyId}/history");
        await ApiErrors.EnsureSuccessAsync(response, "Could not load the key's history.");
        return await response.Content.ReadFromJsonAsync<List<KeyCheckoutRecord>>() ?? new List<KeyCheckoutRecord>();
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
        await ApiErrors.EnsureSuccessAsync(response, "Could not report the problem.");
        return await response.Content.ReadFromJsonAsync<WorkOrderInfo>()
            ?? throw new InvalidOperationException("The API returned an empty work order response.");
    }

    public async Task<WorkOrderInfo> UpdateWorkOrderStatusAsync(string buildingId, Guid workOrderId, WorkOrderStatus status, string? notes)
    {
        var response = await _httpClient.PatchAsJsonAsync($"/api/buildings/{buildingId}/workorders/{workOrderId}/status",
            new { Status = status, Notes = notes });
        await ApiErrors.EnsureSuccessAsync(response, "Could not update the work order.");
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

    public async Task SetFeeStructureAsync(string buildingId, string name, decimal amountPerM2, decimal? fixedAmount, DateTime? effectiveFrom = null)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/buildings/{buildingId}/finance/fee-structure",
            new
            {
                Name = name,
                AmountPerM2 = amountPerM2,
                FixedAmount = fixedAmount,
                EffectiveFrom = effectiveFrom?.ToString("yyyy-MM-dd")
            });
        await ApiErrors.EnsureSuccessAsync(response, "Could not save the fee.");
    }

    public async Task<List<FeeStructureInfo>> GetFeeStructuresAsync(string buildingId)
    {
        var response = await _httpClient.GetAsync($"/api/buildings/{buildingId}/finance/fee-structures");
        await ApiErrors.EnsureSuccessAsync(response, "Could not load the fee structures.");
        return await response.Content.ReadFromJsonAsync<List<FeeStructureInfo>>() ?? new List<FeeStructureInfo>();
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
