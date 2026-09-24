using BuildingManager.Core.Entities;
using BuildingManager.Core.Enums;
using BuildingManager.Core.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BuildingManager.Tests.Domain;

public class AgendaItemVoteCountingTests
{
    private static (Assembly assembly, AgendaItem item) CreateAssemblyWithItem(VotingType votingType)
    {
        var assembly = Assembly.Create(Guid.NewGuid(), DateTime.UtcNow.AddDays(7), "Lobby", SessionType.First);
        assembly.AddAgendaItem("Decision", votingType);
        return (assembly, assembly.AgendaItems.Single());
    }

    private static void CastVotes(AgendaItem item, decimal forShare, decimal againstShare, decimal abstainShare)
    {
        if (forShare > 0)
            item.RecordVote(Guid.NewGuid(), VoteChoice.For, forShare);
        if (againstShare > 0)
            item.RecordVote(Guid.NewGuid(), VoteChoice.Against, againstShare);
        if (abstainShare > 0)
            item.RecordVote(Guid.NewGuid(), VoteChoice.Abstain, abstainShare);
    }

    [Fact]
    public void Simple_Majority_Passes_When_More_Than_Half_Of_Present_Votes_Are_For()
    {
        var (_, item) = CreateAssemblyWithItem(VotingType.Simple);
        CastVotes(item, forShare: 55, againstShare: 30, abstainShare: 15);

        item.FinalizeVote();

        item.Result.Should().Be(VoteResult.Passed);
        item.IsFinalized.Should().BeTrue();
    }

    [Fact]
    public void Simple_Majority_Fails_At_Exactly_Half()
    {
        var (_, item) = CreateAssemblyWithItem(VotingType.Simple);
        CastVotes(item, forShare: 50, againstShare: 30, abstainShare: 20);

        item.FinalizeVote();

        item.Result.Should().Be(VoteResult.Failed);
    }

    [Fact]
    public void Simple_Majority_Fails_When_Abstentions_Prevent_Majority()
    {
        // 40 for / 35 against / 25 abstain: only 40% of the present voting
        // share supports the decision, which is not a majority.
        var (_, item) = CreateAssemblyWithItem(VotingType.Simple);
        CastVotes(item, forShare: 40, againstShare: 35, abstainShare: 25);

        item.FinalizeVote();

        item.Result.Should().Be(VoteResult.Failed);
    }

    [Fact]
    public void TwoThirds_Passes_At_Exactly_TwoThirds()
    {
        // 66.666...% voted for: must pass. This is the regression test for the
        // old >= 66.67 percentage comparison which rejected an exact two-thirds vote.
        var (_, item) = CreateAssemblyWithItem(VotingType.TwoThirds);
        CastVotes(item, forShare: 20, againstShare: 7, abstainShare: 3);

        item.FinalizeVote();

        item.Result.Should().Be(VoteResult.Passed);
    }

    [Fact]
    public void TwoThirds_Fails_Below_TwoThirds()
    {
        var (_, item) = CreateAssemblyWithItem(VotingType.TwoThirds);
        CastVotes(item, forShare: 60, againstShare: 30, abstainShare: 10);

        item.FinalizeVote();

        item.Result.Should().Be(VoteResult.Failed);
    }

    [Fact]
    public void Unanimous_Passes_Only_When_Everyone_Votes_For()
    {
        var (_, item) = CreateAssemblyWithItem(VotingType.Unanimous);
        CastVotes(item, forShare: 60, againstShare: 0, abstainShare: 0);

        item.FinalizeVote();

        item.Result.Should().Be(VoteResult.Passed);
    }

    [Fact]
    public void Unanimous_Fails_When_Anyone_Abstains()
    {
        var (_, item) = CreateAssemblyWithItem(VotingType.Unanimous);
        CastVotes(item, forShare: 60, againstShare: 0, abstainShare: 5);

        item.FinalizeVote();

        item.Result.Should().Be(VoteResult.Failed);
    }

    [Fact]
    public void Finalized_Item_Rejects_New_Votes()
    {
        var (_, item) = CreateAssemblyWithItem(VotingType.Simple);
        CastVotes(item, forShare: 100, againstShare: 0, abstainShare: 0);
        item.FinalizeVote();

        var act = () => item.RecordVote(Guid.NewGuid(), VoteChoice.For, 10);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Recasting_Vote_Replaces_Previous_Vote()
    {
        var (_, item) = CreateAssemblyWithItem(VotingType.Simple);
        var residentId = Guid.NewGuid();
        item.RecordVote(residentId, VoteChoice.Against, 30);
        item.RecordVote(residentId, VoteChoice.For, 30);

        item.FinalizeVote();

        item.Votes.Should().ContainSingle(v => v.ResidentId == residentId);
        item.VotesFor.Should().Be(30);
        item.VotesAgainst.Should().Be(0);
    }

    [Fact]
    public void Item_Without_Voting_Requirement_Passes_On_Finalize()
    {
        var assembly = Assembly.Create(Guid.NewGuid(), DateTime.UtcNow.AddDays(7), "Lobby", SessionType.First);
        assembly.AddAgendaItem("Informational item", VotingType.Simple, requiresVote: false);
        var item = assembly.AgendaItems.Single();

        item.FinalizeVote();

        item.Result.Should().Be(VoteResult.Passed);
    }
}

public class AssemblyQuorumTests
{
    [Fact]
    public void First_Session_Requires_51_Percent()
    {
        var assembly = Assembly.Create(Guid.NewGuid(), DateTime.UtcNow, "Lobby", SessionType.First);

        assembly.RecordAttendance(Guid.NewGuid(), AttendanceType.InPerson, 51);
        assembly.CheckQuorum(totalEligibleVotes: 100);

        assembly.QuorumAchieved.Should().BeTrue();
        assembly.QuorumPercentage.Should().Be(51m);
    }

    [Fact]
    public void First_Session_Fails_Below_51_Percent()
    {
        var assembly = Assembly.Create(Guid.NewGuid(), DateTime.UtcNow, "Lobby", SessionType.First);

        assembly.RecordAttendance(Guid.NewGuid(), AttendanceType.InPerson, 50.9m);
        assembly.CheckQuorum(100);

        assembly.QuorumAchieved.Should().BeFalse();
    }

    [Fact]
    public void Repeated_Session_Accepts_Exactly_One_Third()
    {
        // 30 of 90 eligible votes is exactly one third and must count as
        // quorum (regression test for percentage-rounding comparisons).
        var assembly = Assembly.Create(Guid.NewGuid(), DateTime.UtcNow, "Lobby", SessionType.Repeated);

        assembly.RecordAttendance(Guid.NewGuid(), AttendanceType.Proxy, 30);
        assembly.CheckQuorum(90);

        assembly.QuorumAchieved.Should().BeTrue();
    }

    [Fact]
    public void Repeated_Session_Fails_Below_One_Third()
    {
        var assembly = Assembly.Create(Guid.NewGuid(), DateTime.UtcNow, "Lobby", SessionType.Repeated);

        assembly.RecordAttendance(Guid.NewGuid(), AttendanceType.InPerson, 29.9m);
        assembly.CheckQuorum(90);

        assembly.QuorumAchieved.Should().BeFalse();
    }

    [Fact]
    public void Duplicate_Attendance_For_Same_Resident_Is_Rejected()
    {
        var assembly = Assembly.Create(Guid.NewGuid(), DateTime.UtcNow, "Lobby", SessionType.First);
        var residentId = Guid.NewGuid();
        assembly.RecordAttendance(residentId, AttendanceType.InPerson, 10);

        var act = () => assembly.RecordAttendance(residentId, AttendanceType.Proxy, 10);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Proxy_Votes_Count_Toward_Quorum()
    {
        var assembly = Assembly.Create(Guid.NewGuid(), DateTime.UtcNow, "Lobby", SessionType.First);
        assembly.RecordAttendance(Guid.NewGuid(), AttendanceType.InPerson, 30);
        assembly.RecordAttendance(Guid.NewGuid(), AttendanceType.Proxy, 25, proxyHolderId: Guid.NewGuid());
        assembly.CheckQuorum(100);

        assembly.QuorumAchieved.Should().BeTrue();
        assembly.TotalPresentVotes.Should().Be(55);
    }
}

public class ValueObjectTests
{
    [Fact]
    public void Money_Addition_Requires_Same_Currency()
    {
        var rsd = new Money(100);
        var eur = new Money(100, "EUR");

        var act = () => rsd + eur;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Money_Subtraction_Clamps_At_Zero()
    {
        var result = new Money(30) - new Money(100);

        result.Amount.Should().Be(0);
    }

    [Fact]
    public void Percentage_Cannot_Exceed_100()
    {
        var act = () => new Percentage(100.01m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("Nemanjina", "4", null, "11000", "Beograd", "Nemanjina 4, 11000 Beograd, Serbia")]
    [InlineData("Nemanjina", "4", "12", "11000", "Beograd", "Nemanjina 4/12, 11000 Beograd, Serbia")]
    [InlineData("Nemanjina", "4", null, null, "Beograd", "Nemanjina 4, Beograd, Serbia")]
    public void Address_FullAddress_Handles_Missing_Parts(
        string street, string number, string? apartment, string? postal, string city, string expected)
    {
        var address = new Address(street, number, city, postal, apartment);

        address.FullAddress.Should().Be(expected);
    }
}
