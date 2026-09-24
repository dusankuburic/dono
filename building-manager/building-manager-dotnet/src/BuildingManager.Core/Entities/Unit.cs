using BuildingManager.Core.Enums;

namespace BuildingManager.Core.Entities;

public class Unit : BaseEntity
{
    public Guid BuildingId { get; private set; }
    public string Number { get; private set; } = string.Empty;
    public int Floor { get; private set; }
    public string? Entrance { get; private set; }
    public decimal AreaM2 { get; private set; }
    public decimal OwnershipShare { get; private set; }
    public UnitType Type { get; private set; } = UnitType.Apartment;
    public UnitStatus Status { get; private set; } = UnitStatus.Vacant;
    
    private readonly List<Resident> _residents = new();
    public IReadOnlyCollection<Resident> Residents => _residents.AsReadOnly();

    private Unit() { }

    public static Unit Create(Guid buildingId, string number, int floor, decimal areaM2, decimal ownershipShare, UnitType type)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Unit number is required", nameof(number));
        
        if (areaM2 <= 0)
            throw new ArgumentException("Area must be positive", nameof(areaM2));
        
        if (ownershipShare <= 0 || ownershipShare > 100)
            throw new ArgumentException("Ownership share must be between 0 and 100", nameof(ownershipShare));

        return new Unit
        {
            BuildingId = buildingId,
            Number = number,
            Floor = floor,
            AreaM2 = areaM2,
            OwnershipShare = ownershipShare,
            Type = type,
            Status = UnitStatus.Vacant
        };
    }

    public void UpdateDetails(decimal areaM2, decimal ownershipShare, UnitType type)
    {
        if (areaM2 <= 0)
            throw new ArgumentException("Area must be positive", nameof(areaM2));

        if (ownershipShare <= 0 || ownershipShare > 100)
            throw new ArgumentException("Ownership share must be between 0 and 100", nameof(ownershipShare));

        AreaM2 = areaM2;
        OwnershipShare = ownershipShare;
        Type = type;
        MarkAsUpdated();
    }

    public void SetEntrance(string? entrance)
    {
        Entrance = entrance;
        MarkAsUpdated();
    }

    public void UpdateStatus(UnitStatus status)
    {
        Status = status;
        MarkAsUpdated();
    }

    public void AddResident(Resident resident)
    {
        _residents.Add(resident);
        Status = resident.Role switch
        {
            ResidentRole.Owner => UnitStatus.OccupiedByOwner,
            ResidentRole.FamilyMember => UnitStatus.OccupiedByFamily,
            ResidentRole.Tenant => UnitStatus.OccupiedByTenant,
            _ => Status
        };
        MarkAsUpdated();
    }

    public void RemoveResident(Guid residentId)
    {
        var resident = _residents.FirstOrDefault(r => r.Id == residentId);
        if (resident != null)
        {
            _residents.Remove(resident);
            if (!_residents.Any())
            {
                Status = UnitStatus.Vacant;
            }
            MarkAsUpdated();
        }
    }
}
