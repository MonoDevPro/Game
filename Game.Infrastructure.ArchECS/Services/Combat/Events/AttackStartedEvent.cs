using Arch.Core;

namespace Game.Infrastructure.ArchECS.Services.Combat.Events;

public readonly record struct AttackStartedEvent(
    Entity Attacker,
    int DirX,
    int DirY,
    int PosX,
    int PosY,
    int Floor);