using BuildingManager.Core.Enums;

namespace BuildingManager.Core.Entities;

public class Attendance : BaseEntity
{
    public Guid AssemblyId { get; private set; }
    public Guid ResidentId { get; private set; }
    public AttendanceType Type { get; private set; }
    public decimal VotingShare { get; private set; }
    public Guid? ProxyHolderId { get; private set; }
    public DateTime? AttendedAt { get; private set; }
    public bool DidAttend { get; private set; }

    private Attendance() { }

    internal static Attendance Create(Guid assemblyId, Guid residentId, AttendanceType type, 
        decimal votingShare, Guid? proxyHolderId = null)
    {
        return new Attendance
        {
            AssemblyId = assemblyId,
            ResidentId = residentId,
            Type = type,
            VotingShare = votingShare,
            ProxyHolderId = proxyHolderId
        };
    }

    internal void MarkAttended()
    {
        DidAttend = true;
        AttendedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }
}
