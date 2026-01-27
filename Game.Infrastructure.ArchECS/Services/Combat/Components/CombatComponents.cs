namespace Game.Infrastructure.ArchECS.Services.Combat.Components;

// Componentes básicos de combate (placeholders para expansão gradual)

/// <summary>
/// Tag para marcar entidades de combate no ECS.
/// </summary>
public struct CombatEntity { }

/// <summary>
/// Tag para marcar entidades mortas.
/// </summary>
public struct Dead { }

/// <summary>
/// Tag para marcar entidades desabilitadas.
/// </summary>
public struct Disabled { }

/// <summary>
/// Componente de AI básico.
/// </summary>
public struct AIComponent 
{ 
    public int BehaviorTreeId;
    public float AggroRange;
}

public struct CombatStats
{
    public int Level;
    public long Experience;
    public int Strength;
    public int Endurance;
    public int Agility;
    public int Intelligence;
    public int Willpower;
    public int MaxHealth;
    public int MaxMana;
    public int CurrentHealth;
    public int CurrentMana;
}

public struct Vitals
{
    public int CurrentHp;
    public int CurrentMp;
    public int MaxHp;
    public int MaxMp;
}

public struct VocationTag
{
    public byte Value;
}

public struct TeamId
{
    public int Value;
}

public struct AttackCooldown
{
    public long NextAttackTick;
    public int CooldownTicks;
}

public struct AttackRequest
{
    public int DirX;
    public int DirY;
    public long RequestedTick;
}

public struct Projectile
{
    public int OwnerId;
    public int OwnerTeamId;
    public int Damage;
    public int DirX;
    public int DirY;
    public int RemainingRange;
    public float SpeedCellsPerTick;
    public float TravelRemainder;
}
