namespace Game.ECS.Shared.Components.Entities;

public enum Gender : byte { Male = 1, Female = 2, }
public enum VocationType : byte { Warrior = 1, Archer = 2, Mage = 3 }

// ============================================
// Player identificadores
// ============================================
public struct PlayerControlled { }
public struct PlayerInfo { public Gender Gender; public VocationType Vocation; }