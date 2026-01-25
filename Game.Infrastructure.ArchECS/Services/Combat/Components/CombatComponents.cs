namespace Game.Infrastructure.ArchECS.Services.Combat.Components;

// Componentes básicos de combate (placeholders para expansão gradual)

public struct CombatStats
{
    public int Level;
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

public struct DamageEvent
{
    public int AttackerId;
    public int TargetId;
    public int Amount;
    public bool IsCritical;
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
