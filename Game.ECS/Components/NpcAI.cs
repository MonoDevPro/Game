using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Arch.Core;

namespace Game.ECS.Components;

public enum NpcAIStateId : byte
{
    Idle = 0,
    Patrolling = 1,
    Chasing = 2,
    Attacking = 3,
    Returning = 4
}

public enum NpcBehaviorType : byte
{
    Passive = 0,
    Defensive = 1,
    Aggressive = 2
}

public struct NpcAIState
{
    public NpcAIStateId Current;
    public float StateTime;

    public void Advance(float deltaTime) => StateTime += deltaTime;

    public bool TrySetState(NpcAIStateId newState)
    {
        if (Current == newState)
            return false;

        Current = newState;
        StateTime = 0f;
        return true;
    }
}

public struct NpcTarget
{
    public Entity Target;
    public int TargetNetworkId;
    public Position LastKnownPosition;
    public float DistanceSquared;
    private bool _hasTarget;

    public readonly bool HasTarget => _hasTarget;

    public static NpcTarget CreateEmpty()
    {
        var target = new NpcTarget
        {
            Target = Entity.Null,
            TargetNetworkId = -1,
            LastKnownPosition = default,
            DistanceSquared = 0f,
            _hasTarget = false
        };
        return target;
    }

    public void SetTarget(Entity target, int networkId, Position position, float distanceSq)
    {
        Target = target;
        TargetNetworkId = networkId;
        LastKnownPosition = position;
        DistanceSquared = distanceSq;
        _hasTarget = true;
    }

    public void Clear()
    {
        Target = Entity.Null;
        TargetNetworkId = -1;  // -1 means no target (allows NetworkId 0 for valid targets)
        DistanceSquared = 0f;
        LastKnownPosition = default;
        _hasTarget = false;
    }
}

public struct NpcBehavior
{
    public NpcBehaviorType Type;
    public float VisionRange;
    public float AttackRange;
    public float LeashRange;
    public float PatrolRadius;
    public float IdleDurationMin;
    public float IdleDurationMax;
    
    public static readonly NpcBehavior Default = new()
    {
        Type = NpcBehaviorType.Passive,
        VisionRange = 5f,
        AttackRange = 1.5f,
        LeashRange = 10f,
        PatrolRadius = 8f,
        IdleDurationMin = 1f,
        IdleDurationMax = 3f
    };
}

public struct NpcPatrol
{
    public Position HomePosition;
    public Position Destination;
    public float Radius;
    public bool HasDestination;

    public void ResetDestination()
    {
        Destination = HomePosition;
        HasDestination = false;
    }
}

/// <summary>
/// Componente que armazena o caminho calculado pelo sistema de pathfinding A*.
/// Usa um buffer inline via fixed array para zero-allocation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NpcPath
{
    /// <summary>Capacidade máxima de waypoints (inline buffer)</summary>
    public const int MaxWaypoints = 32;
    
    /// <summary>Intervalo padrão de recálculo em segundos (só usado quando alvo se move)</summary>
    public const float RecalculateInterval = 1.0f;
    
    /// <summary>Distância mínima do alvo para considerar recálculo (tiles²)</summary>
    public const int TargetMovedThresholdSq = 9; // 3 tiles
    
    /// <summary>Tempo máximo que um NPC pode ficar no mesmo waypoint antes de recalcular (stuck detection)</summary>
    public const float StuckTimeout = 1.0f;
    
    /// <summary>
    /// Buffer fixo inline de waypoints (X,Y alternados: X0,Y0,X1,Y1...).
    /// Total: 32 waypoints = 64 ints = 256 bytes
    /// </summary>
    private fixed int _waypoints[MaxWaypoints * 2];
    
    /// <summary>Índice do waypoint atual sendo seguido (0 a WaypointCount-1)</summary>
    public byte CurrentIndex;
    
    /// <summary>Quantos waypoints válidos existem no buffer</summary>
    public byte WaypointCount;
    
    /// <summary>Timer para recálculo periódico do caminho</summary>
    public float RecalculateTimer;
    
    /// <summary>Flag que indica se o caminho precisa ser recalculado</summary>
    public bool NeedsRecalculation;
    
    /// <summary>Última posição conhecida do alvo (para detectar mudanças significativas)</summary>
    public Position LastTargetPosition;
    
    /// <summary>Timer de stuck detection - reseta quando muda de waypoint</summary>
    public float StuckTimer;
    
    /// <summary>
    /// Obtém o waypoint no índice especificado.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Position GetWaypoint(int index)
    {
        if ((uint)index >= MaxWaypoints) return default;
        fixed (int* ptr = _waypoints)
        {
            int baseIdx = index * 2;
            return new Position(ptr[baseIdx], ptr[baseIdx + 1]);
        }
    }
    
    /// <summary>
    /// Define o waypoint no índice especificado.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWaypoint(int index, Position position)
    {
        if ((uint)index >= MaxWaypoints) return;
        fixed (int* ptr = _waypoints)
        {
            int baseIdx = index * 2;
            ptr[baseIdx] = position.X;
            ptr[baseIdx + 1] = position.Y;
        }
    }
    
    /// <summary>
    /// Retorna o waypoint atual sendo seguido.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Position GetCurrentWaypoint() => GetWaypoint(CurrentIndex);
    
    /// <summary>
    /// Avança para o próximo waypoint. Retorna true se ainda há waypoints.
    /// Reseta o timer de stuck detection.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AdvanceToNextWaypoint()
    {
        if (CurrentIndex + 1 >= WaypointCount)
            return false;
        
        CurrentIndex++;
        StuckTimer = 0f; // Reseta o timer ao avançar
        return true;
    }
    
    /// <summary>
    /// Verifica se há um caminho válido.
    /// </summary>
    public readonly bool HasPath => WaypointCount > 0;
    
    /// <summary>
    /// Verifica se completou o caminho.
    /// </summary>
    public readonly bool IsPathComplete => CurrentIndex >= WaypointCount;
    
    /// <summary>
    /// Verifica se o NPC está stuck (não progride no path).
    /// </summary>
    public readonly bool IsStuck => StuckTimer >= StuckTimeout;
    
    /// <summary>
    /// Limpa o caminho atual.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearPath()
    {
        WaypointCount = 0;
        CurrentIndex = 0;
        StuckTimer = 0f;
    }
    
    /// <summary>
    /// Marca para recálculo.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RequestRecalculation()
    {
        NeedsRecalculation = true;
    }
    
    /// <summary>
    /// Cria um NpcPath com valores iniciais.
    /// </summary>
    public static NpcPath CreateDefault() => new()
    {
        CurrentIndex = 0,
        WaypointCount = 0,
        RecalculateTimer = 0f,
        NeedsRecalculation = true,
        LastTargetPosition = default
    };
}
