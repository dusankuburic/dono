using BuildingManager.Core.Enums;

namespace BuildingManager.Core.Rules;

public static class AssemblyRules
{
        public static readonly TimeSpan MinimumNotice = TimeSpan.FromDays(3);

        public static readonly TimeSpan EarliestStartBeforeAnnounced = TimeSpan.FromHours(1);

        public static bool IsQuorumAchieved(SessionType sessionType, decimal presentVotes, decimal eligibleVotes) =>
        eligibleVotes > 0 && sessionType switch
        {
            SessionType.First => presentVotes * 100 >= eligibleVotes * 51m,
            SessionType.Repeated => presentVotes * 3 >= eligibleVotes,
            _ => false
        };

    public static decimal QuorumPercentage(decimal presentVotes, decimal eligibleVotes) =>
        eligibleVotes > 0 ? presentVotes / eligibleVotes * 100 : 0;

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
