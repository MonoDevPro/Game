namespace Game.ECS.Components;

public enum MovementResult { None, OutOfBounds, BlockedByMap, BlockedByEntity, Allowed }

// ============================================
// Transform - Posicionamento
// ============================================
public struct MapId                             { public int Value; }
public partial struct Speed                     { public float Value; }
public partial struct Direction                 { public int X; public int Y; }
public partial struct Position                  { public int X; public int Y; public int Z; }
public struct SpatialAnchor                     { public int MapId; public Position Position; public bool IsTracked; }