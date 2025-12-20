
using Game.ECS.Components;
using Game.ECS.Navigation.Components;

namespace Game.ECS.Navigation.Data;

/// <summary>
/// Pacote de sincronização em lote (múltiplas entidades).
/// </summary>
public struct BatchMovementUpdate
{
    public long ServerTick;
    public MovementSnapshot[] Snapshots;
}

/// <summary>
/// Pacote de teleporte (posição instantânea).
/// </summary>
public struct TeleportMessage
{
    public int EntityId;
    public short X;
    public short Y;
    public MovementDirection FacingDirection;
}

/// <summary>
/// Resposta do servidor ao input do player.
/// </summary>
public struct MoveConfirmation
{
    public int SequenceId;          // ID da requisição original
    public bool Success;
    public short FinalX;
    public short FinalY;
    public long ServerTick;
}

/// <summary>
/// Input de movimento do player para enviar ao servidor.
/// </summary>
public struct MoveInput
{
    public int SequenceId;
    public short TargetX;
    public short TargetY;
    public long ClientTick;
}