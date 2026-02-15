using Arch.Core;
using Game.Contracts;


namespace Game.Infrastructure.ArchECS.Services.Combat.Components;

public readonly partial record struct EntityCombatEvent(
    CombatEventType Type,
    Entity Attacker,
    Entity Target,
    int DirX,
    int DirY,
    int Damage,
    int X,
    int Y,
    int Floor,
    float Speed,
    int Range);
