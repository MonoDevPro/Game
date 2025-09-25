namespace Simulation.Core.Ports.Network;

public interface IChannelProcessorFactory
{
    /// <summary>
    /// Cria (ou retorna cached) um IPacketProcessor + dispatcher endpoint para o canal.
    /// </summary>
    IChannelEndpoint CreateOrGet(NetworkChannel channel);
        
    /// <summary>
    /// Tenta obter sem criar.
    /// </summary>
    bool TryGet(NetworkChannel channel, out IChannelEndpoint? processor);
}