namespace Simulation.Core.Ports.Network;


public delegate void PacketHandler<in T>(INetPeerAdapter fromPeer, T packet) 
    where T : IPacket;