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
/// </summary>
public struct NpcPath
{
    /// <summary>
    /// Buffer de waypoints pré-alocado (tamanho fixo para evitar alocações).
    /// </summary>
    public Position Waypoint0, Waypoint1, Waypoint2, Waypoint3, Waypoint4;
    public Position Waypoint5, Waypoint6, Waypoint7, Waypoint8, Waypoint9;
    public Position Waypoint10, Waypoint11, Waypoint12, Waypoint13, Waypoint14;
    public Position Waypoint15, Waypoint16, Waypoint17, Waypoint18, Waypoint19;
    public Position Waypoint20, Waypoint21, Waypoint22, Waypoint23, Waypoint24;
    public Position Waypoint25, Waypoint26, Waypoint27, Waypoint28, Waypoint29;
    public Position Waypoint30, Waypoint31;
    
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
    
    /// <summary>Capacidade máxima de waypoints</summary>
    public const int MaxWaypoints = 32;
    
    /// <summary>Intervalo padrão de recálculo em segundos</summary>
    public const float RecalculateInterval = 0.5f;
    
    /// <summary>Distância mínima do alvo para considerar recálculo (tiles²)</summary>
    public const int TargetMovedThresholdSq = 4; // 2 tiles
    
    public readonly Position GetWaypoint(int index) => index switch
    {
        0 => Waypoint0, 1 => Waypoint1, 2 => Waypoint2, 3 => Waypoint3,
        4 => Waypoint4, 5 => Waypoint5, 6 => Waypoint6, 7 => Waypoint7,
        8 => Waypoint8, 9 => Waypoint9, 10 => Waypoint10, 11 => Waypoint11,
        12 => Waypoint12, 13 => Waypoint13, 14 => Waypoint14, 15 => Waypoint15,
        16 => Waypoint16, 17 => Waypoint17, 18 => Waypoint18, 19 => Waypoint19,
        20 => Waypoint20, 21 => Waypoint21, 22 => Waypoint22, 23 => Waypoint23,
        24 => Waypoint24, 25 => Waypoint25, 26 => Waypoint26, 27 => Waypoint27,
        28 => Waypoint28, 29 => Waypoint29, 30 => Waypoint30, 31 => Waypoint31,
        _ => default
    };
    
    public void SetWaypoint(int index, Position position)
    {
        switch (index)
        {
            case 0: Waypoint0 = position; break; case 1: Waypoint1 = position; break;
            case 2: Waypoint2 = position; break; case 3: Waypoint3 = position; break;
            case 4: Waypoint4 = position; break; case 5: Waypoint5 = position; break;
            case 6: Waypoint6 = position; break; case 7: Waypoint7 = position; break;
            case 8: Waypoint8 = position; break; case 9: Waypoint9 = position; break;
            case 10: Waypoint10 = position; break; case 11: Waypoint11 = position; break;
            case 12: Waypoint12 = position; break; case 13: Waypoint13 = position; break;
            case 14: Waypoint14 = position; break; case 15: Waypoint15 = position; break;
            case 16: Waypoint16 = position; break; case 17: Waypoint17 = position; break;
            case 18: Waypoint18 = position; break; case 19: Waypoint19 = position; break;
            case 20: Waypoint20 = position; break; case 21: Waypoint21 = position; break;
            case 22: Waypoint22 = position; break; case 23: Waypoint23 = position; break;
            case 24: Waypoint24 = position; break; case 25: Waypoint25 = position; break;
            case 26: Waypoint26 = position; break; case 27: Waypoint27 = position; break;
            case 28: Waypoint28 = position; break; case 29: Waypoint29 = position; break;
            case 30: Waypoint30 = position; break; case 31: Waypoint31 = position; break;
        }
    }
    
    /// <summary>
    /// Retorna o waypoint atual sendo seguido.
    /// </summary>
    public readonly Position GetCurrentWaypoint() => GetWaypoint(CurrentIndex);
    
    /// <summary>
    /// Avança para o próximo waypoint. Retorna true se ainda há waypoints.
    /// </summary>
    public bool AdvanceToNextWaypoint()
    {
        if (CurrentIndex + 1 >= WaypointCount)
            return false;
        
        CurrentIndex++;
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
    /// Limpa o caminho atual.
    /// </summary>
    public void ClearPath()
    {
        WaypointCount = 0;
        CurrentIndex = 0;
    }
    
    /// <summary>
    /// Marca para recálculo.
    /// </summary>
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
