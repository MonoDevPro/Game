using Arch.Core;
using LiteNetLib;
using Simulation.Core.ECS.Shared.Systems.Indexes;

namespace Simulation.Core.Network;

public class PacketProcessor(PacketRegistry registry)
{
    public void Process(NetPacketReader reader, World world, IPlayerIndex playerIndex)
    {
        if (reader.AvailableBytes < 1) return;

        var packetId = reader.GetByte();
        if (registry.TryGetHandler(packetId, out var handler))
        {
            handler?.Invoke(in reader, world, playerIndex);
        }
    }
}