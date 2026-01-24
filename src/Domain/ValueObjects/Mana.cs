namespace Domain.ValueObjects;

public record Mana
{
    public int Current { get; }
    public int Max { get; }

    private Mana(int current, int max)
    {
        if (max <= 0)
            throw new ArgumentException("Max mana must be positive.", nameof(max));

        Current = Math.Clamp(current, 0, max);
        Max = max;
    }

    public static Mana Full(int max) => new(max, max);

    public Mana Consume(int amount) => new(Current - amount, Max);

    public Mana Restore(int amount) => new(Current + amount, Max);

    public bool HasEnough(int amount) => Current >= amount;

    public bool IsEmpty => Current <= 0;
    
    public bool IsFull => Current >= Max;
    
    public double Percentage => Max > 0 ? (double)Current / Max * 100 : 0;
}
