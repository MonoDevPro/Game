using Arch.Core;
using Arch.LowLevel;
using Game.ECS.Components;
using Game.ECS.Entities.Npc;
using Game.ECS.Entities.Player;
using Game.ECS.Services;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Game.ECS.Entities.Factories;

/// <summary>
/// Extension methods for building entity snapshots from the ECS World.
/// Provides convenient access to PlayerSnapshotBuilder and NpcSnapshotBuilder functionality.
/// </summary>
public static class WorldSnapshotExtensions
{
    // Thread-safe cache of builders keyed by World hash code
    // This allows multiple World instances to each have their own builders
    private static readonly ConditionalWeakTable<World, SnapshotBuildersHolder> _buildersCache = new();
    
    /// <summary>
    /// Holds the snapshot builders for a specific World instance.
    /// </summary>
    private sealed class SnapshotBuildersHolder
    {
        public required PlayerSnapshotBuilder PlayerBuilder { get; init; }
        public required NpcSnapshotBuilder NpcBuilder { get; init; }
        public required GameResources Resources { get; init; }
    }

    /// <summary>
    /// Initializes the snapshot builders for the given world and resources.
    /// Should be called once during simulation setup.
    /// </summary>
    public static void InitializeSnapshotBuilders(this World world, GameResources resources)
    {
        var holder = new SnapshotBuildersHolder
        {
            PlayerBuilder = new PlayerSnapshotBuilder(world, resources),
            NpcBuilder = new NpcSnapshotBuilder(world, resources),
            Resources = resources
        };
        _buildersCache.AddOrUpdate(world, holder);
    }
    
    private static SnapshotBuildersHolder GetOrCreateBuilders(World world)
    {
        return _buildersCache.GetValue(world, w =>
        {
            // Create default resources if not explicitly initialized
            var resources = new GameResources();
            return new SnapshotBuildersHolder
            {
                PlayerBuilder = new PlayerSnapshotBuilder(w, resources),
                NpcBuilder = new NpcSnapshotBuilder(w, resources),
                Resources = resources
            };
        });
    }
    
    /// <summary>
    /// Builds a complete player snapshot from the entity.
    /// </summary>
    public static PlayerSnapshot BuildPlayerSnapshot(this World world, Entity entity)
    {
        return GetOrCreateBuilders(world).PlayerBuilder.BuildPlayerSnapshot(entity);
    }
    
    /// <summary>
    /// Builds a player state snapshot (position, velocity, direction) from the entity.
    /// </summary>
    public static PlayerStateSnapshot BuildPlayerStateSnapshot(this World world, Entity entity)
    {
        return GetOrCreateBuilders(world).PlayerBuilder.BuildPlayerStateSnapshot(entity);
    }
    
    /// <summary>
    /// Builds a player vitals snapshot (HP, MP) from the entity.
    /// </summary>
    public static PlayerVitalsSnapshot BuildPlayerVitalsSnapshot(this World world, Entity entity)
    {
        return GetOrCreateBuilders(world).PlayerBuilder.BuildPlayerVitalsSnapshot(entity);
    }
    
    /// <summary>
    /// Builds a complete NPC snapshot from the entity.
    /// </summary>
    public static NpcSnapshot BuildNpcData(this World world, Entity entity)
    {
        return GetOrCreateBuilders(world).NpcBuilder.BuildNpcData(entity);
    }
    
    /// <summary>
    /// Builds an NPC state snapshot (position, velocity, direction) from the entity.
    /// </summary>
    public static NpcStateSnapshot BuildNpcStateData(this World world, Entity entity)
    {
        return GetOrCreateBuilders(world).NpcBuilder.BuildNpcStateData(entity);
    }
    
    /// <summary>
    /// Builds an NPC vitals snapshot (HP, MP) from the entity.
    /// </summary>
    public static NpcVitalsSnapshot BuildNpcVitalsSnapshot(this World world, Entity entity)
    {
        return GetOrCreateBuilders(world).NpcBuilder.BuildNpcVitalsSnapshot(entity);
    }
}
