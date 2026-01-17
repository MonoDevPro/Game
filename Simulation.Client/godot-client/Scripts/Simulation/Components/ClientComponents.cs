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
