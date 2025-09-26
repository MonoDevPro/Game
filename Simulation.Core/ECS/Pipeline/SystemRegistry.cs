using Arch.Core;
using Arch.System;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Resources;
using Simulation.Core.ECS.Sync;
using Simulation.Core.ECS.Systems;
using Simulation.Core.Options;

namespace Simulation.Core.ECS.Pipeline;

/// <summary>
/// Responsible for registering and configuring systems in the ECS pipeline.
/// Separates system registration logic from GroupSystems execution logic.
/// </summary>
public class SystemRegistry
{
    private readonly World _world;
    private readonly ResourceContext _resources;
    private readonly Group<float> _groupSystems;

    public SystemRegistry(World world, ResourceContext resources, Group<float> groupSystems)
    {
        _world = world;
        _resources = resources;
        _groupSystems = groupSystems;
    }

    /// <summary>
    /// Registers all systems for server-side simulation.
    /// </summary>
    public void RegisterServerSystems()
    {
        // Input systems (inbox) - handle incoming client data
        RegisterInboxSync<Input>();

        // Core game logic systems
        RegisterGameplaySystems();

        // Output systems (outbox) - send server state to clients
        RegisterServerOutputSystems();
    }

    /// <summary>
    /// Registers all systems for client-side simulation.
    /// </summary>
    public void RegisterClientSystems()
    {
        // Client systems would be registered here
        // Currently the codebase only shows server systems
    }

    /// <summary>
    /// Registers core gameplay systems that process game logic.
    /// </summary>
    private void RegisterGameplaySystems()
    {
        RegisterSystem<MovementSystem>(_world);
        // Additional gameplay systems can be easily added here
    }

    /// <summary>
    /// Registers server output systems that synchronize state to clients.
    /// Uses the new SyncOptionsBuilder to reduce boilerplate.
    /// </summary>
    private void RegisterServerOutputSystems()
    {
        // State sync with reliable delivery for important state changes
        RegisterOutboxSync<State>(SyncOptionsBuilder.ServerBroadcast().Build());

        // Position sync with unreliable high-frequency updates for smoother movement
        RegisterOutboxSync<Position>(SyncOptionsBuilder.PositionSync().Build());

        // Direction sync for character facing
        RegisterOutboxSync<Direction>(SyncOptionsBuilder.ServerBroadcast().Build());

        // Health sync with reliable delivery for critical information
        RegisterOutboxSync<Health>(SyncOptionsBuilder.ServerBroadcast().Build());
    }

    /// <summary>
    /// Registers a generic system with dependency injection.
    /// </summary>
    private void RegisterSystem<TSystem>(params object[] args) where TSystem : BaseSystem<World, float>
    {
        var system = Activator.CreateInstance(typeof(TSystem), args) as ISystem<float>;
        if (system != null)
        {
            _groupSystems.Add(system);
        }
    }

    /// <summary>
    /// Registers an inbox synchronization system for receiving component updates.
    /// </summary>
    private void RegisterInboxSync<T>() where T : struct, IEquatable<T>
    {
        var networkInbox = new NetworkInbox<T>();
        _resources.NetworkEndpoint.RegisterHandler<ComponentSyncPacket<T>>((peer, packet) => { networkInbox.Enqueue(packet); });
        RegisterSystem<NetworkComponentApplySystem<T>>(_world, networkInbox, _resources.PlayerIndex);
    }

    /// <summary>
    /// Registers an outbox synchronization system for sending component updates.
    /// </summary>
    private void RegisterOutboxSync<T>(SyncOptions options) where T : struct, IEquatable<T>
    {
        RegisterSystem<NetworkOutbox<T>>(_world, _resources.NetworkEndpoint, options);
    }
}