using System;
using MemoryPack;

namespace Game.ECS.Components;

// ============================================
// Tags - Marcadores de comportamento
// ============================================
public struct LocalPlayerTag { }
public struct RemotePlayerTag { }
public struct PlayerControlled { }
public struct AIControlled { }
public struct Dead { }
public struct Invulnerable { }
public struct Silenced { } // Não pode usar habilidades

// ============================================
// Identity - Identificadores únicos
// ============================================
public struct PlayerId { public int Value; }
public struct NetworkId { public int Value; }

// ============================================
// World Context - Informações de mapa
// ============================================
public struct MapId { public int Value; }

// ============================================
// Inputs - Entrada do jogador
// ============================================
[MemoryPackable]
public struct PlayerInput { public sbyte InputX; public sbyte InputY; public InputFlags Flags; }

// ============================================
// Vitals - Vida e Mana
// ============================================
public struct Health { public int Current; public int Max; public float RegenerationRate; }
public struct Mana { public int Current; public int Max; public float RegenerationRate; }

// ============================================
// Transform - Posicionamento
// ============================================
public struct Position
{
	public int X;
	public int Y;
	public int Z;

	/// <summary>
	/// Distância Manhattan (taxicab) em células.
	/// </summary>
	public readonly int ManhattanDistance(Position other)
	{
		return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
	}
}
public struct Velocity { public int DirectionX; public int DirectionY; public float Speed; }
public struct PreviousPosition { public int X; public int Y; public int Z; } // Para reconciliação

// ============================================
// Movement - Movimento
// ============================================
public struct Walkable { public float BaseSpeed; public float CurrentModifier; }
public struct Facing { public int DirectionX; public int DirectionY; }
public struct Movement { public float Timer; } // Acumulador de movimento por célula

// ============================================
// Combat - Combate
// ============================================
public struct Attackable { public float BaseSpeed; public float CurrentModifier; }
public struct AttackPower { public int Physical; public int Magical; }
public struct Defense { public int Physical; public int Magical; }
public struct CombatState { public bool InCombat; public int TargetNetworkId; public float LastAttackTime; }

// ============================================
// Status Effects - Efeitos de status
// ============================================
public struct Stun { public float RemainingTime; } // Atordoado
public struct Slow { public float RemainingTime; public float SpeedModifier; } // Desacelerado
public struct Poison { public float RemainingTime; public int DamagePerSecond; } // Envenenado
public struct Burning { public float RemainingTime; public int DamagePerSecond; } // Queimado

// ============================================
// Cooldowns - Tempos de espera
// ============================================
public struct AbilityCooldown { public float[] RemainingTimes; } // Array de cooldowns por habilidade
public struct ItemCooldown { public float RemainingTime; }

// ============================================
// Respawn - Reaparição
// ============================================
public struct RespawnData { public float RespawnTimer; public int RespawnMapId; public int RespawnX; public int RespawnY; }

// ============================================
// AI - Estado e comportamento
// ============================================
public struct AIState
{
	public float DecisionCooldown;
	public AIBehavior CurrentBehavior;
	public int TargetNetworkId;
}

public enum AIBehavior : byte
{
	Idle,
	Wander,
	Patrol,
	Chase,
	Attack,
	Flee
}

// ============================================
// Network Sync - Flags de sujidade
// ============================================
public struct DirtyFlags
{
	public ushort Flags;

	public void MarkDirty(DirtyComponentType type)
	{
		Flags |= (ushort)(1 << (int)type);
	}

	public void ClearDirty(DirtyComponentType type)
	{
		Flags &= (ushort)~(1 << (int)type);
	}

	public bool IsDirty(DirtyComponentType type)
	{
		return (Flags & (ushort)(1 << (int)type)) != 0;
	}

	public void ClearAll()
	{
		Flags = 0;
	}
}

public enum DirtyComponentType : byte
{
	Position = 0,
	Health = 1,
	Mana = 2,
	Facing = 3,
	Combat = 4,
}