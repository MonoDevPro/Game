using Arch.Core;
using Arch.LowLevel;
using Game.ECS.Components;
using Game.ECS.Entities.Npc;
using Game.ECS.Entities.Player;
using Game.ECS.Services;

namespace Game.ECS.Entities.Factories;

/// <summary>
/// Extension methods for building entity snapshots from the ECS World.
/// Provides convenient access to PlayerSnapshotBuilder and NpcSnapshotBuilder functionality.
/// </summary>
public static class WorldSnapshotExtensions
{
    // Thread-local builders to avoid allocations (note: this assumes single-threaded usage per World)
    // In a multi-threaded scenario, consider using a dictionary keyed by World
    [ThreadStatic]
    private static PlayerSnapshotBuilder? _playerSnapshotBuilder;
    
    [ThreadStatic]
    private static NpcSnapshotBuilder? _npcSnapshotBuilder;
    
    [ThreadStatic]
    private static GameResources? _resources;
    
    [ThreadStatic]
    private static World? _currentWorld;

    /// <summary>
    /// Initializes the snapshot builders for the given world and resources.
    /// Should be called once during simulation setup.
    /// </summary>
    public static void InitializeSnapshotBuilders(this World world, GameResources resources)
    {
        _currentWorld = world;
        _resources = resources;
        _playerSnapshotBuilder = new PlayerSnapshotBuilder(world, resources);
        _npcSnapshotBuilder = new NpcSnapshotBuilder(world, resources);
    }
    
    private static void EnsureInitialized(World world)
    {
        if (_currentWorld != world || _resources == null)
        {
            // Auto-initialize with a new GameResources if not yet initialized
            // This is a fallback - ideally InitializeSnapshotBuilders should be called explicitly
            _currentWorld = world;
            _resources ??= new GameResources();
            _playerSnapshotBuilder = new PlayerSnapshotBuilder(world, _resources);
            _npcSnapshotBuilder = new NpcSnapshotBuilder(world, _resources);
        }
    }
    
    /// <summary>
    /// Builds a complete player snapshot from the entity.
    /// </summary>
    public static PlayerSnapshot BuildPlayerSnapshot(this World world, Entity entity)
    {
        EnsureInitialized(world);
        return _playerSnapshotBuilder!.BuildPlayerSnapshot(entity);
    }
    
    /// <summary>
    /// Builds a player state snapshot (position, velocity, direction) from the entity.
    /// </summary>
    public static PlayerStateSnapshot BuildPlayerStateSnapshot(this World world, Entity entity)
    {
        EnsureInitialized(world);
        return _playerSnapshotBuilder!.BuildPlayerStateSnapshot(entity);
    }
    
    /// <summary>
    /// Builds a player vitals snapshot (HP, MP) from the entity.
    /// </summary>
    public static PlayerVitalsSnapshot BuildPlayerVitalsSnapshot(this World world, Entity entity)
    {
        EnsureInitialized(world);
        return _playerSnapshotBuilder!.BuildPlayerVitalsSnapshot(entity);
    }
    
    /// <summary>
    /// Builds a complete NPC snapshot from the entity.
    /// </summary>
    public static NpcSnapshot BuildNpcData(this World world, Entity entity)
    {
        EnsureInitialized(world);
        return _npcSnapshotBuilder!.BuildNpcData(entity);
    }
    
    /// <summary>
    /// Builds an NPC state snapshot (position, velocity, direction) from the entity.
    /// </summary>
    public static NpcStateSnapshot BuildNpcStateData(this World world, Entity entity)
    {
        EnsureInitialized(world);
        return _npcSnapshotBuilder!.BuildNpcStateData(entity);
    }
    
    /// <summary>
    /// Builds an NPC vitals snapshot (HP, MP) from the entity.
    /// </summary>
    public static NpcVitalsSnapshot BuildNpcVitalsSnapshot(this World world, Entity entity)
    {
        EnsureInitialized(world);
        return _npcSnapshotBuilder!.BuildNpcVitalsSnapshot(entity);
    }
}
