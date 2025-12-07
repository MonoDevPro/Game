
namespace Game.ECS.Entities.Components;

// ============================================
// Tags - Marcadores
// ============================================
public struct PlayerControlled { }
public struct AIControlled { }

// ============================================
// Identity - Identificadores únicos
// ============================================
public struct UniqueID { public int Value; }
public struct NetworkId { public int Value; }

// ============================================
// Outros identificadores
// ============================================
public struct GenderId { public byte Value; }
public struct VocationId { public byte Value; }