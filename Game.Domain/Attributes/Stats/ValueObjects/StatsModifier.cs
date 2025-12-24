namespace Game.Domain.Attributes.Stats.ValueObjects;

/// <summary>
/// Modificadores percentuais aplicados aos atributos.
/// </summary>
public readonly record struct StatsModifier(
    double StrengthFactor,
    double DexterityFactor,
    double IntelligenceFactor,
    double ConstitutionFactor,
    double SpiritFactor,
    double CriticalFactor = 1.0,
    double AttackSpeedFactor = 1.0,
    double MovementSpeedFactor = 1.0)
{
    public static StatsModifier Default => new(1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0);

    public double ApplyStrength(double value) => value * StrengthFactor;
    public double ApplyDexterity(double value) => value * DexterityFactor;
    public double ApplyIntelligence(double value) => value * IntelligenceFactor;
    public double ApplyConstitution(double value) => value * ConstitutionFactor;
    public double ApplySpirit(double value) => value * SpiritFactor;
    public float ApplyCritical(float value) => (float)(value * CriticalFactor);
    public float ApplyAttackSpeed(float value) => (float)(value * AttackSpeedFactor);
    public float ApplyMovementSpeed(float value) => (float)(value * MovementSpeedFactor);
}