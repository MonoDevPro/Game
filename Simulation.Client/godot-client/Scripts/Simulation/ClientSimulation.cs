using Arch.Core;
using Arch.System;
using Game.DTOs.Npc;
using Game.DTOs.Player;
using Game.ECS;
using Game.ECS.Entities;
using Game.ECS.Services;
using Game.Network.Abstractions;
using Godot;
using GodotClient.Simulation.Components;
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
    private ClientVisualSyncSystem? _visualSyncSystem;
    
    private readonly EntityIndex<int> _networkIndex = new();
    
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
        systems.Add(new GodotInputSystem(world));
        
        // Sync de nós visuais (interpolação e animação)
        _visualSyncSystem = new ClientVisualSyncSystem(world, GameClient.Instance.EntitiesRoot);
        systems.Add(_visualSyncSystem);
        
        // Sincronização com o servidor (envia input)
        systems.Add(new NetworkSyncSystem(world, _networkManager));
    }
    
    // Visual
    public bool TryGetVisual(int networkId, out Visuals.DefaultVisual visual)
    {
        if (_visualSyncSystem != null)
        {
            if (_visualSyncSystem.TryGetAnyVisual(networkId, out var playerVisual))
            {
                visual = playerVisual;
                return true;
            }
        }
        visual = null!;
        return false;
    }
    
    public Entity CreatePlayer(ref PlayerSnapshot playerSnapshot)
    {
        var entity = World.CreatePlayer(ref playerSnapshot);
        _networkIndex.Register(playerSnapshot.NetworkId, entity);
        return entity;
    }
    
    /// <summary>
    /// Tenta obter a entidade de um jogador pelo NetworkId.
    /// </summary>
    public bool TryGetEntity(int networkId, out Entity entity) =>
        _networkIndex.TryGetEntity(networkId, out entity);
    
    /// <summary>
    /// Destrói a entidade de um jogador pelo NetworkId.
    /// </summary>
    public bool DestroyEntity(int networkId)
    {
        if (!_networkIndex.TryGetEntity(networkId, out var entity))
            return false;
        
        _visualSyncSystem?.UnregisterVisual(networkId);
        _networkIndex.RemoveByKey(networkId);
        World.Destroy(entity);
        return true;
    }

    public Entity CreateLocalPlayer(ref PlayerSnapshot snapshot, Visuals.PlayerVisual visual)
    {
        GD.Print($"[GameClient] Spawning player visual for '{snapshot.Name}' (NetID: {snapshot.NetworkId}, Local: {true})");
        var entity = CreatePlayer(ref snapshot);
        World.Add<LocalPlayerTag, DirtyFlags>(entity);
        _visualSyncSystem?.RegisterVisual(snapshot.NetworkId, visual);
        visual.UpdateFromSnapshot(in snapshot);
        visual.MakeCamera();
        return entity;
    }
    
    public Entity CreateRemotePlayer(ref PlayerSnapshot snapshot, Visuals.PlayerVisual visual)
    {
        GD.Print($"[GameClient] Spawning player visual for '{snapshot.Name}' (NetID: {snapshot.NetworkId}, Local: {false})");
        var entity = CreatePlayer(ref snapshot);
        World.Add<RemotePlayerTag>(entity);
        _visualSyncSystem?.RegisterVisual(snapshot.NetworkId, visual);
        visual.UpdateFromSnapshot(snapshot);
        return entity;
    }

    public Entity CreateNpc(ref NpcData snapshot, Visuals.NpcVisual visual)
    {
        var defaultBehaviour = Behaviour.Default;
        
        // Atualiza o template com a localização de spawn e networkId
        var entity = World.CreateNpc(ref snapshot, ref defaultBehaviour);
        _networkIndex.Register(snapshot.NetworkId, entity);
        _visualSyncSystem?.RegisterVisual(snapshot.NetworkId, visual);
        visual.UpdateFromSnapshot(snapshot);
        return entity;
    }
    
    public void DestroyAny(int networkId)
    {
        if (TryGetEntity(networkId, out _))
        {
            DestroyEntity(networkId);
        }
    }
    
    public void ApplyState(ref StateSnapshot stateSnapshot)
    {
        if (!TryGetEntity(stateSnapshot.NetworkId, out Entity entity))
        {
            GD.PrintErr($"[GameClient] Cannot apply state: entity with NetworkId {stateSnapshot.NetworkId} not found.");
            return;
        }
        
        World.UpdateState(entity, ref stateSnapshot);
    }
    
    public void ApplyVitals(ref VitalsSnapshot vitalsSnapshot)
    {
        if (!TryGetEntity(vitalsSnapshot.NetworkId, out var entity))
        {
            GD.PrintErr($"[GameClient] Cannot apply vitals: entity with NetworkId {vitalsSnapshot.NetworkId} not found.");
            return;
        }
        World.UpdateVitals(entity, ref vitalsSnapshot);
    }
    
}