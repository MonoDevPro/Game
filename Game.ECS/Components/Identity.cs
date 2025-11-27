using Arch.LowLevel;

namespace Game.ECS.Components;

// ============================================
// Identity - Identificadores únicos
// ============================================
public struct PlayerId { public int Value; }
public struct NetworkId { public int Value; }
public struct MapId { public int Value; }
public struct NameHandle { public Handle<string> Value; }