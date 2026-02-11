using Arch.Core;

namespace Game.Infrastructure.ArchECS.Services.Combat.Events;

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