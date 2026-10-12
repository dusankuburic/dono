using System.Net.Http;
using System.Net.Http.Json;
using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingManager.Blazor.Services;

/// <summary>Building documents. Failures throw InvalidOperationException with the API's message.</summary>
public interface IDocumentService
{
    /// <summary>Documents the user may see (managers: all; others: shared with owners), newest first.</summary>
    Task<List<DocumentInfo>> GetDocumentsAsync(string buildingId);

    Task<DocumentInfo> UploadAsync(string buildingId, IBrowserFile file, DocumentFolder folder,
        DocumentAccessLevel accessLevel, string? description);

    Task<DownloadedDocument> DownloadAsync(string buildingId, Guid documentId);

    Task DeleteAsync(string buildingId, Guid documentId);
}

public record DownloadedDocument(string FileName, string ContentType, byte[] Content);

public class DocumentService : IDocumentService
{
    private readonly HttpClient _httpClient;

    public DocumentService([FromKeyedServices("ApiHttpClient")] HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private static string Base(string buildingId) => $"/api/buildings/{Uri.EscapeDataString(buildingId)}/documents";

    public async Task<List<DocumentInfo>> GetDocumentsAsync(string buildingId)
    {
        var response = await _httpClient.GetAsync(Base(buildingId));
        await ApiErrors.EnsureSuccessAsync(response, "Could not load the documents.");
        return await response.Content.ReadFromJsonAsync<List<DocumentInfo>>() ?? new();
    }

    public async Task<DocumentInfo> UploadAsync(string buildingId, IBrowserFile file, DocumentFolder folder,
        DocumentAccessLevel accessLevel, string? description)
    {
        await using var stream = file.OpenReadStream(BuildingDocumentLimits.MaxDocumentSizeBytes);
        using var content = new MultipartFormDataContent
        {
            { new StreamContent(stream), "file", file.Name },
            { new StringContent(folder.ToString()), "folder" },
            { new StringContent(accessLevel.ToString()), "accessLevel" }
        };
        if (!string.IsNullOrWhiteSpace(description))
            content.Add(new StringContent(description.Trim()), "description");

        var response = await _httpClient.PostAsync(Base(buildingId), content);
        await ApiErrors.EnsureSuccessAsync(response, "Could not upload the document.");
        return await response.Content.ReadFromJsonAsync<DocumentInfo>()
            ?? throw new InvalidOperationException("Could not upload the document.");
    }

    public async Task<DownloadedDocument> DownloadAsync(string buildingId, Guid documentId)
    {
        var response = await _httpClient.GetAsync($"{Base(buildingId)}/{documentId}/content");
        await ApiErrors.EnsureSuccessAsync(response, "Could not download the document.");
        var content = await response.Content.ReadAsByteArrayAsync();
        return new DownloadedDocument(
            response.Content.Headers.ContentDisposition?.FileNameStar?.Trim('"') ?? "document",
            response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
            content);
    }

    public async Task DeleteAsync(string buildingId, Guid documentId)
    {
        var response = await _httpClient.DeleteAsync($"{Base(buildingId)}/{documentId}");
        await ApiErrors.EnsureSuccessAsync(response, "Could not delete the document.");
    }
}
