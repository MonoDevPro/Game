using System;
using Godot;

namespace Game.Core.Autoloads;

/// <summary>
/// Gerencia estado global do jogo (persist entre cenas).
/// Autor: MonoDevPro
/// Data: 2025-01-11 21:16:36
/// </summary>
public partial class GameStateManager : Node
{
    private static GameStateManager? _instance;
    public static GameStateManager Instance => _instance ?? throw new InvalidOperationException("GameStateManager not initialized");

    public string? AuthToken { get; set; }
    public string? EnterTicket { get; set; }
    public string? WorldEndpoint { get; set; }
    public int CharacterId { get; set; } = -1;
    public string? CharacterName { get; set; }
    public bool Connected => CharacterId > -1 && !string.IsNullOrWhiteSpace(WorldEndpoint);
    
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
        AuthToken = null;
        EnterTicket = null;
        WorldEndpoint = null;
        CharacterId = -1;
        CharacterName = null;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        _instance = null;
    }
}
