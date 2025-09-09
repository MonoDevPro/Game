using Arch.Core;

namespace Simulation.Components;

public struct AttackIntent { public Entity Target; }
public struct AttackStats { public float CastTime; public float Cooldown; public int Damage, AttackRange; }
public struct AttackAction { public Entity Target; public float CastTimeRemaining; }
public struct AttackCooldown { public float CooldownRemaining; }
public struct Dead;
public struct Health { public int Current, Max; }
