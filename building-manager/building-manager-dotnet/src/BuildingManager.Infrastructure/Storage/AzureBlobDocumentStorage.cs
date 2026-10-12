using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace BuildingManager.Infrastructure.Storage;

public interface IBuildingDocumentStorage
{
    Task UploadAsync(string buildingId, Guid documentId, string contentType, Stream content);

        Task<Stream> OpenReadAsync(string buildingId, Guid documentId);

        Task DeleteAsync(string buildingId, Guid documentId);
}

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
