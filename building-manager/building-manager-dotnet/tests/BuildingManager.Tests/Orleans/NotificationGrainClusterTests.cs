using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Grains;
using BuildingManager.Orleans.Interfaces;
using FluentAssertions;
using Orleans;
using Xunit;

namespace BuildingManager.Tests.Orleans;

/// <summary>
/// In-app notifications per user (keyed by email): adding, read tracking and
/// the 100-item cap that never drops unread items while read ones exist.
/// </summary>
public class NotificationGrainClusterTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fixture;

    public NotificationGrainClusterTests(ClusterFixture fixture)
    {
        _fixture = fixture;
    }

    private INotificationGrain Grain() =>
        _fixture.Cluster.GrainFactory.GetGrain<INotificationGrain>($"notify-{Guid.NewGuid():N}");

    private static NotificationInfo Note(string title, NotificationType type = NotificationType.System) => new()
    {
        Id = Guid.NewGuid(),
        Type = type,
        Title = title,
        BuildingId = "b1",
        Link = "/announcements"
    };

    [Fact]
    public async Task Notifications_Are_Added_Unread_And_Listed_Newest_First()
    {
        var grain = Grain();
        await grain.AddAsync(Note("First"));
        await grain.AddAsync(Note("Second", NotificationType.Announcement));

        var recent = await grain.GetRecentAsync();
        recent.Select(n => n.Title).Should().Equal("Second", "First");
        recent[0].IsRead.Should().BeFalse();
        recent[0].Link.Should().Be("/announcements");
        (await grain.GetUnreadCountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Notifications_Can_Be_Marked_Read()
    {
        var grain = Grain();
        var first = Note("First");
        var second = Note("Second");
        await grain.AddAsync(first);
        await grain.AddAsync(second);

        await grain.MarkReadAsync(first.Id);
        // Marking an unknown notification is a no-op, not an error.
        await grain.MarkReadAsync(Guid.NewGuid());

        (await grain.GetUnreadCountAsync()).Should().Be(1);
        (await grain.GetRecentAsync()).Single(n => n.Id == first.Id).IsRead.Should().BeTrue();

        await grain.MarkAllReadAsync();
        (await grain.GetUnreadCountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Invalid_Notifications_Are_Rejected()
    {
        var grain = Grain();

        var noTitle = () => grain.AddAsync(Note(" "));
        var badType = () => grain.AddAsync(Note("Broken") with { Type = (NotificationType)99 });

        await noTitle.Should().ThrowAsync<ArgumentException>();
        await badType.Should().ThrowAsync<ArgumentException>();
        (await grain.GetUnreadCountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Over_The_Limit_The_Oldest_Read_Items_Go_First()
    {
        var grain = Grain();
        var read = Note("old but read");
        await grain.AddAsync(read);
        await grain.MarkReadAsync(read.Id);

        var keep = NotificationGrain.KeepLimit;
        var unread = Enumerable.Range(0, keep).Select(i => Note($"unread {i}")).ToList();
        foreach (var note in unread)
        {
            await grain.AddAsync(note);
        }
        // One more pushes the total over the limit: the read one is dropped,
        // the unread ones stay.
        var newest = Note("newest");
        await grain.AddAsync(newest);

        var recent = await grain.GetRecentAsync(keep);
        recent.Should().HaveCount(keep);
        recent.Select(n => n.Title).Should().NotContain("old but read");
        (await grain.GetUnreadCountAsync()).Should().Be(keep);
        recent[0].Title.Should().Be("newest");
    }
}
