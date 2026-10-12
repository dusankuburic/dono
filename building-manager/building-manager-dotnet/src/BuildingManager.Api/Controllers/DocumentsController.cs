using BuildingManager.Api.Authorization;
using BuildingManager.Core.Enums;
using BuildingManager.Infrastructure.Storage;
using BuildingManager.Orleans.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace BuildingManager.Api.Controllers;

/// <summary>
/// Building documents (UC-026): PDFs and images filed per building. Managers
/// upload and see everything; other members see and download only documents
/// shared with all owners.
/// </summary>
[ApiController]
[Route("api/buildings/{buildingId}/documents")]
[Authorize(Policy = AuthPolicies.BuildingAccess)]
public class DocumentsController : ControllerBase
{
    /// <summary>One byte of headroom over the 25 MB document limit, for multipart framing.</summary>
    private const long MaxRequestBytes = 26L * 1024 * 1024;

    /// <summary>PDF plus the image types phones and browsers produce.</summary>
    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif",
        [".heic"] = "image/heic"
    };

    private readonly IGrainFactory _grainFactory;
    private readonly IBuildingDocumentStorage _storage;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(IGrainFactory grainFactory, IBuildingDocumentStorage storage,
        ILogger<DocumentsController> logger)
    {
        _grainFactory = grainFactory;
        _storage = storage;
        _logger = logger;
    }

    /// <summary>
    /// The building's documents, newest first. Managers get all of them;
    /// everyone else only those shared with all owners.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentInfo>>> GetDocuments(string buildingId)
    {
        var documents = await Grain(buildingId).GetDocumentsAsync();
        if (AuthPolicies.IsManager(User))
            return Ok(documents);

        return Ok(documents.Where(d => d.AccessLevel == DocumentAccessLevel.AllOwners));
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    [RequestSizeLimit(MaxRequestBytes)]
    public async Task<ActionResult<DocumentInfo>> Upload(string buildingId,
        IFormFile file, [FromForm] DocumentFolder folder, [FromForm] DocumentAccessLevel accessLevel,
        [FromForm] string? description)
    {
        // Browsers send the whole client path in some cases; only the name matters.
        var fileName = Path.GetFileName(file?.FileName ?? string.Empty).Trim();
        if (file is null || file.Length == 0 || string.IsNullOrEmpty(fileName))
            return BadRequestProblem("Choose a file to upload.");

        var extension = Path.GetExtension(fileName);
        if (!AllowedTypes.TryGetValue(extension, out var contentType))
            return BadRequestProblem("Only PDF and image files (PDF, PNG, JPG, WebP, GIF, HEIC) can be uploaded.");
        if (file.Length > BuildingDocumentLimits.MaxDocumentSizeBytes)
            return BadRequestProblem($"A document may be at most {BuildingDocumentLimits.MaxDocumentSizeBytes / (1024 * 1024)} MB.");

        var documentId = Guid.NewGuid();
        await using var stream = file.OpenReadStream();
        await _storage.UploadAsync(buildingId, documentId, contentType, stream);

        var document = new DocumentInfo
        {
            Id = documentId,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = file.Length,
            Folder = folder,
            AccessLevel = accessLevel,
            Description = description,
            // The uploader is whoever is signed in, never what the client claims.
            UploadedBy = Guid.TryParse(User.FindFirst("sub")?.Value, out var userId) ? userId : Guid.Empty,
            UploadedByName = User.FindFirst("name")?.Value,
            UploadedAt = DateTime.UtcNow
        };

        try
        {
            await Grain(buildingId).AddDocumentAsync(document);
        }
        catch
        {
            // The grain rejected the record (quota or validation): don't leave an orphaned blob.
            try
            {
                await _storage.DeleteAsync(buildingId, documentId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not remove the orphaned blob for document {DocumentId}", documentId);
            }
            throw;
        }

        return StatusCode(StatusCodes.Status201Created, document);
    }

    /// <summary>Streams the file. Owners can only download documents shared with all owners.</summary>
    [HttpGet("{documentId:guid}/content")]
    public async Task<IActionResult> Download(string buildingId, Guid documentId)
    {
        var document = await Grain(buildingId).GetDocumentAsync(documentId);
        if (!AuthPolicies.IsManager(User) && document.AccessLevel != DocumentAccessLevel.AllOwners)
            return Forbid();

        var content = await _storage.OpenReadAsync(buildingId, documentId);
        return File(content, document.ContentType, document.FileName);
    }

    [HttpDelete("{documentId:guid}")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    public async Task<IActionResult> Delete(string buildingId, Guid documentId)
    {
        await Grain(buildingId).RemoveDocumentAsync(documentId);
        await _storage.DeleteAsync(buildingId, documentId);
        return Ok();
    }

    private IBuildingGrain Grain(string buildingId) => _grainFactory.GetGrain<IBuildingGrain>(buildingId);

    private ActionResult BadRequestProblem(string detail) =>
        BadRequest(new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = "Invalid request",
            Detail = detail,
            Status = StatusCodes.Status400BadRequest
        });
}
