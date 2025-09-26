
using Arch.LowLevel;
using Simulation.Core.ECS.Components.Data;

namespace Simulation.Core.ECS.Components;

// Player
public readonly record struct PlayerId(int Value);
public readonly record struct PlayerInfo(Handle<string> Name, Gender Gender, Vocation Vocation);

// Saving
public struct NeedSave;

public readonly record struct LastKnownPosition(Position Position);

// ---> Componentes de Combate
public readonly record struct AttackAction(float CastTimeRemaining);
public readonly record struct AttackCooldown(float CooldownRemaining);
public readonly record struct AttackStats(float CastTime, float Cooldown, int Damage, int AttackRange);

// ---> Componentes de Movimento
public readonly record struct MoveAction(Position Start, Position Target, float Elapsed, float Duration);
public readonly record struct MoveStats(float Speed);

// ---> Componentes de Teleporte
public readonly record struct TeleportAction(Position TargetPosition, float CastTimeRemaining);
public readonly record struct TeleportCooldown(float CooldownRemaining);
public readonly record struct TeleportStats(float CastTime, float Cooldown);