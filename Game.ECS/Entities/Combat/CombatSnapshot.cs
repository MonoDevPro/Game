namespace Game.ECS.Entities.Combat;

public readonly record struct CombatHitEvent (
    int TargetNetworkId, int SourceNetworkId, int DamageAmount, byte DamageType, byte HitFlags);

public readonly record struct NpcAttackSnapshot(
    int AttackerNetworkId, int TargetNetworkId, int TargetX, int TargetY, byte AttackType);