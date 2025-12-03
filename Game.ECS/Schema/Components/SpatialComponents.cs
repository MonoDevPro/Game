namespace Game.ECS.Schema.Components;

public enum MovementResult
{
    None,           // sem movimento (zero direction / speed)
    OutOfBounds,
    BlockedByMap,
    BlockedByEntity,
    Allowed
}

// ============================================
// Transform - Posicionamento
// ============================================
public struct MapId                             { public int Value; }
public struct Floor                             { public sbyte Value; }
public struct Speed                             { public float Value; }
public struct Direction                         { public sbyte X; public sbyte Y; }
public struct Position : IEquatable<Position>   { 
    public int X; 
    public int Y;
    
    public bool Equals(Position other) => X == other.X && Y == other.Y; 
    public override bool Equals(object? obj) => obj is Position other && Equals(other); 
    public override int GetHashCode() => HashCode.Combine(X, Y); }
    
// ============================================
// Movement - Movimento
// ============================================
public struct Walkable { public float BaseSpeed; public float CurrentModifier; public float Accumulator; }

/// <summary>
/// Componente temporário que sinaliza intenção de movimento. 
/// Adicionado pelo MovementIntentSystem, consumido pelo ValidationSystem.
/// </summary>
public struct MovementIntent { public Position TargetPosition; public sbyte TargetFloor; }

/// <summary>
/// Tag: Movimento foi validado e pode ser aplicado.
/// </summary>
public struct MovementApproved;

/// <summary>
/// Resultado da validação (opcional, para debug/feedback ao cliente).
/// </summary>
public struct MovementBlocked { public MovementResult Reason; }