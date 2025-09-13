using Arch.Core;
using Arch.System;
using Simulation.Core.Options;

namespace Simulation.Core.ECS;

/// <summary>
/// Define um contrato para a construção de uma pipeline de simulação completa.
/// </summary>
public interface ISimulationBuilder<TData> where TData : notnull
{
    /// <summary>
    /// Fornece as opções de configuração do mundo ECS.
    /// </summary>
    ISimulationBuilder<TData> WithWorldOptions(WorldOptions options);

    /// <summary>
    /// Fornece as opções de configuração do sistema espacial.
    /// </summary>
    ISimulationBuilder<TData> WithSpatialOptions(SpatialOptions options);
    
    /// <summary> Fornece as opções de configuração do sistema de rede.</summary>
    ISimulationBuilder<TData> WithNetworkOptions(NetworkOptions options);
    
    /// <summary>
    /// Fornece o contentor de serviços da aplicação principal para resolver dependências externas.
    /// </summary>
    ISimulationBuilder<TData> WithRootServices(IServiceProvider services);

    /// <summary>
    /// Registra um componente para ser sincronizado automaticamente pela rede.
    /// </summary>
    /// <typeparam name="T">O tipo do componente a ser sincronizado.</typeparam>
    /// <param name="options">As opções de sincronização (autoridade, gatilho, etc.).</param>
    ISimulationBuilder<TData> WithSynchronizedComponent<T>(SyncOptions options) where T : struct, IEquatable<T>;

    /// <summary>
    /// Constrói e retorna o grupo de sistemas (a pipeline) configurado.
    /// </summary>
    /// Um Group pronto a ser executado.
    (Group<TData> Group, World World) Build();
}