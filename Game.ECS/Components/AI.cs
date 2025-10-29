// ============================================
// AI - Estado e comportamento
// ============================================
namespace Game.ECS.Components;

public struct AIState { public float DecisionCooldown; public AIBehavior CurrentBehavior; public int TargetNetworkId; }

public enum AIBehavior : byte
{
    Idle,
    Wander,
    Patrol,
    Chase,
    Attack,
    Flee
}