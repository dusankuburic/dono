using BuildingManager.Core.Enums;
using BuildingManager.Core.Rules;
using BuildingManager.Orleans.Interfaces;
using Orleans;
using Orleans.Runtime;

namespace BuildingManager.Orleans.Grains;

public class AssemblyGrain : Grain, IAssemblyGrain
{
    private const int MaxNotesLength = 10_000;

    private readonly IPersistentState<AssemblyState> _state;

    public AssemblyGrain(
        [PersistentState("assembly", "buildingStorage")]
        IPersistentState<AssemblyState> state)
    {
        _state = state;
    }

    private AssemblyState S => _state.State;

    private bool IsScheduled => S.Status != 0;

    private (string BuildingId, Guid AssemblyId) Key
    {
        get
        {
            var key = this.GetPrimaryKeyString();
            var separator = key.LastIndexOf('|');
            if (separator <= 0 || !Guid.TryParse(key[(separator + 1)..], out var assemblyId))
                throw new InvalidOperationException($"Assembly grain key '{key}' is not '{{buildingId}}|{{assemblyId}}'.");
            return (key[..separator], assemblyId);
        }
    }

    public async Task<AssemblyDetails> ScheduleAsync(ScheduleAssemblyRequest request)
    {
        if (IsScheduled)
            throw new InvalidOperationException("This assembly is already scheduled.");

        if (string.IsNullOrWhiteSpace(request.Location))
            throw new ArgumentException("Location is required.", nameof(request));

        var now = DateTime.UtcNow;

        var date = request.Date.Kind switch
        {
            DateTimeKind.Local => request.Date.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(request.Date, DateTimeKind.Utc),
            _ => request.Date
        };
        if (date <= now)
            throw new ArgumentException("The assembly date must be in the future.", nameof(request));

        if (request.Agenda is not { Count: > 0 })
            throw new ArgumentException("An assembly needs at least one agenda item.", nameof(request));
        foreach (var item in request.Agenda)
            ValidateDraft(item);

        var shortNotice = date - now < AssemblyRules.MinimumNotice;
        if (shortNotice && !request.AllowShortNotice)
            throw new InvalidOperationException(
                "Owners must get at least 3 days' notice. Confirm short notice to schedule anyway.");

        var (buildingId, assemblyId) = Key;
        if (request.SessionType == SessionType.Repeated)
        {
            if (request.PreviousAssemblyId is not { } previousId)
                throw new ArgumentException("A repeated session must reference the first session.", nameof(request));

            var previous = await GrainFactory.GetGrain<IAssemblyGrain>($"{buildingId}|{previousId}").GetAsync();
            if (previous.SessionType != SessionType.First
                || previous.Status != AssemblyStatus.Completed
                || previous.QuorumAchieved != false)
            {
                throw new InvalidOperationException(
                    "A repeated session can only follow a completed first session that did not reach quorum.");
            }
        }
        else if (request.PreviousAssemblyId is not null)
        {
            throw new ArgumentException("Only a repeated session references a previous assembly.", nameof(request));
        }

        await GrainFactory.GetGrain<IAssemblyRegistryGrain>(buildingId).RegisterAsync(assemblyId);

        S.Date = date;
        S.Location = request.Location.Trim();
        S.SessionType = request.SessionType;
        S.PreviousAssemblyId = request.PreviousAssemblyId;
        S.CreatedBy = request.CreatedBy;
        S.ScheduledAt = now;
        S.HasShortNotice = shortNotice;
        S.Agenda = request.Agenda.Select((draft, index) => NewItem(draft, index + 1)).ToList();
        S.Status = AssemblyStatus.Scheduled;
        await _state.WriteStateAsync();

        return ToDetails();
    }

    public Task<AssemblyDetails> GetAsync()
    {
        EnsureScheduled();
        return Task.FromResult(ToDetails());
    }

    public Task<AssemblySummary> GetSummaryAsync()
    {
        EnsureScheduled();
        return Task.FromResult(new AssemblySummary
        {
            Id = Key.AssemblyId,
            Date = S.Date,
            Location = S.Location,
            SessionType = S.SessionType,
            Status = S.Status,
            AgendaItemCount = S.Agenda.Count,
            QuorumAchieved = QuorumAchieved()
        });
    }

    public async Task<AgendaItemInfo> AddAgendaItemAsync(AgendaItemDraft draft)
    {
        EnsureStatus("The agenda can only be changed before notice is sent.", AssemblyStatus.Scheduled);
        ValidateDraft(draft);

        var item = NewItem(draft, S.Agenda.Count == 0 ? 1 : S.Agenda.Max(i => i.Order) + 1);
        S.Agenda.Add(item);
        await _state.WriteStateAsync();
        return ToInfo(item);
    }

    public async Task RemoveAgendaItemAsync(Guid itemId)
    {
        EnsureStatus("The agenda can only be changed before notice is sent.", AssemblyStatus.Scheduled);
        var item = FindItem(itemId);
        if (S.Agenda.Count == 1)
            throw new InvalidOperationException("An assembly needs at least one agenda item.");

        S.Agenda.Remove(item);
        var order = 1;
        foreach (var remaining in S.Agenda.OrderBy(i => i.Order))
            remaining.Order = order++;
        await _state.WriteStateAsync();
    }

    public async Task MarkNoticeSentAsync(bool physicallyPosted)
    {
        EnsureStatus("Notice can only be recorded before the meeting starts.",
            AssemblyStatus.Scheduled, AssemblyStatus.NoticeSent);

        var now = DateTime.UtcNow;
        var changed = false;
        if (S.Status == AssemblyStatus.Scheduled)
        {
            S.Status = AssemblyStatus.NoticeSent;
            S.NoticeSentAt = now;
            changed = true;
        }

        if (physicallyPosted && S.PhysicallyPostedAt is null)
        {
            S.PhysicallyPostedAt = now;
            changed = true;
        }

        if (changed)
            await _state.WriteStateAsync();
    }

    public async Task StartAsync()
    {
        EnsureStatus("The meeting can only start after notice has been sent.", AssemblyStatus.NoticeSent);

        if (DateTime.UtcNow < S.Date - AssemblyRules.EarliestStartBeforeAnnounced)
            throw new InvalidOperationException(
                "The meeting cannot start more than an hour before the time announced to the owners.");

        var units = await GrainFactory.GetGrain<IBuildingGrain>(Key.BuildingId).GetAllUnitsAsync();
        var shares = units
            .Where(u => u.OwnershipShare > 0)
            .ToDictionary(u => u.Number, u => u.OwnershipShare);
        if (shares.Count == 0)
            throw new InvalidOperationException(
                "No unit has an ownership share, so there is nobody to vote. Add units with their shares first.");

        S.EligibleUnitShares = shares;
        S.Status = AssemblyStatus.InProgress;
        S.StartedAt = DateTime.UtcNow;
        await _state.WriteStateAsync();
    }

    public async Task RecordAttendanceAsync(AttendanceDraft attendance)
    {
        EnsureStatus("Attendance can only be recorded while the meeting is in progress.", AssemblyStatus.InProgress);

        var unit = attendance.UnitNumber?.Trim() ?? string.Empty;
        if (!S.EligibleUnitShares.TryGetValue(unit, out var share))
            throw new KeyNotFoundException($"Unit '{unit}' is not entitled to vote at this assembly.");
        if (!Enum.IsDefined(attendance.Type))
            throw new ArgumentException("Unknown attendance type.", nameof(attendance));
        if (attendance.Type == AttendanceType.Proxy && string.IsNullOrWhiteSpace(attendance.ProxyHolderName))
            throw new ArgumentException("A proxy needs the proxy holder's name.", nameof(attendance));

        S.Attendance[unit] = new AttendanceState
        {
            Type = attendance.Type,
            AttendeeName = attendance.AttendeeName?.Trim(),
            ProxyHolderName = attendance.Type == AttendanceType.Proxy ? attendance.ProxyHolderName?.Trim() : null,
            VotingShare = share,
            RecordedAt = DateTime.UtcNow
        };
        await _state.WriteStateAsync();
    }

    public async Task RemoveAttendanceAsync(string unitNumber)
    {
        EnsureStatus("Attendance can only be changed while the meeting is in progress.", AssemblyStatus.InProgress);

        var unit = unitNumber?.Trim() ?? string.Empty;
        if (!S.Attendance.ContainsKey(unit))
            throw new KeyNotFoundException($"Unit '{unit}' has no recorded attendance.");
        if (S.Agenda.Any(i => i.IsFinalized && i.Votes.ContainsKey(unit)))
            throw new InvalidOperationException(
                $"Unit '{unit}' voted on an item that is already decided, so its attendance must stay on record.");

        foreach (var item in S.Agenda)
            item.Votes.Remove(unit);
        S.Attendance.Remove(unit);
        await _state.WriteStateAsync();
    }

    public async Task RecordVoteAsync(Guid itemId, string unitNumber, VoteChoice choice)
    {
        EnsureStatus("Votes can only be recorded while the meeting is in progress.", AssemblyStatus.InProgress);

        var item = FindItem(itemId);
        if (!item.RequiresVote)
            throw new InvalidOperationException("This agenda item is informational and is not voted on.");
        if (item.IsFinalized)
            throw new InvalidOperationException("Voting on this item is closed.");
        if (!Enum.IsDefined(choice))
            throw new ArgumentException("Unknown vote choice.", nameof(choice));

        var unit = unitNumber?.Trim() ?? string.Empty;
        if (!S.Attendance.TryGetValue(unit, out var attendance))
            throw new InvalidOperationException($"Unit '{unit}' has no recorded attendance and cannot vote.");
        EnsureQuorum();

        item.Votes[unit] = new VoteState { Choice = choice, VotingShare = attendance.VotingShare };
        await _state.WriteStateAsync();
    }

    public async Task<AgendaItemInfo> FinalizeAgendaItemAsync(Guid itemId)
    {
        EnsureStatus("Items can only be decided while the meeting is in progress.", AssemblyStatus.InProgress);

        var item = FindItem(itemId);
        if (item.IsFinalized)
            throw new InvalidOperationException("This item is already decided.");

        if (item.RequiresVote)
        {
            EnsureQuorum();
            var (votesFor, votesAgainst, votesAbstain) = Tally(item);
            item.VotesFor = votesFor;
            item.VotesAgainst = votesAgainst;
            item.VotesAbstain = votesAbstain;
            item.Result = AssemblyRules.IsPassed(item.VotingType, votesFor, votesAgainst, votesAbstain)
                ? VoteResult.Passed
                : VoteResult.Failed;
        }
        else
        {

            item.Result = VoteResult.Passed;
        }

        item.IsFinalized = true;
        await _state.WriteStateAsync();
        return ToInfo(item);
    }

    public async Task SetNotesAsync(string? notes)
    {
        EnsureStatus("The minutes are final once the assembly is completed or cancelled.",
            AssemblyStatus.Scheduled, AssemblyStatus.NoticeSent, AssemblyStatus.InProgress);
        if (notes is { Length: > MaxNotesLength })
            throw new ArgumentException($"Notes can be at most {MaxNotesLength} characters.", nameof(notes));

        S.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        await _state.WriteStateAsync();
    }

    public async Task CompleteAsync()
    {
        EnsureStatus("Only a meeting in progress can be completed.", AssemblyStatus.InProgress);

        var (present, eligible) = Votes();
        var quorum = AssemblyRules.IsQuorumAchieved(S.SessionType, present, eligible);
        if (quorum)
        {
            var undecided = S.Agenda.Where(i => i.RequiresVote && !i.IsFinalized).OrderBy(i => i.Order).ToList();
            if (undecided.Count > 0)
                throw new InvalidOperationException(
                    "Decide every agenda item before completing: " +
                    string.Join(", ", undecided.Select(i => $"#{i.Order} {i.Title}")) + ".");

            foreach (var item in S.Agenda.Where(i => !i.IsFinalized))
            {
                item.Result = VoteResult.Passed;
                item.IsFinalized = true;
            }
        }

        S.FinalQuorumAchieved = quorum;
        S.FinalQuorumPercentage = AssemblyRules.QuorumPercentage(present, eligible);
        S.Status = AssemblyStatus.Completed;
        S.CompletedAt = DateTime.UtcNow;
        await _state.WriteStateAsync();
    }

    public async Task CancelAsync()
    {
        EnsureStatus("A completed or cancelled assembly cannot be cancelled.",
            AssemblyStatus.Scheduled, AssemblyStatus.NoticeSent, AssemblyStatus.InProgress);

        S.Status = AssemblyStatus.Cancelled;
        S.CancelledAt = DateTime.UtcNow;
        await _state.WriteStateAsync();
    }

    private void EnsureScheduled()
    {
        if (!IsScheduled)
            throw new KeyNotFoundException($"Assembly '{this.GetPrimaryKeyString()}' not found.");
    }

    private void EnsureStatus(string message, params AssemblyStatus[] allowed)
    {
        EnsureScheduled();
        if (!allowed.Contains(S.Status))
            throw new InvalidOperationException(message);
    }

    private void EnsureQuorum()
    {
        var (present, eligible) = Votes();
        if (!AssemblyRules.IsQuorumAchieved(S.SessionType, present, eligible))
        {
            var needed = S.SessionType == SessionType.First ? "51%" : "one third";
            throw new InvalidOperationException(
                $"Quorum not reached: {AssemblyRules.QuorumPercentage(present, eligible):0.##}% of votes present, {needed} needed.");
        }
    }

    private (decimal Present, decimal Eligible) Votes() =>
        (S.Attendance.Values.Sum(a => a.VotingShare), S.EligibleUnitShares.Values.Sum());

    private bool? QuorumAchieved()
    {
        if (S.FinalQuorumAchieved is { } final)
            return final;
        if (S.Status != AssemblyStatus.InProgress)
            return null;
        var (present, eligible) = Votes();
        return AssemblyRules.IsQuorumAchieved(S.SessionType, present, eligible);
    }

    private AgendaItemState FindItem(Guid itemId) =>
        S.Agenda.FirstOrDefault(i => i.Id == itemId)
        ?? throw new KeyNotFoundException($"Agenda item '{itemId}' not found.");

    private static void ValidateDraft(AgendaItemDraft draft)
    {
        if (string.IsNullOrWhiteSpace(draft.Title))
            throw new ArgumentException("Every agenda item needs a title.", nameof(draft));
        if (!Enum.IsDefined(draft.VotingType))
            throw new ArgumentException("Unknown voting type.", nameof(draft));
    }

    private static AgendaItemState NewItem(AgendaItemDraft draft, int order) => new()
    {
        Id = Guid.NewGuid(),
        Order = order,
        Title = draft.Title.Trim(),
        Description = string.IsNullOrWhiteSpace(draft.Description) ? null : draft.Description.Trim(),
        VotingType = draft.VotingType,
        RequiresVote = draft.RequiresVote,
        Result = VoteResult.Pending
    };

    private static (decimal For, decimal Against, decimal Abstain) Tally(AgendaItemState item) =>
        (item.Votes.Values.Where(v => v.Choice == VoteChoice.For).Sum(v => v.VotingShare),
         item.Votes.Values.Where(v => v.Choice == VoteChoice.Against).Sum(v => v.VotingShare),
         item.Votes.Values.Where(v => v.Choice == VoteChoice.Abstain).Sum(v => v.VotingShare));

    private static AgendaItemInfo ToInfo(AgendaItemState item)
    {

        var (votesFor, votesAgainst, votesAbstain) = item.IsFinalized && item.RequiresVote
            ? (item.VotesFor, item.VotesAgainst, item.VotesAbstain)
            : Tally(item);

        return new AgendaItemInfo
        {
            Id = item.Id,
            Order = item.Order,
            Title = item.Title,
            Description = item.Description,
            VotingType = item.VotingType,
            RequiresVote = item.RequiresVote,
            Result = item.Result,
            IsFinalized = item.IsFinalized,
            VotesFor = votesFor,
            VotesAgainst = votesAgainst,
            VotesAbstain = votesAbstain,
            Votes = item.Votes
                .OrderBy(v => v.Key)
                .Select(v => new VoteInfo { UnitNumber = v.Key, Choice = v.Value.Choice, VotingShare = v.Value.VotingShare })
                .ToList()
        };
    }

    private AssemblyDetails ToDetails()
    {
        var (present, eligible) = Votes();
        var (buildingId, assemblyId) = Key;
        return new AssemblyDetails
        {
            Id = assemblyId,
            BuildingId = buildingId,
            Date = S.Date,
            Location = S.Location,
            SessionType = S.SessionType,
            Status = S.Status,
            ScheduledAt = S.ScheduledAt,
            CreatedBy = S.CreatedBy,
            PreviousAssemblyId = S.PreviousAssemblyId,
            HasShortNotice = S.HasShortNotice,
            NoticeSentAt = S.NoticeSentAt,
            PhysicallyPostedAt = S.PhysicallyPostedAt,
            StartedAt = S.StartedAt,
            CompletedAt = S.CompletedAt,
            CancelledAt = S.CancelledAt,
            Notes = S.Notes,
            EligibleVotes = eligible,
            PresentVotes = present,
            QuorumPercentage = S.FinalQuorumPercentage ?? AssemblyRules.QuorumPercentage(present, eligible),
            QuorumAchieved = QuorumAchieved(),
            Agenda = S.Agenda.OrderBy(i => i.Order).Select(ToInfo).ToList(),
            Attendance = S.Attendance
                .OrderBy(a => a.Key)
                .Select(a => new AttendanceInfo
                {
                    UnitNumber = a.Key,
                    Type = a.Value.Type,
                    AttendeeName = a.Value.AttendeeName,
                    ProxyHolderName = a.Value.ProxyHolderName,
                    VotingShare = a.Value.VotingShare,
                    RecordedAt = a.Value.RecordedAt
                })
                .ToList(),
            EligibleUnits = S.EligibleUnitShares
                .OrderBy(u => u.Key)
                .Select(u => new EligibleUnit { UnitNumber = u.Key, VotingShare = u.Value })
                .ToList()
        };
    }
}

public class AssemblyRegistryGrain : Grain, IAssemblyRegistryGrain
{
    private readonly IPersistentState<AssemblyRegistryState> _state;

    public AssemblyRegistryGrain(
        [PersistentState("assemblyRegistry", "buildingStorage")]
        IPersistentState<AssemblyRegistryState> state)
    {
        _state = state;
    }

    public async Task RegisterAsync(Guid assemblyId)
    {
        if (_state.State.AssemblyIds.Add(assemblyId))
        {
            await _state.WriteStateAsync();
        }
    }

    public Task<List<Guid>> GetAssemblyIdsAsync() =>
        Task.FromResult(_state.State.AssemblyIds.ToList());
}

[GenerateSerializer]
public class AssemblyRegistryState
{
    [Id(0)] public HashSet<Guid> AssemblyIds { get; set; } = new();
}

[GenerateSerializer]
public class AssemblyState
{
    [Id(0)] public AssemblyStatus Status { get; set; }
    [Id(1)] public DateTime Date { get; set; }
    [Id(2)] public string Location { get; set; } = string.Empty;
    [Id(3)] public SessionType SessionType { get; set; } = SessionType.First;
    [Id(4)] public Guid? PreviousAssemblyId { get; set; }
    [Id(5)] public string? CreatedBy { get; set; }
    [Id(6)] public DateTime ScheduledAt { get; set; }
    [Id(7)] public bool HasShortNotice { get; set; }
    [Id(8)] public DateTime? NoticeSentAt { get; set; }
    [Id(9)] public DateTime? PhysicallyPostedAt { get; set; }
    [Id(10)] public DateTime? StartedAt { get; set; }
    [Id(11)] public DateTime? CompletedAt { get; set; }
    [Id(12)] public DateTime? CancelledAt { get; set; }
    [Id(13)] public string? Notes { get; set; }
    [Id(14)] public List<AgendaItemState> Agenda { get; set; } = new();
        [Id(15)] public Dictionary<string, AttendanceState> Attendance { get; set; } = new();
        [Id(16)] public Dictionary<string, decimal> EligibleUnitShares { get; set; } = new();
    [Id(17)] public bool? FinalQuorumAchieved { get; set; }
    [Id(18)] public decimal? FinalQuorumPercentage { get; set; }
}

[GenerateSerializer]
public class AgendaItemState
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public int Order { get; set; }
    [Id(2)] public string Title { get; set; } = string.Empty;
    [Id(3)] public string? Description { get; set; }
    [Id(4)] public VotingType VotingType { get; set; }
    [Id(5)] public bool RequiresVote { get; set; }
    [Id(6)] public VoteResult Result { get; set; } = VoteResult.Pending;
    [Id(7)] public bool IsFinalized { get; set; }
    [Id(8)] public decimal VotesFor { get; set; }
    [Id(9)] public decimal VotesAgainst { get; set; }
    [Id(10)] public decimal VotesAbstain { get; set; }
        [Id(11)] public Dictionary<string, VoteState> Votes { get; set; } = new();
}

[GenerateSerializer]
public class AttendanceState
{
    [Id(0)] public AttendanceType Type { get; set; }
    [Id(1)] public string? AttendeeName { get; set; }
    [Id(2)] public string? ProxyHolderName { get; set; }
    [Id(3)] public decimal VotingShare { get; set; }
    [Id(4)] public DateTime RecordedAt { get; set; }
}

[GenerateSerializer]
public class VoteState
{
    [Id(0)] public VoteChoice Choice { get; set; }
    [Id(1)] public decimal VotingShare { get; set; }
}
