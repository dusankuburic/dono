using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;
using FluentAssertions;
using Orleans;
using Xunit;

namespace BuildingManager.Tests.Orleans;

/// <summary>
/// Owners' assemblies on the real grains: scheduling and notice rules,
/// attendance-based quorum from unit shares, voting thresholds, completion
/// and the repeated session after a first session without quorum.
/// </summary>
public class AssemblyGrainClusterTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fixture;

    public AssemblyGrainClusterTests(ClusterFixture fixture)
    {
        _fixture = fixture;
    }

    private IGrainFactory GrainFactory => _fixture.Cluster.GrainFactory;

    /// <summary>A building whose three units hold 40, 35 and 25 percent.</summary>
    private async Task<string> BuildingWithUnits()
    {
        var buildingId = $"assembly-bldg-{Guid.NewGuid():N}";
        var building = GrainFactory.GetGrain<IBuildingGrain>(buildingId);
        await building.InitializeAsync(new BuildingInfo
        {
            Name = "Assembly Building", Street = "Knez Mihailova", StreetNumber = "1",
            City = "Beograd", TotalFloors = 3, TotalAreaM2 = 300
        });
        foreach (var (number, share) in new[] { ("1", 40m), ("2", 35m), ("3", 25m) })
        {
            await building.AddOrUpdateUnitAsync(new UnitInfo
            {
                Id = Guid.NewGuid(), Number = number, Floor = 1, AreaM2 = 100,
                OwnershipShare = share, Type = UnitType.Apartment, Status = UnitStatus.OccupiedByOwner
            });
        }
        return buildingId;
    }

    private IAssemblyGrain Assembly(string buildingId, Guid? id = null) =>
        GrainFactory.GetGrain<IAssemblyGrain>($"{buildingId}|{id ?? Guid.NewGuid()}");

    private static ScheduleAssemblyRequest Request(params AgendaItemDraft[] agenda) => new()
    {
        Date = DateTime.UtcNow.AddDays(7),
        Location = "Ground floor lobby",
        Agenda = agenda.Length > 0
            ? agenda.ToList()
            : new List<AgendaItemDraft> { new() { Title = "Facade repair", VotingType = VotingType.Simple } }
    };

    /// <summary>
    /// A meeting in half an hour (short notice confirmed), so it can be started
    /// now: a meeting cannot open more than an hour before its announced time.
    /// </summary>
    private static ScheduleAssemblyRequest SoonRequest(params AgendaItemDraft[] agenda) =>
        Request(agenda) with { Date = DateTime.UtcNow.AddMinutes(30), AllowShortNotice = true };

    /// <summary>Scheduled, notice sent and started: ready for attendance.</summary>
    private async Task<(IAssemblyGrain Grain, AssemblyDetails Details)> StartedAssembly(
        string buildingId, params AgendaItemDraft[] agenda)
    {
        var grain = Assembly(buildingId);
        await grain.ScheduleAsync(SoonRequest(agenda));
        await grain.MarkNoticeSentAsync(physicallyPosted: true);
        await grain.StartAsync();
        return (grain, await grain.GetAsync());
    }

    private static AttendanceDraft InPerson(string unit) =>
        new() { UnitNumber = unit, Type = AttendanceType.InPerson, AttendeeName = $"Owner {unit}" };

    [Fact]
    public async Task Scheduling_Rejects_Past_Dates_And_Empty_Agendas()
    {
        var grain = Assembly(await BuildingWithUnits());

        var past = () => grain.ScheduleAsync(Request() with { Date = DateTime.UtcNow.AddHours(-1) });
        var noAgenda = () => grain.ScheduleAsync(Request() with { Agenda = new() });

        await past.Should().ThrowAsync<ArgumentException>();
        await noAgenda.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Short_Notice_Needs_An_Explicit_Override()
    {
        var buildingId = await BuildingWithUnits();
        var tomorrow = Request() with { Date = DateTime.UtcNow.AddDays(1) };

        var withoutOverride = () => Assembly(buildingId).ScheduleAsync(tomorrow);
        await withoutOverride.Should().ThrowAsync<InvalidOperationException>().WithMessage("*3 days*");

        var details = await Assembly(buildingId).ScheduleAsync(tomorrow with { AllowShortNotice = true });
        details.HasShortNotice.Should().BeTrue();
        details.Status.Should().Be(AssemblyStatus.Scheduled);
    }

    [Fact]
    public async Task Full_Meeting_Decides_Items_By_Their_Voting_Type()
    {
        var buildingId = await BuildingWithUnits();
        var (grain, started) = await StartedAssembly(buildingId,
            new AgendaItemDraft { Title = "Annual budget", VotingType = VotingType.Simple },
            new AgendaItemDraft { Title = "Roof renovation", VotingType = VotingType.TwoThirds },
            new AgendaItemDraft { Title = "Manager report", RequiresVote = false });
        started.EligibleVotes.Should().Be(100m);
        var (budget, roof) = (started.Agenda[0].Id, started.Agenda[1].Id);

        // 40 in person + 25 by proxy = 65% present: quorum for a first session.
        await grain.RecordAttendanceAsync(InPerson("1"));
        await grain.RecordAttendanceAsync(new AttendanceDraft
        {
            UnitNumber = "3", Type = AttendanceType.Proxy, AttendeeName = "Owner 3", ProxyHolderName = "Neighbour"
        });

        await grain.RecordVoteAsync(budget, "1", VoteChoice.For);
        await grain.RecordVoteAsync(budget, "3", VoteChoice.Against);
        await grain.RecordVoteAsync(roof, "1", VoteChoice.For);
        await grain.RecordVoteAsync(roof, "3", VoteChoice.Abstain);

        // 40 of 65 is a simple majority but short of two thirds.
        (await grain.FinalizeAgendaItemAsync(budget)).Result.Should().Be(VoteResult.Passed);
        var roofResult = await grain.FinalizeAgendaItemAsync(roof);
        roofResult.Result.Should().Be(VoteResult.Failed);
        roofResult.VotesFor.Should().Be(40m);
        roofResult.VotesAbstain.Should().Be(25m);

        await grain.SetNotesAsync("Budget adopted; roof to be re-discussed.");
        await grain.CompleteAsync();

        var completed = await grain.GetAsync();
        completed.Status.Should().Be(AssemblyStatus.Completed);
        completed.QuorumAchieved.Should().BeTrue();
        completed.QuorumPercentage.Should().Be(65m);
        completed.PresentVotes.Should().Be(65m);
        completed.Attendance.Single(a => a.UnitNumber == "3").ProxyHolderName.Should().Be("Neighbour");
        completed.Agenda[2].IsFinalized.Should().BeTrue("informational items are closed on completion");
        completed.Notes.Should().Contain("Budget adopted");

        var listed = await GrainFactory.GetGrain<IAssemblyRegistryGrain>(buildingId).GetAssemblyIdsAsync();
        listed.Should().Contain(completed.Id);
    }

    [Fact]
    public async Task Without_Quorum_No_Votes_Are_Taken_And_A_Repeated_Session_Can_Follow()
    {
        var buildingId = await BuildingWithUnits();
        var (first, started) = await StartedAssembly(buildingId);
        var item = started.Agenda[0].Id;

        // 40% present: below the 51% a first session needs.
        await first.RecordAttendanceAsync(InPerson("1"));
        var vote = () => first.RecordVoteAsync(item, "1", VoteChoice.For);
        await vote.Should().ThrowAsync<InvalidOperationException>().WithMessage("Quorum not reached*");

        await first.CompleteAsync();
        var firstDetails = await first.GetAsync();
        firstDetails.QuorumAchieved.Should().BeFalse();
        firstDetails.Agenda[0].Result.Should().Be(VoteResult.Pending, "no decision without quorum");

        // The repeated session needs only one third, so the same 40% suffices.
        var repeated = Assembly(buildingId);
        await repeated.ScheduleAsync(SoonRequest() with
        {
            SessionType = SessionType.Repeated, PreviousAssemblyId = firstDetails.Id
        });
        await repeated.MarkNoticeSentAsync(physicallyPosted: false);
        await repeated.StartAsync();
        await repeated.RecordAttendanceAsync(InPerson("1"));
        var repeatedItem = (await repeated.GetAsync()).Agenda[0].Id;
        await repeated.RecordVoteAsync(repeatedItem, "1", VoteChoice.For);

        (await repeated.FinalizeAgendaItemAsync(repeatedItem)).Result.Should().Be(VoteResult.Passed);
        (await repeated.GetAsync()).PreviousAssemblyId.Should().Be(firstDetails.Id);
    }

    [Fact]
    public async Task A_Repeated_Session_Cannot_Follow_A_Session_That_Had_Quorum()
    {
        var buildingId = await BuildingWithUnits();
        var (first, started) = await StartedAssembly(buildingId);
        await first.RecordAttendanceAsync(InPerson("1"));
        await first.RecordAttendanceAsync(InPerson("2"));
        await first.RecordVoteAsync(started.Agenda[0].Id, "1", VoteChoice.For);
        await first.FinalizeAgendaItemAsync(started.Agenda[0].Id);
        await first.CompleteAsync();

        var repeat = () => Assembly(buildingId).ScheduleAsync(Request() with
        {
            SessionType = SessionType.Repeated, PreviousAssemblyId = started.Id
        });

        await repeat.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task The_Meeting_Cannot_Start_Long_Before_The_Announced_Time()
    {
        var grain = Assembly(await BuildingWithUnits());
        await grain.ScheduleAsync(Request()); // a week from now
        await grain.MarkNoticeSentAsync(physicallyPosted: true);

        var startNow = () => grain.StartAsync();

        await startNow.Should().ThrowAsync<InvalidOperationException>().WithMessage("*announced*");
        (await grain.GetAsync()).Status.Should().Be(AssemblyStatus.NoticeSent);
    }

    [Fact]
    public async Task Lifecycle_Rules_Are_Enforced()
    {
        var buildingId = await BuildingWithUnits();
        var grain = Assembly(buildingId);
        await grain.ScheduleAsync(SoonRequest());

        var startEarly = () => grain.StartAsync();
        await startEarly.Should().ThrowAsync<InvalidOperationException>("notice must be sent first");

        await grain.MarkNoticeSentAsync(physicallyPosted: false);
        var changeAgenda = () => grain.AddAgendaItemAsync(new AgendaItemDraft { Title = "Late item" });
        await changeAgenda.Should().ThrowAsync<InvalidOperationException>("the agenda is fixed once notice is sent");

        // Posting can be confirmed after the notice itself.
        await grain.MarkNoticeSentAsync(physicallyPosted: true);
        (await grain.GetAsync()).PhysicallyPostedAt.Should().NotBeNull();

        await grain.StartAsync();
        var item = (await grain.GetAsync()).Agenda[0].Id;

        var unknownUnit = () => grain.RecordAttendanceAsync(InPerson("99"));
        await unknownUnit.Should().ThrowAsync<KeyNotFoundException>();
        var proxyWithoutHolder = () => grain.RecordAttendanceAsync(
            new AttendanceDraft { UnitNumber = "1", Type = AttendanceType.Proxy });
        await proxyWithoutHolder.Should().ThrowAsync<ArgumentException>();

        await grain.RecordAttendanceAsync(InPerson("1"));
        await grain.RecordAttendanceAsync(InPerson("2"));
        var absentVoter = () => grain.RecordVoteAsync(item, "3", VoteChoice.For);
        await absentVoter.Should().ThrowAsync<InvalidOperationException>("unit 3 is not present");

        await grain.RecordVoteAsync(item, "1", VoteChoice.Against);
        await grain.RecordVoteAsync(item, "1", VoteChoice.For); // recast replaces
        (await grain.GetAsync()).Agenda[0].Votes.Should().ContainSingle(v => v.Choice == VoteChoice.For);

        var completeUndecided = () => grain.CompleteAsync();
        await completeUndecided.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Facade repair*");

        await grain.FinalizeAgendaItemAsync(item);
        var removeVoter = () => grain.RemoveAttendanceAsync("1");
        await removeVoter.Should().ThrowAsync<InvalidOperationException>("its vote decided an item");
        await grain.RemoveAttendanceAsync("2"); // did not vote: can be corrected

        await grain.CompleteAsync();
        var cancelCompleted = () => grain.CancelAsync();
        await cancelCompleted.Should().ThrowAsync<InvalidOperationException>();
        var editMinutes = () => grain.SetNotesAsync("changed later");
        await editMinutes.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Start_Snapshots_Unit_Shares()
    {
        var buildingId = await BuildingWithUnits();
        var (grain, started) = await StartedAssembly(buildingId);

        // A unit added after the start does not change who may vote.
        await GrainFactory.GetGrain<IBuildingGrain>(buildingId).AddOrUpdateUnitAsync(new UnitInfo
        {
            Id = Guid.NewGuid(), Number = "4", Floor = 2, AreaM2 = 50, OwnershipShare = 10m,
            Type = UnitType.Apartment, Status = UnitStatus.OccupiedByOwner
        });

        var later = await grain.GetAsync();
        later.EligibleUnits.Select(u => u.UnitNumber).Should().BeEquivalentTo("1", "2", "3");
        later.EligibleVotes.Should().Be(started.EligibleVotes);
    }

    [Fact]
    public async Task An_Assembly_Is_Only_Reachable_Through_Its_Own_Building()
    {
        var buildingA = await BuildingWithUnits();
        var buildingB = await BuildingWithUnits();
        var id = Guid.NewGuid();
        await Assembly(buildingA, id).ScheduleAsync(Request());

        var throughOtherBuilding = () => Assembly(buildingB, id).GetAsync();

        await throughOtherBuilding.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Cancelled_Assembly_Accepts_No_Further_Changes()
    {
        var grain = Assembly(await BuildingWithUnits());
        await grain.ScheduleAsync(Request());
        await grain.CancelAsync();

        var notice = () => grain.MarkNoticeSentAsync(physicallyPosted: true);

        await notice.Should().ThrowAsync<InvalidOperationException>();
        (await grain.GetSummaryAsync()).Status.Should().Be(AssemblyStatus.Cancelled);
    }
}
