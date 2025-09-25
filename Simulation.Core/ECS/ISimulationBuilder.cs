using Arch.Core;
using Simulation.Core.ECS.Builders;
using Simulation.Core.ECS.Pipeline;
using Simulation.Core.ECS.Services;
using Simulation.Core.Options;

namespace Simulation.Core.ECS;

/// <summary>
/// Define um contrato para a construção de uma pipeline de simulação completa.
/// </summary>
public interface ISimulationBuilder<TData> where TData : notnull
{
    /// <summary>
    /// Fornece as opções de configuração de autoridade.
    /// </summary>
    ISimulationBuilder<float> WithAuthorityOptions(AuthorityOptions options);
    
    /// <summary>
    /// Fornece as opções de configuração do mundo ECS.
    /// </summary>
    ISimulationBuilder<TData> WithWorldOptions(WorldOptions options);

    /// <summary>
    /// Fornece o serviço de mapas para o gerenciamento do mundo.
    /// </summary>
    /// <param name="service">O serviço de mapas a ser utilizado.</param>
    /// <returns>O construtor de simulação para encadeamento.</returns>
    ISimulationBuilder<float> WithMapService(MapService service);
    
    /// <summary>
    /// Fornece o contentor de serviços da aplicação principal para resolver dependências externas.
    /// </summary>
    ISimulationBuilder<TData> WithRootServices(IServiceProvider services);

    /// <summary>
    /// Constrói e retorna o grupo de sistemas (a pipeline) configurado.
    /// </summary>
    /// Um Group pronto a ser executado.
    (GroupSystems Systems, World World, WorldManager WorldManager) Build();
}