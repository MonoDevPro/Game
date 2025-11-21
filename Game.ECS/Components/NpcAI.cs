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
