using BuildingManager.Core.Enums;

namespace BuildingManager.Core.Entities;

public class Resident : BaseEntity
{
    public Guid BuildingId { get; private set; }
    public Guid? UnitId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public ResidentRole Role { get; private set; } = ResidentRole.Owner;
    public decimal? OwnershipShare { get; private set; }
    public string? EmergencyContactName { get; private set; }
    public string? EmergencyContactPhone { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime? MoveInDate { get; private set; }
    public DateTime? MoveOutDate { get; private set; }
    
    public int MissedAssembliesCount { get; private set; }
    public bool IsUnavailableOwner => MissedAssembliesCount >= 3;

    private Resident() { }

    public static Resident Create(Guid buildingId, Guid? unitId, string firstName, string lastName, 
        ResidentRole role, decimal? ownershipShare = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required", nameof(firstName));
        
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required", nameof(lastName));

        return new Resident
        {
            BuildingId = buildingId,
            UnitId = unitId,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            OwnershipShare = ownershipShare,
            MoveInDate = DateTime.UtcNow
        };
    }

    public void UpdateContactInfo(string? phone, string? email)
    {
        Phone = phone;
        Email = email;
        MarkAsUpdated();
    }

    public void UpdateEmergencyContact(string? name, string? phone)
    {
        EmergencyContactName = name;
        EmergencyContactPhone = phone;
        MarkAsUpdated();
    }

    public void UpdateOwnershipShare(decimal share)
    {
        if (share < 0 || share > 100)
            throw new ArgumentException("Ownership share must be between 0 and 100", nameof(share));
        
        OwnershipShare = share;
        MarkAsUpdated();
    }

    public void ChangeRole(ResidentRole newRole)
    {
        Role = newRole;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MoveOutDate = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void Reactivate()
    {
        IsActive = true;
        MoveOutDate = null;
        MarkAsUpdated();
    }

    public void RecordAssemblyAttendance(bool attended)
    {
        if (!attended)
        {
            MissedAssembliesCount++;
        }
        else
        {
            MissedAssembliesCount = 0;
        }
        MarkAsUpdated();
    }

    public void ResetMissedAssemblies()
    {
        MissedAssembliesCount = 0;
        MarkAsUpdated();
    }

    public string FullName => $"{FirstName} {LastName}";
}
