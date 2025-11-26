using Arch.Core;

namespace Game.ECS.Components;

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
/// Componente que representa um projétil em movimento.
/// </summary>
public struct Projectile
{
    /// <summary>Entidade que disparou o projétil</summary>
    public Entity Source;
    
    /// <summary>Posição alvo do projétil</summary>
    public Position TargetPosition;
    
    /// <summary>Posição atual do projétil (float para interpolação suave)</summary>
    public float CurrentX;
    public float CurrentY;
    
    /// <summary>Velocidade do projétil em tiles por segundo</summary>
    public float Speed;
    
    /// <summary>Dano a ser aplicado no impacto</summary>
    public int Damage;
    
    /// <summary>Se true, usa ataque/defesa mágica; senão, física</summary>
    public bool IsMagical;
    
    /// <summary>Tempo de vida restante do projétil em segundos (para evitar projéteis eternos)</summary>
    public float RemainingLifetime;
    
    /// <summary>Se o projétil já atingiu algo</summary>
    public bool HasHit;
}

/// <summary>
/// Tipos de animação de ataque disponíveis.
/// </summary>
public enum AttackType : byte
{
    Basic = 0,          // Ataque básico
    Projectile = 1,     // Ataque com projétil
    Special = 2         // Ataque especial/habilidade
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