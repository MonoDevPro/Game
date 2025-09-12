using Simulation.Abstractions.Network;
using Simulation.Core.Models;

namespace Simulation.Core.ECS.Shared;

// ---> Flags
[Flags] public enum InputFlags : byte
{
    None=0, 
    Up=1<<0, 
    Down=1<<1, 
    Left=1<<2, 
    Right=1<<3
}
[Flags] public enum IntentFlags : byte
{
    None=0, 
    Move=1<<0, 
    Attack=1<<1
}
[Flags] public enum StateFlags : byte
{
    None     = 0,
    Idle     = 1 << 0,
    Running  = 1 << 1,
    Attacking= 1 << 2,
    Dead     = 1 << 3,
}

// Client --> Server Synced Components
[SynchronizedComponent(Authority.Client)]
public struct InputComponent { public IntentFlags Intent; public InputFlags Input; }

// Server --> Client Synced Components
[SynchronizedComponent(Authority.Server, SyncTrigger.OnChange)]
public struct StateComponent : IEquatable<StateComponent>
{ 
    public StateFlags Value;

    public bool Equals(StateComponent other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is StateComponent other && Equals(other);
    public override int GetHashCode() => (int)Value;
    public static bool operator ==(StateComponent left, StateComponent right) => left.Equals(right);
    public static bool operator !=(StateComponent left, StateComponent right) => !(left == right);
}
[SynchronizedComponent(Authority.Server, SyncTrigger.OnChange)]
public struct Position : IEquatable<Position>
{ 
    public int X, Y;
    public bool Equals(Position other) => X == other.X && Y == other.Y;
    public override bool Equals(object? obj) => obj is Position other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public static bool operator ==(Position left, Position right) => left.Equals(right);
    public static bool operator !=(Position left, Position right) => !(left == right);
}
[SynchronizedComponent(Authority.Server, SyncTrigger.OnChange)]
public struct Direction : IEquatable<Direction>
{ 
    public int X, Y;
    public bool Equals(Direction other) => X == other.X && Y == other.Y;
    public override bool Equals(object? obj) => obj is Direction other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y);
}
[SynchronizedComponent(Authority.Server, SyncTrigger.OnChange)]
public struct Health : IEquatable<Health>
{ 
    public int Current, Max;
    public bool Equals(Health other) => Current == other.Current && Max == other.Max;
    public override bool Equals(object? obj) => obj is Health other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Current, Max);
    public static bool operator ==(Health left, Health right) => left.Equals(right);
    public static bool operator !=(Health left, Health right) => !(left == right);
}

// ---> Tags from Systems of indexing
public struct NewlyCreated; // Tag to identify newly created entities
public struct NewlyDestroyed; // Tag to identify entities to be destroyed
public struct NeedSave; // Tag to identify entities that have changed and need to be synced
public struct SpatialIndexed;
public struct LastKnownPosition { public int X, Y; }


/// Identity Components
public struct MapId { public int Value; }
public struct PlayerId { public int Value; }

/// Metadata Components
public struct MapInfo { public string Name; public int Width, Height; }
public struct PlayerInfo { public string Name; public Gender Gender; public Vocation Vocation; }

// ---> Combat Components
public struct AttackAction { public float CastTimeRemaining; }
public struct AttackCooldown { public float CooldownRemaining; }
public struct AttackStats { public float CastTime; public float Cooldown; public int Damage, AttackRange; }

// ---> Movement Components
public struct MoveAction { public Position Start, Target; public float Elapsed, Duration; }
public struct MoveStats { public float Speed; }

// ---> Teleport Components
public struct TeleportAction { public Position TargetPosition; public float CastTimeRemaining; }
public struct TeleportCooldown { public float CooldownRemaining; }
public struct TeleportStats { public float CastTime; public float Cooldown; }