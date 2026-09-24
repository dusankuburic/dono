namespace BuildingManager.Core.ValueObjects;

public record Percentage
{
    public decimal Value { get; init; }

    public Percentage() { }

    public Percentage(decimal value)
    {
        if (value < 0 || value > 100)
            throw new ArgumentOutOfRangeException(nameof(value), "Percentage must be between 0 and 100");
        Value = value;
    }

    public static Percentage Zero => new(0);
    public static Percentage Hundred => new(100);

    public static Percentage FromFraction(decimal fraction)
    {
        if (fraction < 0 || fraction > 1)
            throw new ArgumentOutOfRangeException(nameof(fraction), "Fraction must be between 0 and 1");
        return new Percentage(fraction * 100);
    }

    public decimal ToFraction() => Value / 100m;

    public decimal ApplyTo(decimal amount) => amount * ToFraction();

    public static Percentage operator +(Percentage left, Percentage right)
        => new(Math.Min(100, left.Value + right.Value));

    public static Percentage operator -(Percentage left, Percentage right)
        => new(Math.Max(0, left.Value - right.Value));

    public override string ToString() => $"{Value:N2}%";
}
