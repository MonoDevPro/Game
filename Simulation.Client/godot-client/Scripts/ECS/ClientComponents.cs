namespace GodotClient.ECS;

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
