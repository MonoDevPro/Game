using MemoryPack;

namespace Game.DTOs.Player;

/// <summary>
/// Snapshot de movimento de jogador para sincronização com clientes.
/// Contém posição atual, destino e estado do movimento para interpolação.
/// </summary>
[MemoryPackable]
public readonly partial record struct PlayerMovementSnapshot(
    int NetworkId,
    short CurrentX,
    short CurrentY,
    short CurrentZ,
    short TargetX,
    short TargetY,
    short TargetZ,
    bool IsMoving,
    sbyte DirectionX,
    sbyte DirectionY,
    ushort TicksRemaining
)
{
    /// <summary>
    /// Calcula duração em segundos baseado nos ticks restantes.
    /// </summary>
    public readonly float GetDurationSeconds(float tickRate)
    {
        return TicksRemaining / tickRate;
    }
}

/// <summary>
/// Pacote de movimentação de jogadores para broadcast.
/// </summary>
[MemoryPackable]
public readonly partial record struct PlayerMovementPacket(PlayerMovementSnapshot[] Movements);
