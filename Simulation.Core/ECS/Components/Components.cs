using Simulation.Core.Network.Contracts;
using Simulation.Core.Options;
using Simulation.Core.Persistence.Models;

namespace Simulation.Core.ECS.Components;

// ---> Flags
[Flags] public enum InputFlags : byte {None=0, Up=1<<0, Down=1<<1, Left=1<<2, Right=1<<3 }
[Flags] public enum IntentFlags : byte {None=0, Move=1<<0, Attack=1<<1 }
[Flags] public enum StateFlags : byte {None=0, Idle=1<<0, Running=1<<1, Attacking=1<<2, Dead=1<<3}

[Sync(Authority = Authority.Server, Trigger = SyncTrigger.OnChange, DeliveryMethod = NetworkDeliveryMethod.ReliableOrdered)]
public readonly record struct InputComponent(IntentFlags Intent, InputFlags Input);

[Sync(Authority = Authority.Server, Trigger = SyncTrigger.OnChange, DeliveryMethod = NetworkDeliveryMethod.ReliableOrdered)]
public readonly record struct ActionComponent(StateFlags Value);

[Sync(Authority = Authority.Server, Trigger = SyncTrigger.OnChange, DeliveryMethod = NetworkDeliveryMethod.ReliableOrdered)]
public readonly record struct Position(int X, int Y);

[Sync(Authority = Authority.Server, Trigger = SyncTrigger.OnChange, DeliveryMethod = NetworkDeliveryMethod.ReliableOrdered)]
public readonly record struct Direction(int X, int Y);

[Sync(Authority = Authority.Server, Trigger = SyncTrigger.OnTick, 
    SyncRateTicks = 10, DeliveryMethod = NetworkDeliveryMethod.Unreliable)]
public readonly record struct Health(int Current, int Max);


// Player
public readonly record struct PlayerId(int Value);
public readonly record struct PlayerInfo(string Name, Gender Gender, Vocation Vocation);

// Saving
public struct NeedSave;
public struct NeedDelete;

// Indexing
public struct Indexed;
public struct Unindexed;
public struct SpatialIndexed;
public struct SpatialUnindexed;
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