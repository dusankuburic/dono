namespace BuildingManager.Core.Entities;

public class FeeStructure : BaseEntity
{
    public Guid BuildingId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal AmountPerM2 { get; private set; }
    public decimal? FixedAmount { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public string? Description { get; private set; }

    private FeeStructure() { }

    public static FeeStructure Create(Guid buildingId, string name, decimal amountPerM2, 
        DateTime effectiveFrom, decimal? fixedAmount = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        return new FeeStructure
        {
            BuildingId = buildingId,
            Name = name,
            AmountPerM2 = amountPerM2,
            FixedAmount = fixedAmount,
            EffectiveFrom = effectiveFrom,
            Description = description
        };
    }

    public void Deactivate(DateTime effectiveTo)
    {
        IsActive = false;
        EffectiveTo = effectiveTo;
        MarkAsUpdated();
    }

    public decimal CalculateFee(decimal areaM2)
    {
        var areaBasedFee = AmountPerM2 * areaM2;
        return FixedAmount.HasValue ? FixedAmount.Value + areaBasedFee : areaBasedFee;
    }
}
