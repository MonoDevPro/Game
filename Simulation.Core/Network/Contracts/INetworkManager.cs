using Simulation.Core.Options;

namespace Simulation.Core.Network.Contracts;

/// <summary>
/// Abstração para o manager de rede; expõe operações de inicialização, envio, broadcast e consulta de peers.
/// </summary>
public interface INetworkManager
{
    NetworkAuthority Authority { get; }
    
    /// <summary>Inicializa o manager (decide StartServer/StartClient com base nas options).</summary>
    void Initialize();

    /// <summary>Processa os eventos pendentes da biblioteca (deve ser chamado periodicamente).</summary>
    void PollEvents();

    /// <summary>Interrompe o manager e fecha conexões.</summary>
    void Stop();
}