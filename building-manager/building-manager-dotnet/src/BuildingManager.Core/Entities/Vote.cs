using BuildingManager.Core.Enums;

namespace BuildingManager.Core.Entities;

public class Vote : BaseEntity
{
    public Guid AgendaItemId { get; private set; }
    public Guid ResidentId { get; private set; }
    public VoteChoice Choice { get; private set; }
    public decimal VotingShare { get; private set; }

    private Vote() { }

    internal static Vote Create(Guid agendaItemId, Guid residentId, VoteChoice choice, decimal votingShare)
    {
        return new Vote
        {
            AgendaItemId = agendaItemId,
            ResidentId = residentId,
            Choice = choice,
            VotingShare = votingShare
        };
    }
}
