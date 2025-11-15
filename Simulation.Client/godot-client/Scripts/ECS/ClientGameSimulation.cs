using Arch.Core;
using Arch.System;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Entities.Factories;
using Game.ECS.Entities.Updates;
using Game.ECS.Services;
using Game.Network.Abstractions;
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
    
    public bool TryGetPlayerEntity(int playerId, out Entity entity) => PlayerIndex.TryGetEntity(playerId, out entity);
    public bool ApplyPlayerState(PlayerStateData data) => World.ApplyPlayerState(PlayerIndex, data);
    public bool ApplyPlayerVitals(PlayerVitalsData data) => World.ApplyPlayerVitals(PlayerIndex, data);
    public bool DespawnPlayer(int networkId) => DestroyPlayer(networkId);
    public Entity SpawnLocalPlayer(PlayerData data, PlayerVisual visual) 
        => CreateLocalPlayer(data, visual);
    public Entity SpawnRemotePlayer(PlayerData data, PlayerVisual visual) 
        => CreateRemotePlayer(data, visual);
    
    // Visual
    public bool TryGetPlayerVisual(int networkId, out PlayerVisual visual)
    {
        if (_visualSyncSystem != null) 
            return _visualSyncSystem.TryGetVisual(networkId, out visual!);
        visual = null!;
        return false;
    }

    private Entity CreateLocalPlayer(in PlayerData data, PlayerVisual visual)
    {
        var entity = World.CreatePlayer(PlayerIndex, data);
        World.Add<LocalPlayerTag>(entity, new LocalPlayerTag());
        RegisterSpatial(entity);
        _visualSyncSystem?.RegisterVisual(data.NetworkId, visual);
        visual.UpdateFromSnapshot(data);
        return entity;
    }
    private Entity CreateRemotePlayer(in PlayerData data, PlayerVisual visual)
    {
        var entity = World.CreatePlayer(PlayerIndex, data);
        World.Add<RemotePlayerTag>(entity, new RemotePlayerTag());
        RegisterSpatial(entity);
        _visualSyncSystem?.RegisterVisual(data.NetworkId, visual);
        visual.UpdateFromSnapshot(data);
        return entity;
    }
    private bool DestroyPlayer(int networkId)
    {
        if (!PlayerIndex.TryGetEntity(networkId, out var entity))
            return false;
        UnregisterSpatial(entity);
        _visualSyncSystem?.UnregisterVisual(networkId);
        return World.TryDestroyPlayer(PlayerIndex, networkId);;
    }
    
    public void RegisterSpatial(Entity entity)
    {
        if (MapService == null)
            return;
        
        if (!World.Has<Position>(entity))
            return;

        int mapId = 0;
        if (World.Has<MapId>(entity))
        {
            ref MapId mapComponent = ref World.Get<MapId>(entity);
            mapId = mapComponent.Value;
        }

        if (!MapService.HasMap(mapId))
        {
            // aqui escolha o número de layers correto para esse mapa; por ora usamos 1
            MapService.RegisterMap(mapId, new MapGrid(100, 100, layers: 3), new MapSpatial());
        }

        var spatial = MapService.GetMapSpatial(mapId);
        ref Position position = ref World.Get<Position>(entity);
        spatial.Insert(position, entity);
    }

    public void UnregisterSpatial(Entity entity)
    {
        if (MapService == null)
            return;
        
        if (!World.Has<Position>(entity))
            return;

        int mapId = 0;
        if (World.Has<MapId>(entity))
        {
            ref MapId mapComponent = ref World.Get<MapId>(entity);
            mapId = mapComponent.Value;
        }

        if (!MapService.HasMap(mapId))
            return;

        var spatial = MapService.GetMapSpatial(mapId);
        ref Position position = ref World.Get<Position>(entity);
        spatial.Remove(position, entity);
    }
}