using Arch.Core;
using Game.Contracts;
using Game.Infrastructure.ArchECS.Services.Navigation.Components;

namespace Game.Infrastructure.ArchECS.Services.Events;

public readonly record struct SpawnEvent(Entity Entity, Position SpawnPosition, int SpawnFloor);

public readonly record struct DespawnEvent(Entity Entity);

public readonly record struct MoveEvent(Entity Entity, Position TargetPosition, int TargetFloor);

public readonly record struct AttackStartedEvent(
    Entity Attacker,
    int DirX,
    int DirY,
    int PosX,
    int PosY,
    int Floor);

public readonly record struct ProjectileSpawnedEvent(
    Entity Attacker,
    int DirX,
    int DirY,
    int PosX,
    int PosY,
    int Floor,
    float Speed,
    int Range,
    int TeamId,
    int Damage);

public readonly record struct CombatDamageEvent(
    Entity Attacker,
    Entity Victim,
    int DirX,
    int DirY,
    int PosX,
    int PosY,
    int Floor);
    