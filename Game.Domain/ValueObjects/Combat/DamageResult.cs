using System.Runtime.InteropServices;
using Game.Domain.Enums;

namespace Game.Domain.ValueObjects.Combat;

/// <summary>
/// Resultado de um cálculo de dano.
/// Component ECS para armazenar o resultado de ataques.
/// Este ValueObject é otimizado para uso como component no ArchECS.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct DamageResult
{
    public int Damage { get; init; }
    public DamageType Type { get; init; }
    public bool IsCritical { get; init; }

    public DamageResult(int damage, DamageType type, bool isCritical)
    {
        if (damage < 0)
            throw new ArgumentException("Damage cannot be negative", nameof(damage));
        
        Damage = damage;
        Type = type;
        IsCritical = isCritical;
    }

    /// <summary>
    /// Cria uma nova instância com um valor de dano diferente.
    /// </summary>
    public DamageResult WithDamage(int newDamage) => new(newDamage, Type, IsCritical);
    
    /// <summary>
    /// Cria uma nova instância com um tipo de dano diferente.
    /// </summary>
    public DamageResult WithType(DamageType newType) => new(Damage, newType, IsCritical);
    
    /// <summary>
    /// Cria uma nova instância marcando ou desmarcando crítico.
    /// </summary>
    public DamageResult WithCritical(bool critical) => new(Damage, Type, critical);
    
    /// <summary>
    /// Aplica o dano aos vitais e retorna os novos valores de HP/MP.
    /// </summary>
    public (int NewHp, int NewMp) ApplyToVitals(int currentHp, int currentMp)
    {
        int newHp = Type switch
        {
            DamageType.Physical or DamageType.Magical or DamageType.True 
                => Math.Max(0, currentHp - Damage),
            _ => currentHp
        };
        
        return (newHp, currentMp);
    }

    /// <summary>
    /// Valor zero para inicialização.
    /// </summary>
    public static DamageResult Zero => default;
}
