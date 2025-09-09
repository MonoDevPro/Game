using Arch.Core;
using Simulation.Core.Shared.Network.Attributes;

namespace Simulation.Core.Shared.Components;

// ---> Flags
[Flags] public enum StateFlagsEnum : byte { None=0, MoveUp=1<<0, MoveDown=1<<1, MoveLeft=1<<2, MoveRight=1<<3, Attack=1<<4 }
[Flags] public enum TargetFlagsEnum : uint { None=0u, Player=1u<<0, NPC=1u<<1, Monster=1u<<2 }

[SynchronizedComponent(Authority.Server, SyncTrigger.OnChange)]
public struct StateFlags { public StateFlagsEnum FlagsEnum; }

public struct TargetFlags { public TargetFlagsEnum FlagsEnum; }

// ---> Tags
public struct Indexed;
public struct MapIndexed;
public struct SpatialIndexed;

// ---> Identity
public struct PlayerId { public int Value; }
public struct MapId { public int Value; }

// ---> Combat
[SynchronizedComponent(Authority.Client)]
public struct AttackIntent { public Entity Target; }

[SynchronizedComponent(Authority.Server, SyncTrigger.OnChange)]
public struct AttackAction { public Entity Target; public float CastTimeRemaining; }

[SynchronizedComponent(Authority.Server, SyncTrigger.OnChange)]
public struct Health { public int Current, Max; }

[SynchronizedComponent(Authority.Server, SyncTrigger.OnChange)]
public struct Dead;

public struct AttackStats { public float CastTime; public float Cooldown; public int Damage, AttackRange; }
public struct AttackCooldown { public float CooldownRemaining; }

// ---> Movement
[SynchronizedComponent(Authority.Client)]
public struct MoveIntent { public Direction Direction; }

[SynchronizedComponent(Authority.Server, SyncTrigger.OnChange)]
public struct MoveAction { public Position Start, Target; public float Elapsed, Duration; }

[SynchronizedComponent(Authority.Server, SyncTrigger.OnChange)]
public struct Position { public int X, Y; }

[SynchronizedComponent(Authority.Server, SyncTrigger.OnChange)]
public struct Direction { public int X, Y; }

public struct MoveStats { public float Speed; }
public struct LastKnownPosition { public Position Value; }

// ---> Teleport
[SynchronizedComponent(Authority.Client)]
public struct TeleportIntent { public Position TargetPosition; }

public struct TeleportCooldown { public float CooldownRemaining; }