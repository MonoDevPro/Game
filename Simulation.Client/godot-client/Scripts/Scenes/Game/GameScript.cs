using System.Collections.Generic;
using Game.Domain.VOs;
using Game.Network.Abstractions;
using Game.Network.Packets.DTOs;
using Game.Network.Packets.Simulation;
using Godot;
using GodotClient.Systems;
using GodotClient.Visuals;

namespace GodotClient.Scenes.Game;

/// <summary>
/// Cliente principal do jogo - Gerencia gameplay após autenticação.
/// Carrega dados do GameStateManager recebidos no menu.
/// Autor: MonoDevPro
/// Data: 2025-10-12 02:13:16
/// </summary>
public partial class GameScript : Node
{
    private INetworkManager? _network;
    private readonly Dictionary<int, PlayerSnapshot> _players = new();
    private PlayerView? _playerView;
    private InputManager? _inputManager;
    
    private int _localNetworkId;
    private Label? _statusLabel;

    public bool CanSendInput => _network?.IsRunning == true && _localNetworkId > -1;
    public AnimatedPlayerVisual? GetLocalPlayer => _playerView?.GetLocalPlayer();

    public override void _Ready()
    {
        base._Ready();

        CreateStatusLabel();
        
        // ✅ Obtém NetworkManager já inicializado no Menu
        _network = NetworkClient.Instance.NetworkManager;
        
        // ✅ Obtém referências das nodes
        _inputManager = GetNode<InputManager>(nameof(InputManager));
        _playerView = GetNode<PlayerView>(nameof(PlayerView));
        
        // ✅ Conecta input manager
        _inputManager.Attach(this);
        
        // ✅ Carrega dados iniciais do GameStateManager
        LoadGameData();
        
        // ✅ Registra handlers de gameplay
        RegisterPacketHandlers();
        
        GD.Print("[GameClient] Ready");
    }

    /// <summary>
    /// Cria label de status (HUD temporário para debug).
    /// </summary>
    private void CreateStatusLabel()
    {
        var hudLayer = new CanvasLayer { Name = "HudLayer", Layer = 100 };
        
        _statusLabel = new Label
        {
            Name = "StatusLabel",
            Position = new Vector2(12, 12),
            Text = "Loading game..."
        };
        _statusLabel.AddThemeColorOverride("font_color", Colors.White);
        
        hudLayer.AddChild(_statusLabel);
        AddChild(hudLayer);
    }

    /// <summary>
    /// ✅ Carrega dados do jogo salvos no GameStateManager (recebidos no Menu).
    /// </summary>
    private void LoadGameData()
    {
        var gameState = GameStateManager.Instance;
        
        if (!gameState.Connected)
        {
            GD.PushError("[GameClient] No game data available! Returning to menu...");
            UpdateStatus("Error: No game data");
            _ = SceneManager.Instance.LoadMainMenu();
            return;
        }

        _localNetworkId = gameState.LocalNetworkId;
        
        // Adiciona player local
        var localPlayer = gameState.CurrentGameData?.LocalPlayer;
        if (localPlayer is null)
        {
            GD.PushError("[GameClient] Local player data is null! Returning to menu...");
            UpdateStatus("Error: Local player data is null");
            _ = SceneManager.Instance.LoadMainMenu();
            return;
        }
        
        _players[_localNetworkId] = localPlayer.Value;
        
        _playerView?.SetLocalPlayer(localPlayer.Value);

        // Adiciona outros players
        foreach (var snapshot in gameState.CurrentGameData?.OtherPlayers ?? [])
        {
            if (_players.TryAdd(snapshot.NetworkId, snapshot))
            {
                _playerView?.ApplySnapshot(snapshot, false);
            }
        }

        UpdateStatus($"Playing as '{gameState.CurrentGameData?.LocalPlayer.Name}' (NetID: {_localNetworkId})");
        
        GD.Print($"[GameClient] Game data loaded:");
        GD.Print($"  LocalPlayer: {gameState.CurrentGameData?.LocalPlayer.Name} (NetID: {_localNetworkId})");
        GD.Print($"  Other Players: {gameState.CurrentGameData?.OtherPlayers.Length}");
        
        // Reseta estado global (não mais necessário)
        gameState.ResetState();
    }

    /// <summary>
    /// ✅ Registra handlers de pacotes de gameplay.
    /// NÃO registra GameDataPacket (já foi processado no Menu).
    /// </summary>
    private void RegisterPacketHandlers()
    {
        if (_network is null)
        {
            GD.PushError("[GameClient] NetworkManager is null!");
            return;
        }
        
        _network.OnPeerDisconnected += OnPeerDisconnected;

        _network.RegisterPacketHandler<PlayerMovementPacket>(HandlePlayerMovement);
        _network.RegisterPacketHandler<PlayerVitalsPacket>(HandlePlayerVitals);
        _network.RegisterPacketHandler<PlayerSpawnPacket>(HandlePlayerSpawn);
        _network.RegisterPacketHandler<PlayerDespawnPacket>(HandlePlayerDespawn);
        
        GD.Print("[GameClient] Packet handlers registered");
    }

    // ========== HANDLERS DE PACOTES ==========

    /// <summary>
    /// Handler de movimento de jogadores.
    /// </summary>
    private void HandlePlayerMovement(INetPeerAdapter peer, PlayerMovementPacket packet)
    {
        if (_players.TryGetValue(packet.NetworkId, out var snapshot))
        {
            // Atualiza snapshot local
            snapshot = snapshot with 
            { 
                Position = packet.Position, 
                Facing = packet.Facing 
            };
            _players[packet.NetworkId] = snapshot;
            
            // Atualiza visualmente
            _playerView?.UpdateMovement(packet.NetworkId, packet.Position, packet.Facing);
        }
    }

    /// <summary>
    /// Handler de vitals (HP/MP).
    /// </summary>
    private void HandlePlayerVitals(INetPeerAdapter peer, PlayerVitalsPacket packet)
    {
        _playerView?.UpdateVitals(
            packet.NetworkId, 
            packet.CurrentHp, 
            packet.MaxHp, 
            packet.CurrentMp, 
            packet.MaxMp);
    }

    /// <summary>
    /// Handler de player entrando no jogo.
    /// </summary>
    private void HandlePlayerSpawn(INetPeerAdapter peer, PlayerSpawnPacket packet)
    {
        GD.Print($"[GameClient] Player joined: {packet.Player.Name} (NetID: {packet.Player.NetworkId})");
        
        if (_players.TryAdd(packet.Player.NetworkId, packet.Player))
        {
            _playerView?.ApplySnapshot(packet.Player, false);
            UpdateStatus($"{packet.Player.Name} joined the game");
        }
    }

    /// <summary>
    /// Handler de player saindo do jogo.
    /// </summary>
    private void HandlePlayerDespawn(INetPeerAdapter peer, PlayerDespawnPacket packet)
    {
        GD.Print($"[GameClient] Player left: NetID {packet.NetworkId}");
        
        if (_players.Remove(packet.NetworkId, out var leftPlayer))
        {
            _playerView?.RemovePlayer(packet.NetworkId);
            UpdateStatus($"{leftPlayer.Name} left the game");
            
            if (packet.NetworkId == _localNetworkId)
            {
                GD.PushWarning("[GameClient] You have been disconnected from the server!");
                UpdateStatus("You have been disconnected from the server");
                DisconnectAndReturnToMenu();
            }
        }
    }

    // ========== INPUT ==========

    /// <summary>
    /// ✅ Envia input do jogador para o servidor.
    /// </summary>
    public void QueueInput(GridOffset movement, GridOffset mouseLook, ushort buttons)
    {
        var packet = new PlayerInputPacket(
            movement,
            mouseLook,
            buttons);
        _network?.SendToServer(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.Sequenced);
    }

    // ========== DESCONEXÃO ==========

    /// <summary>
    /// ✅ Desconecta e retorna ao menu principal.
    /// </summary>
    public void DisconnectAndReturnToMenu()
    {
        GD.Print("[GameClient] Disconnecting...");

        UpdateStatus("Disconnecting...");

        // Limpa estado local
        _players.Clear();
        _playerView?.Clear();

        // Para rede
        if (_network?.IsRunning == true)
        {
            _network.Stop();
        }

        // Reseta estado global
        GameStateManager.Instance.ResetState();

        // Volta ao menu
        SceneManager.Instance.LoadMainMenu();
    }

    // ========== INPUT DE UI ==========

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        // ESC = Desconectar e voltar ao menu
        if (@event.IsActionPressed("ui_cancel"))
        {
            DisconnectAndReturnToMenu();
        }
    }

    // ========== CLEANUP ==========

    public override void _ExitTree()
    {
        base._ExitTree();
        
        // Desconecta input
        _inputManager?.Detach();
        
        // ✅ Desregistra handlers
        if (_network is not null)
        {
            _network.OnPeerDisconnected -= OnPeerDisconnected;
            
            _network.UnregisterPacketHandler<PlayerMovementPacket>();
            _network.UnregisterPacketHandler<PlayerVitalsPacket>();
            _network.UnregisterPacketHandler<PlayerSpawnPacket>();
            _network.UnregisterPacketHandler<PlayerDespawnPacket>();
        }
        
        GD.Print("[GameClient] Unloaded");
    }
    
    private void OnPeerDisconnected(INetPeerAdapter peer)
    {
        GD.Print("[GameClient] Disconnected from server");
        UpdateStatus("Disconnected from server");
        DisconnectAndReturnToMenu();
    }

    // ========== UTILIDADES ==========

    private void UpdateStatus(string message)
    {
        if (_statusLabel is not null)
        {
            _statusLabel.Text = message;
        }
        GD.Print($"[GameClient] {message}");
    }
}