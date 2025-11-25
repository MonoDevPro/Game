using Arch.Core;
using Arch.System;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Entities.Data;
using Game.ECS.Entities.Factories;
using Game.ECS.Entities.Updates;
using Game.ECS.Logic;
using Game.ECS.Systems;
using Game.Network.Abstractions;
using Godot;
using GodotClient.ECS.Systems;
using GodotClient.Simulation;

namespace GodotClient.ECS;

/// <summary>
/// Exemplo de uso do ECS como CLIENTE.
/// O cliente executa uma simulação local parcial: apenas movimento local, renderização e input.
/// Estado autorizado vem do servidor.
/// </summary>
public sealed class ClientGameSimulation : GameSimulation
{
    private readonly INetworkManager _networkManager;
    private ClientVisualSyncSystem? _visualSyncSystem;

    /// <summary>
    /// Exemplo de uso do ECS como CLIENTE.
    /// O cliente executa uma simulação local parcial: apenas movimento local, renderização e input.
    /// Estado autorizado vem do servidor.
    /// </summary>
    public ClientGameSimulation(INetworkManager networkManager)
    {
        _networkManager = networkManager;
        ConfigureSystems(World, Systems);
    }

    /// <summary>
    /// Configura sistemas apenas para o cliente.
    /// Ordem: Input → Movement (previsão local) → Sync (recebe correções do servidor)
    /// </summary>
    protected override void ConfigureSystems(World world, Group<float> systems)
    {
        // Sistemas de entrada do jogador
        systems.Add(new GodotInputSystem(world));
        
        // Sync de nós visuais
        _visualSyncSystem = new ClientVisualSyncSystem(world, GameClient.Instance.EntitiesRoot);
        systems.Add(_visualSyncSystem);
        
        // Spatial updates
        systems.Add(new SpatialSyncSystem(world, MapService));
        
        // Sincronização com o servidor
        systems.Add(new NetworkSyncSystem(world, _networkManager));
    }
    
    public bool ApplyPlayerState(PlayerStateData data) => World.ApplyPlayerState(PlayerIndex, data);
    public bool ApplyPlayerVitals(PlayerVitalsData data) => World.ApplyPlayerVitals(PlayerIndex, data);
    
    // Visual
    public bool TryGetPlayerVisual(int networkId, out PlayerVisual visual)
    {
        if (_visualSyncSystem != null) 
            return _visualSyncSystem.TryGetPlayerVisual(networkId, out visual);
        visual = null!;
        return false;
    }

    public bool TryGetNpcVisual(int networkId, out NpcVisual visual)
    {
        if (_visualSyncSystem != null)
            return _visualSyncSystem.TryGetNpcVisual(networkId, out visual);
        visual = null!;
        return false;
    }
    
    public bool TryGetAnyVisual(int networkId, out DefaultVisual visual)
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

    public Entity CreateLocalPlayer(in PlayerData data, PlayerVisual visual)
    {
        GD.Print($"[GameClient] Spawning player visual for '{data.Name}' (NetID: {data.NetworkId}, Local: {true})");
        var entity = base.CreatePlayer(data);
        World.Add<LocalPlayerTag>(entity);
        _visualSyncSystem?.RegisterPlayerVisual(data.NetworkId, visual);
        visual.UpdateFromSnapshot(in data);
        visual.MakeCamera();
        return entity;
    }
    
    public Entity CreateRemotePlayer(in PlayerData data, PlayerVisual visual)
    {
        GD.Print($"[GameClient] Spawning player visual for '{data.Name}' (NetID: {data.NetworkId}, Local: {false})");
        var entity = base.CreatePlayer(data);
        World.Add<RemotePlayerTag>(entity);
        _visualSyncSystem?.RegisterPlayerVisual(data.NetworkId, visual);
        visual.UpdateFromSnapshot(data);
        return entity;
    }
    
    public override bool DestroyPlayer(int networkId)
    {
        _visualSyncSystem?.UnregisterPlayerVisual(networkId);
        return base.DestroyPlayer(networkId);
    }

    public Entity CreateNpc(in NPCData data, NpcVisual visual)
    {
        var entity = base.CreateNpc(data, new NpcBehaviorData());
        _visualSyncSystem?.RegisterNpcVisual(data.NetworkId, visual);
        visual.UpdateFromSnapshot(data);
        return entity;
    }

    public override bool DestroyNpc(int networkId)
    {
        _visualSyncSystem?.UnregisterNpcVisual(networkId);
        return base.DestroyNpc(networkId);
    }

    public bool UpdateNpcState(in NpcStateData state)
    {
        return NpcIndex.TryGetEntity(state.NetworkId, out var entity) && 
               World.ApplyNpcState(entity, state);
    }
    
    public bool UpdateNpcVitals(in NpcVitalsData data)
    {
        return NpcIndex.TryGetEntity(data.NetworkId, out var entity) && 
               World.ApplyNpcVitals(entity, data);
    }
}