namespace FitnessCenter.SharedKernel;

/// <summary>
/// An amount of Danish kroner, stored as whole øre. Never floating point (CLAUDE.md, conventions).
/// </summary>
public readonly record struct Money
{
    public Money(long ore)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ore);
        Ore = ore;
    }

    public long Ore { get; }

    public static Money FromKroner(long kroner) => new(checked(kroner * 100));

    public override string ToString() => $"{Ore / 100},{Ore % 100:00} kr.";
}
