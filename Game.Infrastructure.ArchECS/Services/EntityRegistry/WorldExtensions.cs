using System.Collections.Concurrent;
using Arch.Core;

namespace Game.Infrastructure.ArchECS.Services.EntityRegistry;

/// <summary>
/// Extensions para integrar CentralEntityRegistry como Resource no World (padrão Arch).
/// </summary>
public static class WorldEntityRegistryExtensions
{
    private static readonly ConcurrentDictionary<World, CentralEntityRegistry> Registries = new();

    /// <summary>
    /// Obtém ou cria o registry centralizado no World.
    /// Usa o sistema de Resources do Arch para armazenamento global.
    /// </summary>
    public static CentralEntityRegistry GetEntityRegistry(this World world)
    {
        if (Registries.TryGetValue(world, out var registry)) 
            return registry;
        
        registry = new CentralEntityRegistry();
        Registries[world] = registry;
        return registry;
    }

    /// <summary>
    /// Registra entidade usando o registry do World.
    /// </summary>
    public static void RegisterEntity(this World world, int externalId, Entity entity, 
        EntityDomain domain)
    {
        var registry = world.GetEntityRegistry();
        registry.Register(externalId, entity, domain);
    }

    /// <summary>
    /// Registra entidade em múltiplos domínios.
    /// </summary>
    public static void RegisterEntityMultiDomain(this World world, int externalId, Entity entity, 
        EntityDomain domains)
    {
        var registry = world.GetEntityRegistry();
        registry.RegisterMultiDomain(externalId, entity, domains);
    }

    /// <summary>
    /// Remove registro de entidade.
    /// </summary>
    public static bool UnregisterEntity(this World world, Entity entity)
    {
        var registry = world.GetEntityRegistry();
        return registry.Unregister(entity);
    }

    /// <summary>
    /// Obtém Entity por ID externo.
    /// </summary>
    public static Entity GetEntityById(this World world, int externalId, EntityDomain domain)
    {
        var registry = world.GetEntityRegistry();
        return registry.GetEntity(externalId, domain);
    }

    /// <summary>
    /// Tenta obter Entity por ID externo.
    /// </summary>
    public static bool TryGetEntityById(this World world, int externalId, EntityDomain domain, out Entity entity)
    {
        var registry = world.GetEntityRegistry();
        return registry.TryGetEntity(externalId, domain, out entity);
    }
}