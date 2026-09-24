using BuildingManager.Core.Enums;

namespace BuildingManager.Core.Entities;

public class Assembly : BaseEntity
{
    public Guid BuildingId { get; private set; }
    public DateTime Date { get; private set; }
    public string Location { get; private set; } = string.Empty;
    public SessionType SessionType { get; private set; } = SessionType.First;
    public AssemblyStatus Status { get; private set; } = AssemblyStatus.Scheduled;
    public bool QuorumAchieved { get; private set; }
    public decimal QuorumPercentage { get; private set; }
    public DateTime? NoticeSentAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    private readonly List<AgendaItem> _agendaItems = new();
    public IReadOnlyCollection<AgendaItem> AgendaItems => _agendaItems.AsReadOnly();
    
    private readonly List<Attendance> _attendances = new();
    public IReadOnlyCollection<Attendance> Attendances => _attendances.AsReadOnly();

    private Assembly() { }

    public static Assembly Create(Guid buildingId, DateTime date, string location, SessionType sessionType)
    {
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required", nameof(location));

        return new Assembly
        {
            BuildingId = buildingId,
            Date = date,
            Location = location,
            SessionType = sessionType,
            Status = AssemblyStatus.Scheduled
        };
    }

    public void AddAgendaItem(string title, VotingType votingType, bool requiresVote = true, int? order = null)
    {
        var agendaOrder = order ?? (_agendaItems.Any() ? _agendaItems.Max(a => a.Order) + 1 : 1);
        var item = AgendaItem.Create(Id, title, agendaOrder, votingType, requiresVote);
        _agendaItems.Add(item);
        MarkAsUpdated();
    }

    public void RemoveAgendaItem(Guid agendaItemId)
    {
        var item = _agendaItems.FirstOrDefault(a => a.Id == agendaItemId);
        if (item != null)
        {
            _agendaItems.Remove(item);
            MarkAsUpdated();
        }
    }

    public void MarkNoticeSent()
    {
        NoticeSentAt = DateTime.UtcNow;
        Status = AssemblyStatus.NoticeSent;
        MarkAsUpdated();
    }

    public void Start()
    {
        StartedAt = DateTime.UtcNow;
        Status = AssemblyStatus.InProgress;
        MarkAsUpdated();
    }

    public void RecordAttendance(Guid residentId, AttendanceType type, decimal votingShare, Guid? proxyHolderId = null)
    {
        if (_attendances.Any(a => a.ResidentId == residentId))
            throw new InvalidOperationException($"Attendance already recorded for resident {residentId}");

        var attendance = Attendance.Create(Id, residentId, type, votingShare, proxyHolderId);
        _attendances.Add(attendance);
        MarkAsUpdated();
    }

    public void CheckQuorum(decimal totalEligibleVotes)
    {
        var presentVotes = _attendances.Sum(a => a.VotingShare);
        QuorumPercentage = totalEligibleVotes > 0 ? (presentVotes / totalEligibleVotes) * 100 : 0;

        // Compare shares instead of computed percentages: percentage division
        // rounds and would reject an exactly-one-third attendance.
        QuorumAchieved = SessionType switch
        {
            // First session: at least 51% of all eligible votes.
            SessionType.First => totalEligibleVotes > 0 && presentVotes * 100 >= totalEligibleVotes * 51m,
            // Repeated session: at least one third of all eligible votes.
            SessionType.Repeated => totalEligibleVotes > 0 && presentVotes * 3 >= totalEligibleVotes,
            _ => false
        };

        MarkAsUpdated();
    }

    public void RecordVote(Guid agendaItemId, Guid residentId, VoteChoice choice, decimal votingShare)
    {
        var agendaItem = _agendaItems.FirstOrDefault(a => a.Id == agendaItemId);
        if (agendaItem == null)
            throw new InvalidOperationException("Agenda item not found");

        agendaItem.RecordVote(residentId, choice, votingShare);
        MarkAsUpdated();
    }

    public void FinalizeAgendaItem(Guid agendaItemId)
    {
        var agendaItem = _agendaItems.FirstOrDefault(a => a.Id == agendaItemId);
        if (agendaItem == null)
            throw new InvalidOperationException("Agenda item not found");

        agendaItem.FinalizeVote();
        MarkAsUpdated();
    }

    public void Complete()
    {
        CompletedAt = DateTime.UtcNow;
        Status = AssemblyStatus.Completed;
        
        foreach (var attendance in _attendances)
        {
            attendance.MarkAttended();
        }
        
        MarkAsUpdated();
    }

    public void Cancel()
    {
        Status = AssemblyStatus.Cancelled;
        MarkAsUpdated();
    }

    public decimal TotalPresentVotes => _attendances.Sum(a => a.VotingShare);
    public int TotalAttendees => _attendances.Count;
}
