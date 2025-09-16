namespace Simulation.Core.Network.Contracts;


public delegate void PacketHandler<in T>(INetPeerAdapter fromPeer, T packet) 
    where T : IPacket;