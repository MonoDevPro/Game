using Arch.System;
using Simulation.Core.Options;

namespace Simulation.Core.ECS;

/// <summary>
/// Define um contrato para a construção de uma pipeline de simulação completa.
/// </summary>
public interface ISimulationBuilder
{
    /// <summary>
    /// Fornece as opções de configuração do mundo ECS.
    /// </summary>
    ISimulationBuilder WithWorldOptions(WorldOptions options);

    /// <summary>
    /// Fornece as opções de configuração do sistema espacial.
    /// </summary>
    ISimulationBuilder WithSpatialOptions(SpatialOptions options);
    
    /// <summary> Fornece as opções de configuração do sistema de rede.</summary>
    ISimulationBuilder WithNetworkOptions(NetworkOptions options, DebugOptions? debugOptions = null);
    
    /// <summary>
    /// Fornece o contentor de serviços da aplicação principal para resolver dependências externas.
    /// </summary>
    ISimulationBuilder WithRootServices(IServiceProvider services);


    /// <summary>
    /// Constrói e retorna o grupo de sistemas (a pipeline) configurado.
    /// </summary>
    /// Um Group pronto a ser executado.
    Group<float> Build();
}