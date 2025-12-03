
using Arch.LowLevel;

namespace Game.ECS.Schema.Components;

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
public struct NameHandle { public Handle<string> Value; }

// ============================================
// Outros identificadores
// ============================================
public struct GenderId { public byte Value; }
public struct VocationId { public byte Value; }