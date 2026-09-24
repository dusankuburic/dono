namespace BuildingManager.Core.Enums;

public enum UnitType
{
    Apartment = 1,
    Studio = 2,
    Commercial = 3,
    Garage = 4,
    Storage = 5,
    Office = 6
}

public enum UnitStatus
{
    OccupiedByOwner = 1,
    OccupiedByFamily = 2,
    OccupiedByTenant = 3,
    Vacant = 4,
    UnderRenovation = 5
}

public enum ResidentRole
{
    Owner = 1,
    CoOwner = 2,
    Tenant = 3,
    FamilyMember = 4
}

public enum KeyType
{
    EntranceMain = 1,
    EntranceSide = 2,
    Unit = 3,
    Storage = 4,
    Garage = 5,
    CommonRoom = 6,
    Basement = 7,
    Roof = 8
}

public enum KeyStatus
{
    Available = 1,
    CheckedOut = 2,
    Lost = 3,
    Damaged = 4,
    InRepair = 5
}

public enum RecipientType
{
    Resident = 1,
    Contractor = 2,
    Viewer = 3,
    NewResident = 4,
    Other = 5
}

public enum WorkOrderCategory
{
    Plumbing = 1,
    Electrical = 2,
    Elevator = 3,
    CommonAreas = 4,
    FacadeRoof = 5,
    EntranceIntercom = 6,
    Heating = 7,
    Cooling = 8,
    Parking = 9,
    Garden = 10,
    Security = 11,
    Cleaning = 12,
    FireSafety = 13,
    Other = 14
}

public enum Priority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Emergency = 4
}

public enum WorkOrderStatus
{
    New = 1,
    Acknowledged = 2,
    InProgress = 3,
    WaitingParts = 4,
    WaitingDecision = 5,
    Completed = 6,
    Cancelled = 7
}

public enum AnnouncementType
{
    General = 1,
    Emergency = 2,
    Maintenance = 3,
    Meeting = 4,
    PaymentReminder = 5
}

public enum AssemblyStatus
{
    Scheduled = 1,
    NoticeSent = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5
}

public enum SessionType
{
    First = 1,
    Repeated = 2
}

public enum VotingType
{
    Simple = 1,
    TwoThirds = 2,
    Unanimous = 3
}

public enum VoteChoice
{
    For = 1,
    Against = 2,
    Abstain = 3
}

public enum VoteResult
{
    Pending = 1,
    Passed = 2,
    Failed = 3
}

public enum AttendanceType
{
    InPerson = 1,
    Proxy = 2,
    Written = 3,
    Electronic = 4
}

public enum ExpenseCategory
{
    UtilitiesElectricity = 1,
    UtilitiesWater = 2,
    UtilitiesHeating = 3,
    UtilitiesGas = 4,
    Repairs = 5,
    Maintenance = 6,
    Cleaning = 7,
    Security = 8,
    Elevator = 9,
    Insurance = 10,
    Legal = 11,
    Administrative = 12,
    Management = 13,
    Reserve = 14,
    Other = 15
}

public enum PaymentStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4
}

public enum PaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    Card = 3,
    Check = 4
}

public enum ManagerType
{
    Regular = 1,
    Professional = 2
}

public enum BuildingStatus
{
    Active = 1,
    Inactive = 2,
    UnderConstruction = 3
}
