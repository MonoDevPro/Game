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
// Network - Sincronização de rede
// ============================================
public struct NetworkDirty { public SyncFlags Flags; }

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
public struct Position { public int X; public int Y; public int Z; }
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
public struct CombatState { public bool InCombat; public uint TargetNetworkId; public float LastAttackTime; }

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