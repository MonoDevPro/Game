namespace Game.ECS.Components;

public struct AttackPower { public int Physical; public int Magical; }
public struct Defense { public int Physical; public int Magical; }
public struct CombatState { public bool InCombat; public uint TargetNetworkId; public float LastAttackTime; }
