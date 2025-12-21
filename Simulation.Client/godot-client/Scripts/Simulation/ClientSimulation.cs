using Arch.Core;
using Arch.System;
using Game.ECS.Client.Client;
using Game.ECS.Client.Client.Components;
using Game.ECS.Client.Client.Systems;
using Game.ECS.Shared;
using Game.ECS.Shared.Components.Combat;
using Game.ECS.Shared.Components.Entities;
using Game.ECS.Shared.Core.Entities;
using Game.ECS.Shared.Services.Network;
using Godot;
using GodotClient.Simulation.Contracts;
using GodotClient.Simulation.Systems;

namespace GodotClient.Simulation;

/// <summary>
/// Exemplo de uso do ECS como CLIENTE.
/// O cliente executa uma simulação local parcial: apenas movimento local, renderização e input.
/// Estado autorizado vem do servidor.
/// </summary>
public sealed class ClientSimulation : GameSimulation
{
    private readonly INetworkManager _networkManager;
    
    // Modules
    public ClientNavigationModule? NavigationModule;
    
    // Systems
    private ClientVisualSyncSystem? _visualSyncSystem;
    private ClientSyncSystem? _clientSyncSystem;
    
    public override void Update(in float deltaTime)
    {
        base.Update(in deltaTime);
        
        // Atualiza o módulo de navegação do cliente
        NavigationModule?.Update(deltaTime);
    }
    
    /// <summary>
    /// Exemplo de uso do ECS como CLIENTE.
    /// O cliente executa uma simulação local parcial: apenas movimento local, renderização e input.
    /// Estado autorizado vem do servidor.
    /// </summary>
    public ClientSimulation(INetworkManager networkManager) 
        : base(null)
    {
        _networkManager = networkManager;
        
        ConfigureSystems(World, Systems);
        
        // Inicializa os sistemas
        Systems.Initialize();
    }

    /// <summary>
    /// Configura sistemas apenas para o cliente.
    /// Ordem: Input → Visual Sync → Network Sync
    /// O cliente não executa sistemas de movimento/combate - recebe estado do servidor.
    /// </summary>
    protected override void ConfigureSystems(World world, Group<float> systems)
    {
        // Sistemas de entrada do jogador
        var inputProvider = new GodotInputProvider();
        NavigationModule = new ClientNavigationModule(
            world,
            cellSize: 32f,
            tickRate: 60f,
            inputProvider: inputProvider,
            networkSender: _networkManager);
        
        // Sync de nós visuais (interpolação e animação)
        _visualSyncSystem = new ClientVisualSyncSystem(world, GameClient.Instance.EntitiesRoot);
        
        // Sistemas de rede (recebe estado do servidor)
        _clientSyncSystem = new ClientSyncSystem(
            world: world,
            tickRate: 60f,
            cellSize: 32f);
        
        systems.Add(_visualSyncSystem);
        systems.Add(_clientSyncSystem);
        
    }
    
    public Entity CreateLocalPlayer(ref PlayerData snapshot, Visuals.PlayerVisual visual)
    {
        GD.Print($"[GameClient] Spawning player visual for '{snapshot.Name}' (NetID: {snapshot.Id}, Local: {true})");
        var entity = NavigationModule?.CreateEntity(
            serverId: snapshot.Id,
            gridX: snapshot.X,
            gridY: snapshot.Y,
            isLocalPlayer: true);
        _visualSyncSystem?.RegisterPlayerVisual(snapshot.Id, visual);
        visual.UpdateFromSnapshot(in snapshot);
        visual.MakeCamera();
        return entity ?? default;
    }
    
    public Entity CreateRemotePlayer(ref PlayerData snapshot, Visuals.PlayerVisual visual)
    {
        GD.Print($"[GameClient] Spawning player visual for '{snapshot.Name}' (NetID: {snapshot.Id}, Local: {false})");
        var entity = NavigationModule?.CreateEntity(
            serverId: snapshot.Id,
            gridX: snapshot.X,
            gridY: snapshot.Y,
            isLocalPlayer: false);
        _visualSyncSystem?.RegisterPlayerVisual(snapshot.Id, visual);
        visual.UpdateFromSnapshot(snapshot);
        return entity ?? default;
    }
    
    public Entity CreateNpc(ref NpcData snapshot, Visuals.NpcVisual visual)
    {
        var defaultBehaviour = AIBehaviour.Default;
        var entity = NavigationModule?.CreateEntity(
            serverId: snapshot.Id,
            gridX: snapshot.X,
            gridY: snapshot.Y,
            isLocalPlayer: false,
            settings: new ClientVisualConfig
            {
                CellSize = 32f,
                InterpolationSpeed = 2f,
                SmoothMovement = true,
                Easing = EasingType.QuadInOut
            });
        _visualSyncSystem?.RegisterNpcVisual(snapshot.Id, visual);
        visual.UpdateFromSnapshot(snapshot);
        return entity ?? default;
    }

    public void DestroyAny(int id)
    {
        _visualSyncSystem?.UnregisterAnyVisual(id);
        NavigationModule?.DestroyEntity(id);
    }
}