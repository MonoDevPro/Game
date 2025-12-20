using Arch.Core;
using Arch.System;
using Game.DTOs.Game.Npc;
using Game.DTOs.Game.Player;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Entities;
using Game.ECS.Navigation.Client;
using Game.ECS.Navigation.Client.Systems;
using Game.Network.Abstractions;
using Godot;
using GodotClient.Simulation.Components;
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
        var networkSender = new ClientNetworkSender(_networkManager);
        NavigationModule = new ClientNavigationModule(
            world,
            cellSize: 32f,
            tickRate: 60f,
            inputProvider: inputProvider,
            networkSender: networkSender);
        
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
    
    // Visual
    public bool TryGetPlayerVisual(int networkId, out Visuals.PlayerVisual visual)
    {
        if (_visualSyncSystem != null) 
            return _visualSyncSystem.TryGetPlayerVisual(networkId, out visual);
        visual = null!;
        return false;
    }

    public bool TryGetNpcVisual(int networkId, out Visuals.NpcVisual visual)
    {
        if (_visualSyncSystem != null)
            return _visualSyncSystem.TryGetNpcVisual(networkId, out visual);
        visual = null!;
        return false;
    }
    
    public bool TryGetAnyVisual(int networkId, out Visuals.DefaultVisual visual)
    {
        if (_visualSyncSystem != null)
        {
            if (_visualSyncSystem.TryGetPlayerVisual(networkId, out var playerVisual))
            {
                visual = playerVisual;
                return true;
            }

            if (_visualSyncSystem.TryGetNpcVisual(networkId, out var npcVisual))
            {
                visual = npcVisual;
                return true;
            }
        }

        visual = null!;
        return false;
    }
    
    public bool TryGetAnyEntity(int networkId, out Entity entity)
    {
        if (TryGetPlayerEntity(networkId, out entity))
            return true;
        if (TryGetNpcEntity(networkId, out entity))
            return true;
        entity = default;
        return false;
    }

    public Entity CreateLocalPlayer(ref PlayerData snapshot, Visuals.PlayerVisual visual)
    {
        GD.Print($"[GameClient] Spawning player visual for '{snapshot.Name}' (NetID: {snapshot.NetworkId}, Local: {true})");
        var entity = NavigationModule?.CreateEntity(
            serverId: snapshot.NetworkId,
            gridX: snapshot.X,
            gridY: snapshot.Y,
            isLocalPlayer: true);
        _visualSyncSystem?.RegisterPlayerVisual(snapshot.NetworkId, visual);
        visual.UpdateFromSnapshot(in snapshot);
        visual.MakeCamera();
        return entity ?? default;
    }
    
    public Entity CreateRemotePlayer(ref PlayerData snapshot, Visuals.PlayerVisual visual)
    {
        GD.Print($"[GameClient] Spawning player visual for '{snapshot.Name}' (NetID: {snapshot.NetworkId}, Local: {false})");
        var entity = NavigationModule?.CreateEntity(
            serverId: snapshot.NetworkId,
            gridX: snapshot.X,
            gridY: snapshot.Y,
            isLocalPlayer: false);
        _visualSyncSystem?.RegisterPlayerVisual(snapshot.NetworkId, visual);
        visual.UpdateFromSnapshot(snapshot);
        return entity ?? default;
    }
    
    public override bool DestroyPlayer(int networkId)
    {
        _visualSyncSystem?.UnregisterPlayerVisual(networkId);
        return base.DestroyPlayer(networkId);
    }

    public Entity CreateNpc(ref NpcData snapshot, Visuals.NpcVisual visual)
    {
        var defaultBehaviour = Behaviour.Default;
        var entity = base.CreateNpc(ref snapshot, ref defaultBehaviour);
        _visualSyncSystem?.RegisterNpcVisual(snapshot.NetworkId, visual);
        visual.UpdateFromSnapshot(snapshot);
        return entity;
    }

    public override bool DestroyNpc(int networkId)
    {
        _visualSyncSystem?.UnregisterNpcVisual(networkId);
        return base.DestroyNpc(networkId);
    }
    
    public void DestroyAny(int networkId)
    {
        if (TryGetPlayerEntity(networkId, out _))
        {
            DestroyPlayer(networkId);
        }
        else if (TryGetNpcEntity(networkId, out _))
        {
            DestroyNpc(networkId);
        }
    }
    
    public void ApplyVitals(ref VitalsData vitalsSnapshot)
    {
        if (!TryGetPlayerEntity(vitalsSnapshot.NetworkId, out var entity) &&
            !TryGetNpcEntity(vitalsSnapshot.NetworkId, out entity))
        {
            GD.PrintErr($"[GameClient] Cannot apply vitals: entity with NetworkId {vitalsSnapshot.NetworkId} not found.");
            return;
        }
        
        World.Set<Health, Mana>(entity,
            new Health { Current = vitalsSnapshot.CurrentHp, Max = vitalsSnapshot.MaxHp },
            new Mana { Current = vitalsSnapshot.CurrentMp, Max = vitalsSnapshot.MaxMp });
        
    }
    
}