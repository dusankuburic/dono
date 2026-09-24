using System.Net.Http.Json;
using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;

namespace BuildingManager.Blazor.Services;

public class BuildingService : IBuildingService
{
    private readonly HttpClient _httpClient;

    public BuildingService(HttpClient httpClient)
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

    public async Task<IEnumerable<ResidentInfo>> GetResidentsAsync(string buildingId)
    {
        var response = await _httpClient.GetAsync($"/api/buildings/{buildingId}/residents");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IEnumerable<ResidentInfo>>() ?? Enumerable.Empty<ResidentInfo>();
    }

    public async Task AddResidentAsync(string buildingId, ResidentInfoRequest resident)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/buildings/{buildingId}/residents", resident);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IEnumerable<KeyInfo>> GetKeysAsync(string buildingId)
    {
        var response = await _httpClient.GetAsync($"/api/buildings/{buildingId}/keys");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IEnumerable<KeyInfo>>() ?? Enumerable.Empty<KeyInfo>();
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
}
