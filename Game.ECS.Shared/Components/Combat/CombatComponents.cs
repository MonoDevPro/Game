
using Arch.Core;

namespace Game.ECS.Shared.Components.Combat;

/// <summary>
/// Componentes Tags - Marcadores
/// </summary>
public struct Dead { }
public struct Invulnerable { }

// ============================================
// Vitals - Vida e Mana
// ============================================
public struct Health
{
    public int Current; 
    public int Max; 
    public float RegenerationRate; 
    public float AccumulatedRegeneration;
}

public struct Mana
{
    public int Current; 
    public int Max; 
    public float RegenerationRate; 
    public float AccumulatedRegeneration;
}

// ============================================
// Combat - Dano e Defesa
// ============================================

public struct CombatStats
{
    public int AttackPower;     // Physical Attack
    public int MagicPower;      // Magical Attack
    public int Defense;         // Physical Defense
    public int MagicDefense;    // Magical Defense
    public float AttackSpeed;   // Ataques por segundo
    public float AttackRange;   // Alcance do ataque básico
}

public struct DeferredDamage
{
    public int Amount;
    public bool IsCritical;
    public bool IsMagical;
    public Entity Source;
}

public struct DamageOverTime
{
    public float DamagePerSecond;
    public float RemainingTime;
    public float TotalDuration;
    public float AccumulatedDamage;
    
    // Opcional: tipo de dano, origem, etc.
    public bool IsMagical;
    public Entity Source; // se quiser saber quem aplicou
}

/// <summary>
/// Define o estilo de ataque baseado na vocação.
/// </summary>
public enum AttackStyle : byte
{
    Melee = 0,   // Ataque corpo a corpo (Warriors)
    Ranged = 1,  // Ataque à distância com projétil físico (Archers)
    Magic = 2    // Ataque à distância com projétil mágico (Mages)
}

/// <summary>
/// Comando de ataque emitido por uma entidade.
/// </summary>
public struct AttackCommand
{
    public Entity Target;
    public AttackStyle Style;               // Estilo de ataque (Melee, Ranged, Magic)
    public float ConjureDuration;           // Duração do ataque (para animações)
}

public struct CombatState
{
    public bool InCombat;                   // Se está em combate
    public float LastAttackTime;            // Timestamp do último ataque
    public AttackStyle LastAttackStyle;     // Estilo do último ataque
}