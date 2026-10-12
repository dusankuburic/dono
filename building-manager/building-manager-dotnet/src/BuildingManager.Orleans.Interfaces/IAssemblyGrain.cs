using BuildingManager.Core.Enums;
using System.ComponentModel.DataAnnotations;
using Orleans;

namespace BuildingManager.Orleans.Interfaces;

public interface IAssemblyGrain : IGrainWithStringKey
{
        Task<AssemblyDetails> ScheduleAsync(ScheduleAssemblyRequest request);

    Task<AssemblyDetails> GetAsync();
    Task<AssemblySummary> GetSummaryAsync();

        Task<AgendaItemInfo> AddAgendaItemAsync(AgendaItemDraft item);
    Task RemoveAgendaItemAsync(Guid itemId);

        Task MarkNoticeSentAsync(bool physicallyPosted);

        Task StartAsync();

        Task RecordAttendanceAsync(AttendanceDraft attendance);

        Task RemoveAttendanceAsync(string unitNumber);

        Task RecordVoteAsync(Guid itemId, string unitNumber, VoteChoice choice);

        Task<AgendaItemInfo> FinalizeAgendaItemAsync(Guid itemId);

    Task SetNotesAsync(string? notes);

        Task CompleteAsync();

    Task CancelAsync();
}

public interface IAssemblyRegistryGrain : IGrainWithStringKey
{
    Task RegisterAsync(Guid assemblyId);
    Task<List<Guid>> GetAssemblyIdsAsync();
}

[GenerateSerializer]
public record ScheduleAssemblyRequest
{
        [Id(0)] public DateTime Date { get; init; }

    [Id(1)][Required, MaxLength(200)]
    public string Location { get; init; } = string.Empty;

    [Id(2)] public SessionType SessionType { get; init; } = SessionType.First;

    [Id(3)][Required, MinLength(1)]
    public List<AgendaItemDraft> Agenda { get; init; } = new();

        [Id(4)] public Guid? PreviousAssemblyId { get; init; }

        [Id(5)] public bool AllowShortNotice { get; init; }

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

        [Id(3)] public bool RequiresVote { get; init; } = true;
}

[GenerateSerializer]
public record AttendanceDraft
{
    [Id(0)][Required, MaxLength(20)]
    public string UnitNumber { get; init; } = string.Empty;

    [Id(1)] public AttendanceType Type { get; init; } = AttendanceType.InPerson;

        [Id(2)][MaxLength(200)]
    public string? AttendeeName { get; init; }

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
        [Id(9)] public bool HasShortNotice { get; init; }
    [Id(10)] public DateTime? NoticeSentAt { get; init; }
    [Id(11)] public DateTime? PhysicallyPostedAt { get; init; }
    [Id(12)] public DateTime? StartedAt { get; init; }
    [Id(13)] public DateTime? CompletedAt { get; init; }
    [Id(14)] public DateTime? CancelledAt { get; init; }
    [Id(15)] public string? Notes { get; init; }

        [Id(16)] public decimal EligibleVotes { get; init; }
    [Id(17)] public decimal PresentVotes { get; init; }
    [Id(18)] public decimal QuorumPercentage { get; init; }
        [Id(19)] public bool? QuorumAchieved { get; init; }

    [Id(20)] public List<AgendaItemInfo> Agenda { get; init; } = new();
    [Id(21)] public List<AttendanceInfo> Attendance { get; init; } = new();
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
