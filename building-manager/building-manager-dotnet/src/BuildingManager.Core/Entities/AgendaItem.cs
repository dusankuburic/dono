using BuildingManager.Core.Enums;

namespace BuildingManager.Core.Entities;

public class AgendaItem : BaseEntity
{
    public Guid AssemblyId { get; private set; }
    public int Order { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool RequiresVote { get; private set; }
    public VotingType VotingType { get; private set; }
    public VoteResult Result { get; private set; } = VoteResult.Pending;
    public decimal? VotesFor { get; private set; }
    public decimal? VotesAgainst { get; private set; }
    public decimal? VotesAbstain { get; private set; }
    public bool IsFinalized { get; private set; }
    
    private readonly List<Vote> _votes = new();
    public IReadOnlyCollection<Vote> Votes => _votes.AsReadOnly();

    private AgendaItem() { }

    internal static AgendaItem Create(Guid assemblyId, string title, int order, VotingType votingType, bool requiresVote = true)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required", nameof(title));

        return new AgendaItem
        {
            AssemblyId = assemblyId,
            Title = title,
            Order = order,
            VotingType = votingType,
            RequiresVote = requiresVote
        };
    }

    public void SetDescription(string? description)
    {
        Description = description;
        MarkAsUpdated();
    }

    public void RecordVote(Guid residentId, VoteChoice choice, decimal votingShare)
    {
        if (IsFinalized)
            throw new InvalidOperationException("Cannot record votes on finalized agenda item");

        var existingVote = _votes.FirstOrDefault(v => v.ResidentId == residentId);
        if (existingVote != null)
        {
            _votes.Remove(existingVote);
        }

        var vote = Vote.Create(Id, residentId, choice, votingShare);
        _votes.Add(vote);
        MarkAsUpdated();
    }

    public void FinalizeVote()
    {
        if (!RequiresVote)
        {
            Result = VoteResult.Passed;
            IsFinalized = true;
            MarkAsUpdated();
            return;
        }

        VotesFor = _votes.Where(v => v.Choice == VoteChoice.For).Sum(v => v.VotingShare);
        VotesAgainst = _votes.Where(v => v.Choice == VoteChoice.Against).Sum(v => v.VotingShare);
        VotesAbstain = _votes.Where(v => v.Choice == VoteChoice.Abstain).Sum(v => v.VotingShare);

        var totalVotes = VotesFor.Value + VotesAgainst.Value + VotesAbstain.Value;

        // Compare with exact fractions instead of rounded percentages so that
        // an exact two-thirds result (66.666...%) is not rejected.
        Result = VotingType switch
        {
            VotingType.Unanimous when totalVotes > 0
                && VotesAgainst.Value == 0 && VotesAbstain.Value == 0 => VoteResult.Passed,
            VotingType.TwoThirds when totalVotes > 0
                && VotesFor.Value * 3 >= totalVotes * 2 => VoteResult.Passed,
            VotingType.Simple when totalVotes > 0
                && VotesFor.Value * 2 > totalVotes => VoteResult.Passed,
            _ => VoteResult.Failed
        };

        IsFinalized = true;
        MarkAsUpdated();
    }

    public void UpdateOrder(int newOrder)
    {
        Order = newOrder;
        MarkAsUpdated();
    }
}
