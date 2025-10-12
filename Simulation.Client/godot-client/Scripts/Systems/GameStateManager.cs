using System;
using Game.Network.Packets;
using Game.Network.Packets.Simulation;
using Godot;

namespace GodotClient.Systems;

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
    public GameDataPacket? CurrentGameData { get; set; }
    
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
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        _instance = null;
    }
}