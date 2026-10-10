using BuildingManager.Core.Enums;
using System.ComponentModel.DataAnnotations;
using Orleans;

namespace BuildingManager.Orleans.Interfaces;

/// <summary>
/// One owners' assembly, keyed "{buildingId}|{assemblyId}". Having the
/// building in the key means an assembly can only be reached through the
/// building it belongs to (the API checks access to that building).
///
/// Lifecycle: Scheduled -> NoticeSent -> InProgress -> Completed, or
/// Cancelled from any state before Completed. Voting shares are the units'
/// ownership shares, snapshotted when the meeting starts.
///
/// Rule violations throw InvalidOperationException (wrong state, no quorum),
/// invalid input throws ArgumentException, unknown assembly/item/unit throws
/// KeyNotFoundException.
/// </summary>
public interface IAssemblyGrain : IGrainWithStringKey
{
    /// <summary>
    /// Schedules the assembly. The date must be in the future; under three
    /// days' notice requires AllowShortNotice. A repeated session must
    /// reference a completed first session of the same building that did
    /// not reach quorum.
    /// </summary>
    Task<AssemblyDetails> ScheduleAsync(ScheduleAssemblyRequest request);

    Task<AssemblyDetails> GetAsync();
    Task<AssemblySummary> GetSummaryAsync();

    /// <summary>Agenda changes are only possible before notice is sent.</summary>
    Task<AgendaItemInfo> AddAgendaItemAsync(AgendaItemDraft item);
    Task RemoveAgendaItemAsync(Guid itemId);

    /// <summary>Records that notice was sent; call again with physicallyPosted to confirm posting later.</summary>
    Task MarkNoticeSentAsync(bool physicallyPosted);

    /// <summary>
    /// Opens the meeting and snapshots the units' voting shares. Requires notice
    /// to have been sent and is refused more than an hour before the announced time.
    /// </summary>
    Task StartAsync();

    /// <summary>Records or corrects a unit's attendance (in person, proxy, written or electronic).</summary>
    Task RecordAttendanceAsync(AttendanceDraft attendance);

    /// <summary>Removes a unit's attendance and its votes on open items; refused if it voted on a finalized item.</summary>
    Task RemoveAttendanceAsync(string unitNumber);

    /// <summary>Records or replaces a present unit's vote. Requires quorum.</summary>
    Task RecordVoteAsync(Guid itemId, string unitNumber, VoteChoice choice);

    /// <summary>Closes voting on an item and decides it by its voting type.</summary>
    Task<AgendaItemInfo> FinalizeAgendaItemAsync(Guid itemId);

    Task SetNotesAsync(string? notes);

    /// <summary>
    /// Ends the meeting. With quorum, every item that requires a vote must
    /// be finalized first; without quorum no decisions are made and a first
    /// session can be followed by a repeated one.
    /// </summary>
    Task CompleteAsync();

    Task CancelAsync();
}

/// <summary>Per-building list of assembly ids (key = building id).</summary>
public interface IAssemblyRegistryGrain : IGrainWithStringKey
{
    Task RegisterAsync(Guid assemblyId);
    Task<List<Guid>> GetAssemblyIdsAsync();
}

[GenerateSerializer]
public record ScheduleAssemblyRequest
{
    /// <summary>Meeting date and time (UTC).</summary>
    [Id(0)] public DateTime Date { get; init; }

    [Id(1)][Required, MaxLength(200)]
    public string Location { get; init; } = string.Empty;

    [Id(2)] public SessionType SessionType { get; init; } = SessionType.First;

    [Id(3)][Required, MinLength(1)]
    public List<AgendaItemDraft> Agenda { get; init; } = new();

    /// <summary>The first session a repeated session follows. Set by the API, not the client.</summary>
    [Id(4)] public Guid? PreviousAssemblyId { get; init; }

    /// <summary>Confirms scheduling with less than the minimum three days' notice.</summary>
    [Id(5)] public bool AllowShortNotice { get; init; }

    /// <summary>Email of the manager who scheduled it. Set by the API, not the client.</summary>
    [Id(6)] public string? CreatedBy { get; init; }
}

[GenerateSerializer]
public record AgendaItemDraft
{
    [Id(0)][Required, MaxLength(300)]
    public string Title { get; init; } = string.Empty;

    [Id(1)][MaxLength(2000)]
    public string? Description { get; init; }

    [Id(2)] public VotingType VotingType { get; init; } = VotingType.Simple;

    /// <summary>False for informational items that are not voted on.</summary>
    [Id(3)] public bool RequiresVote { get; init; } = true;
}

[GenerateSerializer]
public record AttendanceDraft
{
    [Id(0)][Required, MaxLength(20)]
    public string UnitNumber { get; init; } = string.Empty;

    [Id(1)] public AttendanceType Type { get; init; } = AttendanceType.InPerson;

    /// <summary>The owner (or representative) present for the unit.</summary>
    [Id(2)][MaxLength(200)]
    public string? AttendeeName { get; init; }

    /// <summary>Required for proxy attendance.</summary>
    [Id(3)][MaxLength(200)]
    public string? ProxyHolderName { get; init; }
}

[GenerateSerializer]
public record AssemblySummary
{
    [Id(0)] public Guid Id { get; init; }
    [Id(1)] public DateTime Date { get; init; }
    [Id(2)] public string Location { get; init; } = string.Empty;
    [Id(3)] public SessionType SessionType { get; init; }
    [Id(4)] public AssemblyStatus Status { get; init; }
    [Id(5)] public int AgendaItemCount { get; init; }
    /// <summary>Null until the meeting has started.</summary>
    [Id(6)] public bool? QuorumAchieved { get; init; }
}

[GenerateSerializer]
public record AssemblyDetails
{
    [Id(0)] public Guid Id { get; init; }
    [Id(1)] public string BuildingId { get; init; } = string.Empty;
    [Id(2)] public DateTime Date { get; init; }
    [Id(3)] public string Location { get; init; } = string.Empty;
    [Id(4)] public SessionType SessionType { get; init; }
    [Id(5)] public AssemblyStatus Status { get; init; }
    [Id(6)] public DateTime ScheduledAt { get; init; }
    [Id(7)] public string? CreatedBy { get; init; }
    [Id(8)] public Guid? PreviousAssemblyId { get; init; }
    /// <summary>Scheduled with less than three days' notice (manager override).</summary>
    [Id(9)] public bool HasShortNotice { get; init; }
    [Id(10)] public DateTime? NoticeSentAt { get; init; }
    [Id(11)] public DateTime? PhysicallyPostedAt { get; init; }
    [Id(12)] public DateTime? StartedAt { get; init; }
    [Id(13)] public DateTime? CompletedAt { get; init; }
    [Id(14)] public DateTime? CancelledAt { get; init; }
    [Id(15)] public string? Notes { get; init; }

    /// <summary>Sum of all units' shares, snapshotted at start (0 before).</summary>
    [Id(16)] public decimal EligibleVotes { get; init; }
    [Id(17)] public decimal PresentVotes { get; init; }
    [Id(18)] public decimal QuorumPercentage { get; init; }
    /// <summary>Null until the meeting has started.</summary>
    [Id(19)] public bool? QuorumAchieved { get; init; }

    [Id(20)] public List<AgendaItemInfo> Agenda { get; init; } = new();
    [Id(21)] public List<AttendanceInfo> Attendance { get; init; } = new();
    /// <summary>Units entitled to vote and their shares, snapshotted at start.</summary>
    [Id(22)] public List<EligibleUnit> EligibleUnits { get; init; } = new();
}

[GenerateSerializer]
public record AgendaItemInfo
{
    [Id(0)] public Guid Id { get; init; }
    [Id(1)] public int Order { get; init; }
    [Id(2)] public string Title { get; init; } = string.Empty;
    [Id(3)] public string? Description { get; init; }
    [Id(4)] public VotingType VotingType { get; init; }
    [Id(5)] public bool RequiresVote { get; init; }
    [Id(6)] public VoteResult Result { get; init; } = VoteResult.Pending;
    [Id(7)] public bool IsFinalized { get; init; }
    /// <summary>Tallies by voting share; live until finalized, then fixed.</summary>
    [Id(8)] public decimal VotesFor { get; init; }
    [Id(9)] public decimal VotesAgainst { get; init; }
    [Id(10)] public decimal VotesAbstain { get; init; }
    [Id(11)] public List<VoteInfo> Votes { get; init; } = new();
}

[GenerateSerializer]
public record VoteInfo
{
    [Id(0)] public string UnitNumber { get; init; } = string.Empty;
    [Id(1)] public VoteChoice Choice { get; init; }
    [Id(2)] public decimal VotingShare { get; init; }
}

[GenerateSerializer]
public record AttendanceInfo
{
    [Id(0)] public string UnitNumber { get; init; } = string.Empty;
    [Id(1)] public AttendanceType Type { get; init; }
    [Id(2)] public string? AttendeeName { get; init; }
    [Id(3)] public string? ProxyHolderName { get; init; }
    [Id(4)] public decimal VotingShare { get; init; }
    [Id(5)] public DateTime RecordedAt { get; init; }
}

[GenerateSerializer]
public record EligibleUnit
{
    [Id(0)] public string UnitNumber { get; init; } = string.Empty;
    [Id(1)] public decimal VotingShare { get; init; }
}
