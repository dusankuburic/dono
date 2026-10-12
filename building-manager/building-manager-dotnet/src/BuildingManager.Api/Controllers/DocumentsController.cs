using BuildingManager.Api.Authorization;
using BuildingManager.Core.Enums;
using BuildingManager.Infrastructure.Storage;
using BuildingManager.Orleans.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace BuildingManager.Api.Controllers;

[ApiController]
[Route("api/buildings/{buildingId}/documents")]
[Authorize(Policy = AuthPolicies.BuildingAccess)]
public class DocumentsController : ControllerBase
{
        private const long MaxRequestBytes = 26L * 1024 * 1024;

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
