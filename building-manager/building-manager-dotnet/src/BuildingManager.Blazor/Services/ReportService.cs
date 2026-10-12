using System.Net.Http;
using BuildingManager.Orleans.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingManager.Blazor.Services;

/// <summary>Server-generated PDF/Excel reports. Failures throw InvalidOperationException with the API's message.</summary>
public interface IReportService
{
    Task<DownloadedDocument> GetAssemblyNoticePdfAsync(string buildingId, Guid assemblyId);
    Task<DownloadedDocument> GetAssemblyMinutesPdfAsync(string buildingId, Guid assemblyId);
    Task<DownloadedDocument> GetFeeStatusPdfAsync(string buildingId, int year, int month);
    Task<DownloadedDocument> GetFeeStatusExcelAsync(string buildingId, int year, int month);
}

public class ReportService : IReportService
{
    private readonly HttpClient _httpClient;

    public ReportService([FromKeyedServices("ApiHttpClient")] HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<DownloadedDocument> GetAssemblyNoticePdfAsync(string buildingId, Guid assemblyId) =>
        DownloadAsync($"/api/buildings/{Uri.EscapeDataString(buildingId)}/assemblies/{assemblyId}/notice/pdf",
            "Could not download the notice.");

    public Task<DownloadedDocument> GetAssemblyMinutesPdfAsync(string buildingId, Guid assemblyId) =>
        DownloadAsync($"/api/buildings/{Uri.EscapeDataString(buildingId)}/assemblies/{assemblyId}/minutes/pdf",
            "Could not download the minutes.");

    public Task<DownloadedDocument> GetFeeStatusPdfAsync(string buildingId, int year, int month) =>
        DownloadAsync($"/api/buildings/{Uri.EscapeDataString(buildingId)}/finance/status/pdf?year={year}&month={month}",
            "Could not download the fee status.");

    public Task<DownloadedDocument> GetFeeStatusExcelAsync(string buildingId, int year, int month) =>
        DownloadAsync($"/api/buildings/{Uri.EscapeDataString(buildingId)}/finance/status/xlsx?year={year}&month={month}",
            "Could not download the fee status.");

    private async Task<DownloadedDocument> DownloadAsync(string url, string fallbackMessage)
    {
        var response = await _httpClient.GetAsync(url);
        await ApiErrors.EnsureSuccessAsync(response, fallbackMessage);
        var content = await response.Content.ReadAsByteArrayAsync();
        return new DownloadedDocument(
            response.Content.Headers.ContentDisposition?.FileNameStar?.Trim('"') ?? "report",
            response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
            content);
    }
}
