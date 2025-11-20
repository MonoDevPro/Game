using Arch.Core;

namespace Game.ECS.Components;

// ============================================
// Combat - Combate
// ============================================
public struct Attackable { public float BaseSpeed; public float CurrentModifier; }
public struct AttackPower { public int Physical; public int Magical; }
public struct Defense { public int Physical; public int Magical; }

/// Estado de combate básico (mantém cooldown)
public struct CombatState
{
    public bool InCombat; 
    public float LastAttackTime;
    
    /// <summary>
    /// Tempo (em segundos) desde o último evento de combate relevante (ex: tomar dano).
    /// Usado para pausar/retomar regeneração de HP/MP.
    /// </summary>
    public float TimeSinceLastHit;
}

public struct Attack
{
    public AttackType Type;           // tipo de animação/ataque
    public float RemainingDuration;   // tempo restante da animação (s)
    public float TotalDuration;       // duração total da animação (s)
    public bool DamageApplied;        // flag para marcar se o dano já foi aplicado
}

public struct AttackTarget
{
    public Entity TargetEntity;       // entidade alvo do ataque
}

/// <summary>
/// Tipos de animação de ataque disponíveis.
/// </summary>
public enum AttackType : byte
{
    Basic = 0,          // Ataque básico
    Heavy = 1,          // Ataque carregado
    Critical = 2,       // Golpe crítico
    Magic = 3,          // Ataque mágico
}

/// <summary>
/// Define em qual fase da animação o dano deve ser aplicado.
/// A animação é dividida em 3 fases: Early (0-33%), Mid (33-66%), Late (66-100%)
/// </summary>
public enum DamageTimingPhase : byte
{
    Early = 0,  // Dano aplicado no início (0-33% da animação)
    Mid = 1,    // Dano aplicado no meio (33-66% da animação)
    Late = 2,   // Dano aplicado no final (66-100% da animação)
}