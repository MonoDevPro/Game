using Game.ECS.Navigation.Client.Contracts;
using Game.ECS.Navigation.Shared.Data;
using Game.Network.Abstractions;

namespace GodotClient.Simulation.Contracts;

public class ClientNetworkSender(INetworkManager sender) : INetworkSender
{
    public void SendMoveInput(MoveInput input)
    {
        sender.SendToServer(input, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
    }
}