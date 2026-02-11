using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Infrastructure.ArchECS.Commons;
using Game.Infrastructure.ArchECS.Services.EntityRegistry.Components;
using Microsoft.Extensions.Logging;

namespace Game.Infrastructure.ArchECS.Services.EntityRegistry.Systems;

/// <summary>
/// Sistema para gerenciar o ciclo de vida do EntityRegistry.
/// Segue o padrão Arch.System para integração consistente.
/// </summary>
public partial class EntityRegistrySystem(World world, ILogger? logger = null) : GameSystem(world, logger)
{
    private readonly CentralEntityRegistry _registry = world.GetEntityRegistry();

    /// <summary>
    /// Cleanup de entidades mortas/inválidas.
    /// Chamado periodicamente para manter registry consistente.
    /// </summary>
    [Query]
    [All<EntityRegistryCleanup>]
    private void CleanupDeadEntities(in Entity entity)
    {
        if (!World.IsAlive(entity))
        {
            _registry.Unregister(entity);
            LogDebug($"Cleaned up dead entity: {entity}");
        }
    }

    public override void AfterUpdate(in long deltaTime)
    {
        // Executar cleanup periódico
        CleanupDeadEntitiesQuery(World);
    }

    public RegistryStatistics GetStatistics() => _registry.GetStatistics();

    public override void Dispose()
    {
        _registry?.Dispose();
        base.Dispose();
    }
}