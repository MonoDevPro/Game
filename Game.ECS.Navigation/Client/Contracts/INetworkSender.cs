using Game.ECS.Navigation.Shared.Data;

namespace Game.ECS.Navigation.Client.Contracts;

/// <summary>
/// Interface para envio de mensagens de rede.
/// </summary>
public interface INetworkSender
{
    void SendMoveInput(MoveInput input);
}