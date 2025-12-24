using Game.Network.Abstractions;
using GameECS.Client.Navigation.Systems;
using GameECS.Modules.Navigation.Client.Systems;
using GameECS.Modules.Navigation.Shared.Data;
using GameECS.Shared.Navigation.Data;

namespace GodotClient.Simulation.Contracts;

public class NetworkSender(INetworkManager net) : INetworkSender
{
    public void SendMoveInput(ref MoveInputData input)
    {
        net.SendToServer(input, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
    }
}