
namespace Game.Infrastructure.ArchECS.Services.Navigation.Components;

// ============================================
// COMPONENTES DO SERVIDOR (Autoritativos)
// Usa Position existente de Game.ECS.Components
// ============================================



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