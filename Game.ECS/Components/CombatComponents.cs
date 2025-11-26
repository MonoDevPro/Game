using Arch.Core;

namespace Game.ECS.Components;

// ============================================
// Combat Refactor - Command Based
// ============================================

// Substitui AttackPower, Defense, AttackSpeed dispersos
public struct CombatStats
{
    public int AttackPower;     // Physical Attack
    public int MagicPower;      // Magical Attack
    public int Defense;         // Physical Defense
    public int MagicDefense;    // Magical Defense
    public float AttackRange;   // Range físico ou mágico
    public float AttackSpeed;   // Ataques por segundo
}

// Gerencia o estado temporal do combate (Cooldowns)
public struct CombatState
{
    // New fields
    public float AttackCooldownTimer;   // Tempo até poder atacar de novo
    public bool InCooldown;             // Se está em cooldown de ataque
    public float CastTimer;             // Tempo restante de cast (se aplicável)
    public bool IsCasting;              // Se está em cast
}

// O "Evento" ou "Intenção". 
// O NPC não causa dano direto; ele emite um "Comando de Ataque".
public struct AttackCommand
{
    public Entity Target;
    public Position TargetPosition; // Posição alvo (para ataques à distância)
    public AttackStyle Style; // Estilo de ataque (Melee, Ranged, Magic)
    public bool IsReady; // Flag processada pelo sistema
}
