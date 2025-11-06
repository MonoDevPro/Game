namespace Game.ECS.Components;

// ============================================
// Combat - Combate
// ============================================
public struct Attackable { public float BaseSpeed; public float CurrentModifier; }
public struct AttackPower { public int Physical; public int Magical; }
public struct Defense { public int Physical; public int Magical; }
public struct CombatState { public bool InCombat; public float LastAttackTime; }
/// <summary>
/// Componente que armazena informações sobre o ataque em progresso.
/// </summary>
public struct AttackAnimation
{
    public int DefenderNetworkId;
    public float RemainingDuration;
    public int Damage;
    public bool WasHit;
    public AttackAnimationType AnimationType;
    public bool IsActive => RemainingDuration > 0;
}

/// <summary>
/// Tipos de animação de ataque disponíveis.
/// </summary>
public enum AttackAnimationType : byte
{
    Basic = 0,          // Ataque básico
    Heavy = 1,          // Ataque carregado
    Critical = 2,       // Golpe crítico
    Magic = 3,          // Ataque mágico
}