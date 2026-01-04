using Arch.Core;
using Game.DTOs.Player;

namespace Game.ECS.Components;

// ============================================
// Componentes de Combate
// ============================================

public struct CombatStats
{
    public int AttackPower;     // Physical Attack
    public int MagicPower;      // Magical Attack
    public int Defense;         // Physical Defense
    public int MagicDefense;    // Magical Defense
    public float AttackSpeed;   // Ataques por segundo
    public float AttackRange;   // Alcance do ataque
}

// Gerencia o estado temporal do combate (Cooldowns)
public struct CombatState
{
    // New fields
    public float CooldownTimer;   // Tempo até poder atacar de novo
    public bool InCooldown;       // Se está em cooldown de ataque
    public float LastAttackTime;  // Timestamp do último ataque
}

// O "Evento" ou "Intenção". 
// O NPC não causa dano direto; ele emite um "Comando de Ataque".
public struct AttackCommand
{
    public Entity Target;
    public Position TargetPosition; // Posição alvo (para ataques à distância)
    public AttackStyle Style; // Estilo de ataque (Melee, Ranged, Magic)
    public float ConjureDuration; // Duração do ataque (para animações)
}

/// <summary>
/// Componente que representa um projétil em movimento.
/// </summary>
public struct Projectile
{
    public Entity Source;
    public Position TargetPosition;
    public float CurrentX;
    public float CurrentY;
    public float Speed;
    public int Damage;
    public bool IsMagical;
    public float RemainingLifetime;
    public bool HasHit;
}

