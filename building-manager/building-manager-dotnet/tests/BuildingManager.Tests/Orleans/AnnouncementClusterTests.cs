using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;
using FluentAssertions;
using Orleans;
using Xunit;

namespace BuildingManager.Tests.Orleans;

public class AnnouncementClusterTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fixture;

    public AnnouncementClusterTests(ClusterFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<IBuildingGrain> Building()
    {
        var grain = _fixture.Cluster.GrainFactory.GetGrain<IBuildingGrain>($"announce-{Guid.NewGuid():N}");
        await grain.InitializeAsync(new BuildingInfo
        {
            Name = "Announcement Building", Street = "Bulevar", StreetNumber = "7",
            City = "Novi Sad", TotalFloors = 4, TotalAreaM2 = 400
        });
        return grain;
    }

    private static AnnouncementInfo Announcement(string title, AnnouncementType type = AnnouncementType.General) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Content = $"{title} - details",
        Type = type,
        CreatedBy = Guid.NewGuid(),
        CreatedByName = "Marija Manager"
    };

    [Fact]
    public async Task Invalid_Announcements_Are_Rejected()
    {
        var grain = await Building();

        var noTitle = () => grain.CreateAnnouncementAsync(Announcement("") );
        var noMessage = () => grain.CreateAnnouncementAsync(Announcement("Water off") with { Content = " " });
        var scheduledInPast = () => grain.CreateAnnouncementAsync(
            Announcement("Water off") with { ScheduledFor = DateTime.UtcNow.AddHours(-2) });
        var expiresBeforePublished = () => grain.CreateAnnouncementAsync(Announcement("Water off") with
        {
            ScheduledFor = DateTime.UtcNow.AddDays(2),
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        });

        await noTitle.Should().ThrowAsync<ArgumentException>();
        await noMessage.Should().ThrowAsync<ArgumentException>();
        await scheduledInPast.Should().ThrowAsync<ArgumentException>();
        await expiresBeforePublished.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Scheduled_Announcements_Stay_Out_Of_The_Feed_Until_Published()
    {
        var grain = await Building();
        var now = Announcement("Cleaning on Monday");
        var later = Announcement("Elevator inspection") with { ScheduledFor = DateTime.UtcNow.AddDays(3) };

        await grain.CreateAnnouncementAsync(now);
        await grain.CreateAnnouncementAsync(later);

        (await grain.GetAnnouncementsAsync()).Select(a => a.Id).Should().Equal(now.Id);
        (await grain.GetAnnouncementHistoryAsync()).Select(a => a.Id).Should().Contain(new[] { now.Id, later.Id });
    }

    [Fact]
    public async Task Pinned_Announcements_Lead_The_Feed()
    {
        var grain = await Building();
        var older = Announcement("House rules");
        await grain.CreateAnnouncementAsync(older);
        var newer = Announcement("Heating starts");
        await grain.CreateAnnouncementAsync(newer);

        (await grain.GetAnnouncementsAsync()).First().Id.Should().Be(newer.Id);

        await grain.SetAnnouncementPinnedAsync(older.Id, true);
        var feed = (await grain.GetAnnouncementsAsync()).ToList();
        feed.First().Id.Should().Be(older.Id);
        feed.First().IsPinned.Should().BeTrue();
        feed.First().CreatedByName.Should().Be("Marija Manager");

        var unknown = () => grain.SetAnnouncementPinnedAsync(Guid.NewGuid(), true);
        await unknown.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Archived_Announcements_Leave_The_Feed_But_Stay_In_The_History()
    {
        var grain = await Building();
        var notice = Announcement("Water off on Friday", AnnouncementType.Maintenance) with { IsPinned = true };
        await grain.CreateAnnouncementAsync(notice);

        await grain.ArchiveAnnouncementAsync(notice.Id);

        await grain.CreateAnnouncementAsync(Announcement("Next notice"));

        (await grain.GetAnnouncementsAsync()).Should().NotContain(a => a.Id == notice.Id);
        var archived = (await grain.GetAnnouncementHistoryAsync()).Single(a => a.Id == notice.Id);
        archived.ExpiresAt.Should().BeOnOrBefore(DateTime.UtcNow);
        archived.IsPinned.Should().BeFalse("an archived announcement is no longer pinned");
    }
}
