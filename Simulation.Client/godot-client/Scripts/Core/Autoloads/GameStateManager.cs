using System;
using System.Collections.Generic;
using System.Linq;
using GameECS.Modules.Entities.Shared.Data;
using Godot;

namespace GodotClient.Core.Autoloads;

/// <summary>
/// Gerencia estado global do jogo (persist entre cenas).
/// Autor: MonoDevPro
/// Data: 2025-01-11 21:16:36
/// </summary>
public partial class GameStateManager : Node
{
    private static GameStateManager? _instance;
    public static GameStateManager Instance => _instance ?? throw new InvalidOperationException("GameStateManager not initialized");

    public int LocalNetworkId { get; set; } = -1;
    public bool Connected => LocalNetworkId > -1;
    public PlayerJoinPacket? CurrentGameData { get; set; }
    private readonly Dictionary<int, PlayerData> _pendingPlayerSpawns = new();
    private readonly Dictionary<int, NpcData> _pendingNpcSpawns = new();
    
    public override void _Ready()
    {
        base._Ready();
        _instance = this;
        
        GD.Print("[GameStateManager] Initialized");
    }
    
    /// <summary>
    /// Reseta estado ao desconectar.
    /// </summary>
    public void ResetState()
    {
        GD.Print("[GameStateManager] State reset");
        CurrentGameData = null;
        LocalNetworkId = -1;
        _pendingPlayerSpawns.Clear();
        _pendingNpcSpawns.Clear();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        _instance = null;
    }

    /// <summary>
    /// Stores player spawn snapshots received before the gameplay scene is ready.
    /// When a player snapshot with the same NetworkId already exists, it's replaced.
    /// </summary>
    /// <param name="snapshots">Snapshots to buffer until consumption.</param>
    public void StorePlayerSnapshots(IEnumerable<PlayerData> snapshots)
    {
        foreach (var snapshot in snapshots)
            _pendingPlayerSpawns[snapshot.NetworkId] = snapshot;
    }

    /// <summary>
    /// Stores NPC spawn snapshots received before the gameplay scene is ready.
    /// When an NPC snapshot with the same NetworkId already exists, it's replaced.
    /// </summary>
    /// <param name="snapshots">Snapshots to buffer until consumption.</param>
    public void StoreNpcSnapshots(IEnumerable<NpcData> snapshots)
    {
        foreach (var snapshot in snapshots)
            _pendingNpcSpawns[snapshot.NetworkId] = snapshot;
    }

    /// <summary>
    /// Returns all buffered player snapshots and clears the buffer.
    /// </summary>
    public PlayerData[] ConsumePlayerSnapshots()
    {
        if (_pendingPlayerSpawns.Count == 0)
            return [];

        var result = _pendingPlayerSpawns.Values.ToArray();
        _pendingPlayerSpawns.Clear();
        return result;
    }

    /// <summary>
    /// Returns all buffered NPC snapshots and clears the buffer.
    /// </summary>
    public NpcData[] ConsumeNpcSnapshots()
    {
        if (_pendingNpcSpawns.Count == 0)
            return [];

        var result = _pendingNpcSpawns.Values.ToArray();
        _pendingNpcSpawns.Clear();
        return result;
    }
}