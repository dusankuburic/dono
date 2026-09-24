using BuildingManager.Core.Enums;
using BuildingManager.Core.ValueObjects;

namespace BuildingManager.Core.Entities;

public class Building : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public Address Address { get; private set; } = null!;
    public int? YearBuilt { get; private set; }
    public int TotalFloors { get; private set; }
    public bool HasElevator { get; private set; }
    public bool HasParking { get; private set; }
    public decimal TotalAreaM2 { get; private set; }
    public BuildingStatus Status { get; private set; } = BuildingStatus.Active;
    
    public bool IsLegalEntity { get; private set; }
    public string? LegalEntityName { get; private set; }
    public string? MbNumber { get; private set; }
    public string? Pib { get; private set; }
    public string? BankAccount { get; private set; }
    public DateTime? RegistrationDate { get; private set; }
    
    public ManagerType ManagerType { get; private set; } = ManagerType.Regular;
    public Guid? ManagerId { get; private set; }
    public string? ManagerLicenseNumber { get; private set; }
    public DateTime? ManagerAssignedDate { get; private set; }
    
    private readonly List<Unit> _units = new();
    public IReadOnlyCollection<Unit> Units => _units.AsReadOnly();
    
    private readonly List<Resident> _residents = new();
    public IReadOnlyCollection<Resident> Residents => _residents.AsReadOnly();

    private Building() { }

    public static Building Create(string name, Address address, int totalFloors, decimal totalAreaM2)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Building name is required", nameof(name));
        
        return new Building
        {
            Name = name,
            Address = address,
            TotalFloors = totalFloors,
            TotalAreaM2 = totalAreaM2,
            Status = BuildingStatus.Active
        };
    }

    public void UpdateBasicInfo(string name, int? yearBuilt, bool hasElevator, bool hasParking)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Building name is required", nameof(name));

        Name = name;
        YearBuilt = yearBuilt;
        HasElevator = hasElevator;
        HasParking = hasParking;
        MarkAsUpdated();
    }

    public void SetAsLegalEntity(string legalEntityName, string? mbNumber, string? pib, string? bankAccount)
    {
        IsLegalEntity = true;
        LegalEntityName = legalEntityName;
        MbNumber = mbNumber;
        Pib = pib;
        BankAccount = bankAccount;
        RegistrationDate = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void AssignManager(Guid managerId, ManagerType managerType, string? licenseNumber = null)
    {
        ManagerId = managerId;
        ManagerType = managerType;
        ManagerLicenseNumber = licenseNumber;
        ManagerAssignedDate = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void AddUnit(Unit unit)
    {
        if (_units.Any(u => u.Number == unit.Number))
            throw new InvalidOperationException($"Unit with number {unit.Number} already exists");
        
        _units.Add(unit);
        MarkAsUpdated();
    }

    public void RemoveUnit(Guid unitId)
    {
        var unit = _units.FirstOrDefault(u => u.Id == unitId);
        if (unit != null)
        {
            _units.Remove(unit);
            MarkAsUpdated();
        }
    }

    public Unit? GetUnit(string number) => _units.FirstOrDefault(u => u.Number == number);

    public void AddResident(Resident resident)
    {
        _residents.Add(resident);
        MarkAsUpdated();
    }

    public void RemoveResident(Guid residentId)
    {
        var resident = _residents.FirstOrDefault(r => r.Id == residentId);
        if (resident != null)
        {
            resident.Deactivate();
            MarkAsUpdated();
        }
    }

    public int TotalUnits => _units.Count;
    public int TotalResidents => _residents.Count(r => r.IsActive);
    public decimal TotalOwnershipShares => _units.Where(u => !u.IsDeleted).Sum(u => u.OwnershipShare);
}
