using Arch.Core;
using Game.DTOs.Player;
using Game.ECS.Components;
using Game.ECS.Services.Snapshot.Data;

namespace Game.ECS.Events;

/// <summary>
/// Event fired when an entity takes damage.
/// </summary>
/// <param name="Source">The entity that dealt the damage (may be Entity.Null for environmental damage).</param>
/// <param name="Target">The entity that received the damage.</param>
/// <param name="Amount">The amount of damage dealt.</param>
/// <param name="IsCritical">Whether this was a critical hit.</param>
public readonly record struct DamageEvent(Entity Source, Entity Target, int Amount, bool IsCritical);

/// <summary>
/// Event fired when an entity dies.
/// </summary>
/// <param name="Entity">The entity that died.</param>
/// <param name="Killer">The entity that killed it (may be Entity.Null for environmental deaths).</param>
/// <param name="Position">The position where death occurred.</param>
public readonly record struct DeathEvent(Entity Entity, Entity Killer, Position Position);

/// <summary>
/// Event fired when an entity spawns.
/// </summary>
/// <param name="Entity">The newly spawned entity.</param>
/// <param name="Position">The spawn position.</param>
/// <param name="NetworkId">The network identifier for the entity.</param>
public readonly record struct SpawnEvent(Entity Entity, SpawnPoint SpawnPoint, int NetworkId);

/// <summary>
/// Event fired when an entity despawns.
/// </summary>
/// <param name="Entity">The entity being despawned.</param>
/// <param name="NetworkId">The network identifier for the entity.</param>
public readonly record struct DespawnEvent(Entity Entity, int NetworkId);

/// <summary>
/// Event fired when an attack is initiated.
/// </summary>
/// <param name="Attacker">The attacking entity.</param>
/// <param name="Target">The target entity (may be Entity.Null for area attacks).</param>
/// <param name="Style">The attack style being used.</param>
/// <param name="Damage">The base damage amount.</param>
public readonly record struct AttackEvent(Entity Attacker, Entity Target, AttackStyle Style, int Damage);

/// <summary>
/// Event fired when an entity's health changes.
/// </summary>
/// <param name="Entity">The entity whose health changed.</param>
/// <param name="OldValue">The previous health value.</param>
/// <param name="NewValue">The new health value.</param>
/// <param name="MaxValue">The maximum health value.</param>
public readonly record struct HealthChangedEvent(Entity Entity, int OldValue, int NewValue, int MaxValue);

/// <summary>
/// Event fired when an entity's mana changes.
/// </summary>
/// <param name="Entity">The entity whose mana changed.</param>
/// <param name="OldValue">The previous mana value.</param>
/// <param name="NewValue">The new mana value.</param>
/// <param name="MaxValue">The maximum mana value.</param>
public readonly record struct ManaChangedEvent(Entity Entity, int OldValue, int NewValue, int MaxValue);

/// <summary>
/// Event fired when an entity moves.
/// </summary>
/// <param name="Entity">The entity that moved.</param>
/// <param name="OldPosition">The previous position.</param>
/// <param name="NewPosition">The new position.</param>
public readonly record struct MovementEvent(Entity Entity, Position OldPosition, Position NewPosition);

/// <summary>
/// Event fired when an Entity changes its direction/state.
/// </summary>
/// <param name="Entity">The entity whose state changed.</param>
/// <param name="OldDirection">The previous direction.</param>
/// <param name="NewDirection">The new direction.</param>
public readonly record struct DirectionChangedEvent(Entity Entity, Direction OldDirection, Direction NewDirection);

/// <summary>
/// Event fired when an NPC changes its AI state.
/// </summary>
/// <param name="Entity">The NPC entity whose state changed.</param>
/// <param name="OldState">The previous AI state.</param>
/// <param name="NewState">The new AI state.</param>
public readonly record struct NpcStateChangedEvent(Entity Entity, AIState OldState, AIState NewState);
