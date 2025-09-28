using Arch.LowLevel;
using Simulation.Core.ECS.Components.Data;

namespace Simulation.Core.ECS.Components;

[Flags] public enum StateFlags { Idle = 0, Moving = 1<<0 , Attacking = 1<<1 , Dead = 1<<2 }

// Player
public readonly record struct PlayerId(int Value);
public readonly record struct PlayerName(Handle<string> Value);
public readonly record struct PlayerGender(Gender Value);
public readonly record struct PlayerVocation(Vocation Value);
public readonly record struct PlayerState(StateFlags Flags);
public readonly record struct AttackStats(float CastTime, float Cooldown, int Damage, int AttackRange);
public readonly record struct MoveStats(float Speed);

// ---> Componentes de Estado
public readonly record struct Position(int X, int Y);
public readonly record struct Health(int Current, int Max);
public readonly record struct Direction(int X, int Y);

// Requests
public readonly record struct SpawnPlayerRequest(PlayerData Player);
public readonly record struct DespawnPlayerRequest(int PlayerId);

// ---> Movimento
public readonly record struct MoveIntent(Direction Direction);
public readonly record struct MoveTarget(Position Start, Position Target);
public readonly record struct MoveTimer(float Elapsed, float Duration);

// ---> Attack
public readonly record struct AttackIntent(Direction Direction);
public readonly record struct AttackTarget(Position Start, Position Target);
public readonly record struct AttackCooldown(float CooldownRemaining);
public readonly record struct AttackTimer (float Elapsed, float Duration);