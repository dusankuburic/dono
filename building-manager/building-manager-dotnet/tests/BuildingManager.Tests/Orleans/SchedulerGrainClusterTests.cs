using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;
using FluentAssertions;
using Orleans;
using Xunit;

namespace BuildingManager.Tests.Orleans;

/// <summary>
/// The background-job scheduler: publishing scheduled announcements, the
/// managers' daily digest (overdue keys, emergencies past 48 hours) and
/// assembly reminders. Each test drives its own scheduler instance (production
/// uses the "global" singleton) so the shared daily-pass date never leaks
/// between tests.
/// </summary>
public class SchedulerGrainClusterTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fixture;

    public SchedulerGrainClusterTests(ClusterFixture fixture)
    {
        _fixture = fixture;
    }

    private IGrainFactory GrainFactory => _fixture.Cluster.GrainFactory;

    private ISchedulerGrain Scheduler() =>
        GrainFactory.GetGrain<ISchedulerGrain>($"test-{Guid.NewGuid():N}");

    /// <summary>A building with a manager and an owner as members (accounts + access).</summary>
    private async Task<(string BuildingId, IBuildingGrain Building, string ManagerEmail, string OwnerEmail)> SetupAsync()
    {
        var buildingId = $"swept-{Guid.NewGuid():N}";
        var building = GrainFactory.GetGrain<IBuildingGrain>(buildingId);
        await building.InitializeAsync(new BuildingInfo
        {
            Name = "Swept Building", Street = "Futoska", StreetNumber = "41",
            City = "Novi Sad", TotalFloors = 2, TotalAreaM2 = 200
        });

        var managerEmail = $"manager-{Guid.NewGuid():N}@test.local";
        var ownerEmail = $"owner-{Guid.NewGuid():N}@test.local";
        foreach (var (email, role) in new[] { (managerEmail, UserRole.Manager), (ownerEmail, UserRole.Owner) })
        {
            var user = GrainFactory.GetGrain<IUserGrain>(email);
            await user.RegisterAsync(new RegisterUserRequest
            {
                Email = email, DisplayName = email, Role = role, Password = "S3curePass!"
            });
            await user.GrantBuildingAccessAsync(buildingId);
        }

        return (buildingId, building, managerEmail, ownerEmail);
    }

    private INotificationGrain Notifications(string email) => GrainFactory.GetGrain<INotificationGrain>(email);

    [Fact]
    public async Task Due_Scheduled_Announcements_Are_Published_And_Members_Notified_Once()
    {
        var (_, building, managerEmail, ownerEmail) = await SetupAsync();
        await building.CreateAnnouncementAsync(new AnnouncementInfo
        {
            Id = Guid.NewGuid(),
            Title = "Water off tomorrow",
            Content = "The water is off between 9 and 12.",
            Type = AnnouncementType.General,
            ScheduledFor = DateTime.UtcNow.AddSeconds(2),
            CreatedBy = Guid.NewGuid()
        });

        // Until it is due nothing happens.
        var scheduler = Scheduler();
        await scheduler.RunSweepAsync();
        (await Notifications(managerEmail).GetRecentAsync()).Should().BeEmpty();

        await Task.Delay(2500);
        await scheduler.RunSweepAsync();

        var title = "Announcement: Water off tomorrow";
        (await Notifications(managerEmail).GetRecentAsync()).Select(n => n.Title).Should().Contain(title);
        (await Notifications(ownerEmail).GetRecentAsync()).Select(n => n.Title).Should().Contain(title);
        (await building.GetAnnouncementsAsync()).Single().SentAt.Should().NotBeNull();

        // A second sweep the same day does not repeat the notification.
        await scheduler.RunSweepAsync();
        (await Notifications(managerEmail).GetRecentAsync()).Count(n => n.Title == title).Should().Be(1);
    }

    [Fact]
    public async Task Overdue_Keys_Digest_Reaches_Managers_Only_And_Once_Per_Day()
    {
        var (_, building, managerEmail, ownerEmail) = await SetupAsync();
        var keyId = Guid.NewGuid();
        await building.AddKeyAsync(new KeyInfo { Id = keyId, Type = KeyType.Unit, Identifier = "K-9", Status = KeyStatus.Available });
        await building.CheckoutKeyAsync(keyId, new KeyCheckoutRequest
        {
            RecipientName = "Petar Petrovic",
            RecipientType = RecipientType.Resident,
            Purpose = "Viewing",
            // A return date that slips into the past during the test.
            ExpectedReturn = DateTime.UtcNow.AddSeconds(2)
        });
        await Task.Delay(2500);

        var scheduler = Scheduler();
        await scheduler.RunSweepAsync();

        var manager = await Notifications(managerEmail).GetRecentAsync();
        manager.Should().Contain(n => n.Title == "1 key is overdue for return");
        (await Notifications(ownerEmail).GetRecentAsync()).Should().BeEmpty("owners do not get the managers' digest");

        await scheduler.RunSweepAsync();
        (await Notifications(managerEmail).GetRecentAsync())
            .Count(n => n.Title == "1 key is overdue for return").Should().Be(1);
    }

    [Fact]
    public async Task Emergencies_Within_48_Hours_Are_Not_Flagged()
    {
        var (_, building, managerEmail, _) = await SetupAsync();
        await building.CreateWorkOrderAsync(new CreateWorkOrderRequest
        {
            Title = "Flooded basement",
            Description = "Water everywhere",
            Category = WorkOrderCategory.Plumbing,
            Priority = Priority.Emergency,
            ReportedByResidentId = Guid.NewGuid(),
            IsEmergency = true
        });

        var scheduler = Scheduler();
        await scheduler.RunSweepAsync();

        (await Notifications(managerEmail).GetRecentAsync())
            .Should().NotContain(n => n.Title == "Emergency past the 48-hour deadline");
    }

    [Fact]
    public async Task Assembly_Reminder_Is_Sent_Once_Per_Stage()
    {
        var (buildingId, _, managerEmail, ownerEmail) = await SetupAsync();
        var assemblyId = Guid.NewGuid();
        await GrainFactory.GetGrain<IAssemblyGrain>($"{buildingId}|{assemblyId}").ScheduleAsync(new ScheduleAssemblyRequest
        {
            Date = DateTime.UtcNow.AddHours(36), // inside the two-day window
            Location = "Entrance hall",
            AllowShortNotice = true,
            Agenda = new List<AgendaItemDraft> { new() { Title = "Repairs" } }
        });
        await GrainFactory.GetGrain<IAssemblyGrain>($"{buildingId}|{assemblyId}").MarkNoticeSentAsync(true);

        var scheduler = Scheduler();
        await scheduler.RunSweepAsync();

        var title = "Reminder: owners' assembly";
        (await Notifications(managerEmail).GetRecentAsync()).Count(n => n.Title == title).Should().Be(1);
        (await Notifications(ownerEmail).GetRecentAsync()).Count(n => n.Title == title).Should().Be(1);

        // Still in the same stage on a second pass: no repeat.
        await scheduler.RunSweepAsync();
        (await Notifications(managerEmail).GetRecentAsync()).Count(n => n.Title == title).Should().Be(1);
    }
}
