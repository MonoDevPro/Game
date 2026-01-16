namespace GodotClient.Simulation.Components;

/// <summary>
/// Tag para identificar o jogador local (controlado por este cliente).
/// </summary>
public struct LocalPlayerTag;

/// <summary>
/// Tag para identificar jogadores remotos (controlados por outros clientes).
/// </summary>
public struct RemotePlayerTag;

/// <summary>
/// Flags de componentes que foram modificados e precisam ser sincronizados.
/// </summary>
[System.Flags]
public enum DirtyComponentType : byte
{
    None = 0,
    Input = 1 << 0,
    Position = 1 << 1,
    Health = 1 << 2,
    Mana = 1 << 3,
    Direction = 1 << 4,
    All = Input | Position | Health | Mana | Direction
}

/// <summary>
/// Componente que armazena quais componentes foram modificados.
/// </summary>
public struct DirtyFlags
{
    public DirtyComponentType Flags;
    
    public bool IsEmpty => Flags == DirtyComponentType.None;
    
    public void Mark(DirtyComponentType flag) => Flags |= flag;
    
    /// <summary>
    /// Marca um componente como dirty (alias para Mark).
    /// </summary>
    public void MarkDirty(DirtyComponentType flag) => Flags |= flag;
    
    public void Clear() => Flags = DirtyComponentType.None;
    
    public bool HasFlag(DirtyComponentType flag) => (Flags & flag) != 0;
    
    /// <summary>
    /// Consume e retorna os flags atuais, limpando o estado.
    /// </summary>
    public DirtyComponentType ConsumeSnapshot()
    {
        var current = Flags;
        Flags = DirtyComponentType.None;
        return current;
    }
}

/// <summary>
/// Dados de movimento de NPC no cliente para interpolação.
/// </summary>
public struct NpcMovementData
{
    public short TargetX;
    public short TargetY;
    public short TargetZ;
    public bool IsMoving;
    public ushort TicksRemaining;
}

/// <summary>
/// Dados de movimento de jogador no cliente para interpolação.
/// </summary>
public struct PlayerMovementData
{
    public short TargetX;
    public short TargetY;
    public short TargetZ;
    public bool IsMoving;
    public ushort TicksRemaining;
}
