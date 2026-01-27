
namespace Game.Infrastructure.ArchECS.Services.Navigation.Components;

// ============================================
// COMPONENTES DO SERVIDOR (Autoritativos)
// Usa Position existente de Game.ECS.Components
// ============================================

// Spatial components
public struct MapId                     { public int Value; }
public struct FloorId                   { public int Value; }
public struct Direction                 { public int X; public int Y; }
public struct Velocity                  { public int X; public int Y; }
public struct Position : IEquatable<Position> { public int X; public int Y;
    public bool Equals(Position other) => X == other.X && Y == other.Y;
    public override bool Equals(object? obj) => obj is Position other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y);
}

/// <summary>
/// Estado do pathfinding da entidade.
/// </summary>
public struct NavPathState
{
    public PathStatus Status;
    public PathFailReason FailReason;
}

/// <summary>
/// Tag: entidade é um agente de navegação.
/// </summary>
public struct NavAgent { }

/// <summary>
/// Tag: entidade está se movendo via navegação.
/// </summary>
public struct NavIsMoving { }

/// <summary>
/// Tag: entidade chegou ao destino. 
/// </summary>
public struct NavReachedDestination { }

/// <summary>
/// Tag: entidade bloqueada aguardando. 
/// </summary>
public struct NavWaitingToMove
{
    public long WaitStartTick;
    public int BlockedByEntityId;
}