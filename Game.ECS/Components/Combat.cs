namespace Game.ECS.Components;

// ============================================
// Combat - Combate
// ============================================
public struct Attackable { public float BaseSpeed; public float CurrentModifier; }
public struct AttackPower { public int Physical; public int Magical; }
public struct Defense { public int Physical; public int Magical; }

/// Estado de combate básico (mantém cooldown)
public struct CombatState { public bool InCombat; public float LastAttackTime; }

public struct AttackAction
{
    public int DefenderNetworkId;     // network id do alvo
    public AttackType Type;           // tipo de animação/ataque
    public float RemainingDuration;   // tempo restante da animação (s)
    public bool WillHit;              // se o ataque vai acertar (para efeitos)
    public int Damage;                // dano calculado que será aplicado (opcional)
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