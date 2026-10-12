using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace BuildingManager.Infrastructure.Storage;

/// <summary>
/// Stores building document content (PDFs, images) outside grain state,
/// which only keeps the metadata. Local development runs against Azurite
/// (docker-compose) via "UseDevelopmentStorage=true".
/// </summary>
public interface IBuildingDocumentStorage
{
    Task UploadAsync(string buildingId, Guid documentId, string contentType, Stream content);

    /// <summary>Opens the blob for reading; the caller disposes the stream.</summary>
    Task<Stream> OpenReadAsync(string buildingId, Guid documentId);

    /// <summary>Removes the blob; a missing blob is not an error (the record may already be gone).</summary>
    Task DeleteAsync(string buildingId, Guid documentId);
}

/// <summary>
/// Azure Blob Storage implementation. One container holds every building;
/// blobs are named "{buildingId}/{documentId}" so buildings never mix and
/// display name / content type come from the grain metadata, not the blob.
/// </summary>
public class AzureBlobDocumentStorage : IBuildingDocumentStorage
{
    private readonly BlobContainerClient _container;
    private bool _containerEnsured;

    public AzureBlobDocumentStorage(string connectionString, string containerName)
    {
        _container = new BlobContainerClient(connectionString, containerName);
    }

    public async Task UploadAsync(string buildingId, Guid documentId, string contentType, Stream content)
    {
        await EnsureContainerAsync();
        var blob = _container.GetBlobClient(BlobName(buildingId, documentId));
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        });
    }

    public async Task<Stream> OpenReadAsync(string buildingId, Guid documentId)
    {
        await EnsureContainerAsync();
        var blob = _container.GetBlobClient(BlobName(buildingId, documentId));
        try
        {
            return await blob.OpenReadAsync();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new KeyNotFoundException($"Document content '{documentId}' not found.");
        }
    }

    public async Task DeleteAsync(string buildingId, Guid documentId)
    {
        await EnsureContainerAsync();
        var blob = _container.GetBlobClient(BlobName(buildingId, documentId));
        await blob.DeleteIfExistsAsync();
    }

    /// <summary>Create the container on first use so a fresh Azurite/account just works.</summary>
    private async Task EnsureContainerAsync()
    {
        if (_containerEnsured)
            return;
        await _container.CreateIfNotExistsAsync();
        _containerEnsured = true;
    }

    private static string BlobName(string buildingId, Guid documentId) =>
        $"{Uri.EscapeDataString(buildingId)}/{documentId}";
}
