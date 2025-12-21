namespace GameECS.Modules.Entities.Shared.Data;

/// <summary>
/// Tipos de entidade no jogo.
/// </summary>
public enum EntityType : byte
{
    None = 0,
    Player = 1,
    Npc = 2,
    Monster = 3,
    Pet = 4,
    Projectile = 5,
    Item = 6,
    Interactive = 7
}

/// <summary>
/// Subtipo de NPC.
/// </summary>
public enum NpcSubType : byte
{
    None = 0,
    Friendly = 1,
    Neutral = 2,
    Hostile = 3,
    Boss = 4,
    Elite = 5
}

/// <summary>
/// Comportamento do NPC.
/// </summary>
public enum NpcBehaviorType : byte
{
    Static = 0,
    Stationary = 0, // Alias
    Wander = 1,
    Patrol = 2,
    Guard = 3,
    Follow = 4,
    Flee = 5
}

/// <summary>
/// Estado de IA do NPC.
/// </summary>
public enum NpcAIState : byte
{
    Idle = 0,
    Wandering = 1,
    Patrolling = 2,
    Chasing = 3,
    Attacking = 4,
    Returning = 5,
    Fleeing = 6,
    Dead = 7
}

/// <summary>
/// Modo do pet.
/// </summary>
public enum PetMode : byte
{
    Follow = 0,
    Stay = 1,
    Aggressive = 2,
    Defensive = 3,
    Passive = 4,
    Attack = 5,
    Defend = 6
}

/// <summary>
/// Distribuição de loot.
/// </summary>
public enum LootDistribution : byte
{
    FreeForAll = 0,
    RoundRobin = 1,
    Leader = 2,
    NeedBeforeGreed = 3
}

/// <summary>
/// Distribuição de experiência.
/// </summary>
public enum ExpDistribution : byte
{
    Equal = 0,
    ByDamage = 1,
    ByLevel = 2
}
