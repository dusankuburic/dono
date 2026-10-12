using BuildingManager.Core.Enums;
using MudBlazor;

namespace BuildingManager.Blazor.Services;

public static class AssemblyLabels
{
    public static string Status(AssemblyStatus status) => status switch
    {
        AssemblyStatus.Scheduled => "Scheduled",
        AssemblyStatus.NoticeSent => "Notice sent",
        AssemblyStatus.InProgress => "In progress",
        AssemblyStatus.Completed => "Completed",
        AssemblyStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    public static Color StatusColor(AssemblyStatus status) => status switch
    {
        AssemblyStatus.Scheduled => Color.Default,
        AssemblyStatus.NoticeSent => Color.Info,
        AssemblyStatus.InProgress => Color.Warning,
        AssemblyStatus.Completed => Color.Success,
        AssemblyStatus.Cancelled => Color.Error,
        _ => Color.Default
    };

    public static string Session(SessionType type) => type == SessionType.Repeated ? "Repeated session" : "First session";

    public static string VotingType(VotingType type) => type switch
    {
        Core.Enums.VotingType.Simple => "Simple majority",
        Core.Enums.VotingType.TwoThirds => "Two-thirds majority",
        Core.Enums.VotingType.Unanimous => "Unanimous",
        _ => type.ToString()
    };

    public static string Attendance(AttendanceType type) => type switch
    {
        AttendanceType.InPerson => "In person",
        AttendanceType.Proxy => "By proxy",
        AttendanceType.Written => "Written vote",
        AttendanceType.Electronic => "Electronic",
        _ => type.ToString()
    };

    public static string Vote(VoteChoice choice) => choice switch
    {
        VoteChoice.For => "For",
        VoteChoice.Against => "Against",
        VoteChoice.Abstain => "Abstain",
        _ => choice.ToString()
    };

        public static string Result(VoteResult result, bool requiresVote) => (result, requiresVote) switch
    {
        (VoteResult.Pending, _) => "Pending",
        (_, false) => "Noted",
        (VoteResult.Passed, _) => "Adopted",
        (VoteResult.Failed, _) => "Rejected",
        _ => result.ToString()
    };

    public static Color ResultColor(VoteResult result, bool requiresVote) => (result, requiresVote) switch
    {
        (VoteResult.Pending, _) => Color.Default,
        (_, false) => Color.Info,
        (VoteResult.Passed, _) => Color.Success,
        _ => Color.Error
    };

    public static string QuorumRule(SessionType type) =>
        type == SessionType.Repeated ? "one third of all votes" : "51% of all votes";
}
