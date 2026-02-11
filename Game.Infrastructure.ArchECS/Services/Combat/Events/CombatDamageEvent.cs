using Arch.Core;

namespace Game.Infrastructure.ArchECS.Services.Combat.Events;

public readonly record struct CombatDamageEvent(
    Entity Attacker,
    Entity Target,
    int Damage,
    int DirX,
    int DirY,
    int PosX,
    int PosY,
    int Floor);