using BuildingManager.Core.Enums;

namespace BuildingManager.Core.Rules;

/// <summary>
/// Legal thresholds for owners' assemblies (Serbian Law on Housing and
/// Building Maintenance). Shares are compared as exact fractions instead of
/// rounded percentages so boundary results (exactly one third, exactly two
/// thirds) are decided correctly.
/// </summary>
public static class AssemblyRules
{
    /// <summary>Minimum notice before an assembly; shorter notice needs an explicit override.</summary>
    public static readonly TimeSpan MinimumNotice = TimeSpan.FromDays(3);

    /// <summary>A meeting may open at most this long before the announced time (registration of attendees).</summary>
    public static readonly TimeSpan EarliestStartBeforeAnnounced = TimeSpan.FromHours(1);

    /// <summary>
    /// First session: at least 51% of all eligible votes present.
    /// Repeated session: at least one third.
    /// </summary>
    public static bool IsQuorumAchieved(SessionType sessionType, decimal presentVotes, decimal eligibleVotes) =>
        eligibleVotes > 0 && sessionType switch
        {
            SessionType.First => presentVotes * 100 >= eligibleVotes * 51m,
            SessionType.Repeated => presentVotes * 3 >= eligibleVotes,
            _ => false
        };

    public static decimal QuorumPercentage(decimal presentVotes, decimal eligibleVotes) =>
        eligibleVotes > 0 ? presentVotes / eligibleVotes * 100 : 0;

    /// <summary>
    /// Whether a decision passes, by the shares of the votes cast:
    /// unanimous needs no votes against and no abstentions, two-thirds needs
    /// at least 2/3 of the votes cast for, simple needs more than half for.
    /// </summary>
    public static bool IsPassed(VotingType votingType, decimal votesFor, decimal votesAgainst, decimal votesAbstain)
    {
        var total = votesFor + votesAgainst + votesAbstain;
        if (total <= 0)
            return false;

        return votingType switch
        {
            VotingType.Unanimous => votesAgainst == 0 && votesAbstain == 0,
            VotingType.TwoThirds => votesFor * 3 >= total * 2,
            VotingType.Simple => votesFor * 2 > total,
            _ => false
        };
    }
}
