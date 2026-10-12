using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;
using FluentAssertions;
using Orleans;
using Xunit;

namespace BuildingManager.Tests.Orleans;

/// <summary>
/// Document records on the building grain: validation, the 1 GB per-building
/// quota and removal. The file bytes live in blob storage and are covered by
/// the API layer; these tests exercise the metadata only.
/// </summary>
public class DocumentClusterTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fixture;

    public DocumentClusterTests(ClusterFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<IBuildingGrain> Building()
    {
        var grain = _fixture.Cluster.GrainFactory.GetGrain<IBuildingGrain>($"documents-{Guid.NewGuid():N}");
        await grain.InitializeAsync(new BuildingInfo
        {
            Name = "Document Building", Street = "Dunavska", StreetNumber = "12",
            City = "Novi Sad", TotalFloors = 3, TotalAreaM2 = 300
        });
        return grain;
    }

    private static DocumentInfo Doc(string name, long sizeBytes,
        DocumentFolder folder = DocumentFolder.Other,
        DocumentAccessLevel access = DocumentAccessLevel.ManagersOnly) => new()
    {
        Id = Guid.NewGuid(),
        FileName = name,
        ContentType = "application/pdf",
        SizeBytes = sizeBytes,
        Folder = folder,
        AccessLevel = access,
        UploadedBy = Guid.NewGuid(),
        UploadedByName = "Marija Manager"
    };

    [Fact]
    public async Task Documents_Are_Listed_Newest_First_And_Removed()
    {
        var grain = await Building();
        var older = Doc("registration-decision.pdf", 1024, DocumentFolder.Registration, DocumentAccessLevel.AllOwners);
        var newer = Doc("elevator-contract.pdf", 2048, DocumentFolder.Contracts);
        await grain.AddDocumentAsync(older);
        await grain.AddDocumentAsync(newer);

        var documents = (await grain.GetDocumentsAsync()).ToList();
        documents.Select(d => d.Id).Should().Equal(newer.Id, older.Id);
        documents.Single(d => d.Id == older.Id).AccessLevel.Should().Be(DocumentAccessLevel.AllOwners);
        documents.Single(d => d.Id == newer.Id).UploadedByName.Should().Be("Marija Manager");

        (await grain.GetDocumentAsync(newer.Id)).FileName.Should().Be("elevator-contract.pdf");

        (await grain.RemoveDocumentAsync(older.Id)).Id.Should().Be(older.Id);
        (await grain.GetDocumentsAsync()).Should().NotContain(d => d.Id == older.Id);
        var missing = () => grain.GetDocumentAsync(older.Id);
        await missing.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Invalid_Documents_Are_Rejected()
    {
        var grain = await Building();

        var noName = () => grain.AddDocumentAsync(Doc("  ", 1024));
        var longName = () => grain.AddDocumentAsync(Doc(new string('a', 201), 1024));
        var noType = () => grain.AddDocumentAsync(Doc("plan.pdf", 1024) with { ContentType = " " });
        var empty = () => grain.AddDocumentAsync(Doc("plan.pdf", 0));
        var tooBig = () => grain.AddDocumentAsync(Doc("plan.pdf", BuildingDocumentLimits.MaxDocumentSizeBytes + 1));
        var badFolder = () => grain.AddDocumentAsync(Doc("plan.pdf", 1024) with { Folder = (DocumentFolder)99 });
        var badAccess = () => grain.AddDocumentAsync(Doc("plan.pdf", 1024) with { AccessLevel = (DocumentAccessLevel)99 });

        await noName.Should().ThrowAsync<ArgumentException>();
        await longName.Should().ThrowAsync<ArgumentException>();
        await noType.Should().ThrowAsync<ArgumentException>();
        await empty.Should().ThrowAsync<ArgumentException>();
        await tooBig.Should().ThrowAsync<ArgumentException>();
        await badFolder.Should().ThrowAsync<ArgumentException>();
        await badAccess.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Building_Quota_Is_Enforced()
    {
        var grain = await Building();
        var max = BuildingDocumentLimits.MaxDocumentSizeBytes;

        // Exactly this many maximum-size files fit under the quota.
        var count = (int)(BuildingDocumentLimits.BuildingQuotaBytes / max);
        for (var i = 0; i < count; i++)
        {
            await grain.AddDocumentAsync(Doc($"part-{i}.pdf", max));
        }

        var over = () => grain.AddDocumentAsync(Doc("one-too-many.pdf", max));
        await over.Should().ThrowAsync<InvalidOperationException>();

        // Freeing space lets the same upload through.
        await grain.RemoveDocumentAsync((await grain.GetDocumentsAsync()).First().Id);
        await grain.AddDocumentAsync(Doc("one-more.pdf", max));
        (await grain.GetDocumentsAsync()).Should().HaveCount(count);
    }

    [Fact]
    public async Task Removing_An_Unknown_Document_Fails()
    {
        var grain = await Building();

        var missing = () => grain.RemoveDocumentAsync(Guid.NewGuid());
        await missing.Should().ThrowAsync<KeyNotFoundException>();
    }
}
