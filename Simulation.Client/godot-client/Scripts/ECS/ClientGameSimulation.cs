using Arch.Core;
using Arch.System;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Entities.Data;
using Game.ECS.Entities.Factories;
using Game.ECS.Entities.Updates;
using Game.ECS.Logic;
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
    
        // Sincronização com o servidor
        systems.Add(new NetworkSyncSystem(world, _networkManager));
    }
    
    public bool ApplyPlayerState(PlayerStateData data) => World.ApplyEntityState(PlayerIndex, data);
    public bool ApplyPlayerVitals(PlayerVitalsData data) => World.ApplyEntityVitals(PlayerIndex, data);
    public bool DespawnPlayer(int networkId) => DestroyPlayer(networkId);
    public Entity SpawnLocalPlayer(PlayerData data, PlayerVisual visual) 
        => CreateLocalPlayer(data, visual);
    public Entity SpawnRemotePlayer(PlayerData data, PlayerVisual visual) 
        => CreateRemotePlayer(data, visual);
    public Entity SpawnNpc(NPCData data, NpcVisual visual) => CreateNpc(data, visual);
    public bool DespawnNpc(int networkId) => DestroyNpc(networkId);
    public void ApplyNpcState(NpcStateData state) => UpdateNpcState(state);
    public void ApplyNpcVitals(NpcVitalsData data) => UpdateNpcVitals(data);
    
    // Visual
    public bool TryGetPlayerVisual(int networkId, out PlayerVisual visual)
    {
        if (_visualSyncSystem != null) 
            return _visualSyncSystem.TryGetPlayerVisual(networkId, out visual!);
        visual = null!;
        return false;
    }

    public bool TryGetNpcVisual(int networkId, out NpcVisual visual)
    {
        if (_visualSyncSystem != null)
            return _visualSyncSystem.TryGetNpcVisual(networkId, out visual!);
        visual = null!;
        return false;
    }

    private Entity CreateLocalPlayer(in PlayerData data, PlayerVisual visual)
    {
        var entity = World.CreatePlayer(PlayerIndex, data);
        World.Add<LocalPlayerTag>(entity, new LocalPlayerTag());
        RegisterSpatial(entity);
        _visualSyncSystem?.RegisterPlayerVisual(data.NetworkId, visual);
        visual.UpdateFromSnapshot(data);
        return entity;
    }
    private Entity CreateRemotePlayer(in PlayerData data, PlayerVisual visual)
    {
        var entity = World.CreatePlayer(PlayerIndex, data);
        World.Add<RemotePlayerTag>(entity, new RemotePlayerTag());
        RegisterSpatial(entity);
        _visualSyncSystem?.RegisterPlayerVisual(data.NetworkId, visual);
        visual.UpdateFromSnapshot(data);
        return entity;
    }
    private bool DestroyPlayer(int networkId)
    {
        if (!PlayerIndex.TryGetEntity(networkId, out var entity))
            return false;
        UnregisterSpatial(entity);
        _visualSyncSystem?.UnregisterPlayerVisual(networkId);
        return World.TryDestroyPlayer(PlayerIndex, networkId);;
    }

    private Entity CreateNpc(in NPCData data, NpcVisual visual)
    {
        var entity = World.CreateNPC(data, default(NpcBehaviorData));
        NpcIndex.AddMapping(data.NetworkId, entity);
        RegisterSpatial(entity);
        _visualSyncSystem?.RegisterNpcVisual(data.NetworkId, visual);
        visual.UpdateFromSnapshot(data);
        return entity;
    }

    private bool DestroyNpc(int networkId)
    {
        if (!NpcIndex.TryGetEntity(networkId, out var entity))
            return false;

        UnregisterSpatial(entity);
        _visualSyncSystem?.UnregisterNpcVisual(networkId);
        NpcIndex.RemoveByKey(networkId);
        World.Destroy(entity);
        return true;
    }

    private void UpdateNpcState(in NpcStateData state)
    {
        if (!NpcIndex.TryGetEntity(state.NetworkId, out var entity))
            return;
        World.ApplyNpcState(entity, state);
    }
    
    private void UpdateNpcVitals(in NpcVitalsData data)
    {
        if (!NpcIndex.TryGetEntity(data.NetworkId, out var entity))
            return;
        World.ApplyNpcVitals(entity, data);
    }
    
    public void RegisterSpatial(Entity entity)
    {
        if (MapService == null)
            return;

        if (!World.Has<Position, MapId, Floor>(entity))
        {
            GD.PrintErr("Entity missing Position, MapId or Floor component for spatial registration.");
            return;
        }

        ref MapId mapId = ref World.Get<MapId>(entity);
        if (!MapService.HasMap(mapId.Value))
        {
            GD.PrintErr($"MapService does not have map with ID {mapId.Value} for spatial registration.");
            return;
        }
        
        var spatial = MapService.GetMapSpatial(mapId.Value);
        spatial.Insert(World.Get<Position>(entity).ToSpatialPosition(World.Get<Floor>(entity).Level), entity);
    }

    public void UnregisterSpatial(Entity entity)
    {
        if (MapService == null)
            return;
        
        if (!World.Has<Position, MapId, Floor>(entity))
        {
            GD.PrintErr("Entity missing Position, MapId or Floor component for spatial destruction.");
            return;
        }

        ref MapId mapId = ref World.Get<MapId>(entity);
        if (!MapService.HasMap(mapId.Value))
        {
            GD.PrintErr($"MapService does not have map with ID {mapId.Value} for spatial registration.");
            return;
        }
        
        var spatial = MapService.GetMapSpatial(mapId.Value);
        spatial.Remove(World.Get<Position>(entity).ToSpatialPosition(World.Get<Floor>(entity).Level), entity);
    }
}