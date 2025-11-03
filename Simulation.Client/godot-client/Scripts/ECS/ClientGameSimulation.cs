using Arch.Core;
using Arch.System;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Entities.Factories;
using Game.ECS.Entities.Repositories;
using Game.ECS.Entities.Updates;
using Game.ECS.Services;
using Game.Network.Abstractions;
using GodotClient.ECS.Components;
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
    private INetworkManager NetworkManager { get; }
    
    public ClientGameSimulation(INetworkManager networkManager, IMapService? mapService = null)
        : base(mapService ?? new MapService())
    {
        NetworkManager = networkManager;
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
        
        // Sistemas de movimento com predição local
        systems.Add(new LocalMovementSystem(world, MapService));

        // Atualização do nó do jogador local (renderização suave entre tiles)
        systems.Add(new LocalVisualUpdateSystem(world));
        
        // ✅ NOVO: Movimento suave para jogadores remotos
        systems.Add(new RemoteInterpolationSystem(world));
        
        // Animação do jogador local
        systems.Add(new PlayerAnimationSystem(world));
    
        // Sincronização com o servidor
        systems.Add(new ClientSyncSystem(world, NetworkManager));
    }
    
    public bool HasPlayerEntity(int playerId)
    {
        if (PlayerIndex.TryGetEntity(playerId, out var entity))
            return World.IsAlive(entity);
        
        return false;
    }

    public bool TryGetPlayerEntity(int playerId, out Entity entity) => PlayerIndex.TryGetEntity(playerId, out entity);
    public bool ApplyPlayerState(PlayerStateData data) => World.ApplyPlayerState(PlayerIndex, data);
    public bool ApplyPlayerVitals(PlayerVitalsData data) => World.ApplyPlayerVitals(PlayerIndex, data);
    public bool DespawnPlayer(int networkId) => DestroyPlayer(networkId);
    public Entity SpawnLocalPlayer(PlayerData data, PlayerVisual visual) 
        => CreateLocalPlayer(PlayerIndex, data, visual);
    public Entity SpawnRemotePlayer(PlayerData data, PlayerVisual visual) 
        => CreateRemotePlayer(PlayerIndex, data, visual);
    
    private Entity CreateLocalPlayer(PlayerIndex index, in PlayerData data, PlayerVisual visual)
    {
        var entity = World.CreatePlayer(index, data);
        World.Add<LocalPlayerTag, VisualReference>(entity,
            new LocalPlayerTag(),
            new VisualReference { VisualNode = visual, IsVisible = true });
        RegisterSpatial(entity);
        return entity;
    }
    private Entity CreateRemotePlayer(PlayerIndex index, in PlayerData data, PlayerVisual visual)
    {
        var entity = World.CreatePlayer(index, data);
        World.Add<RemotePlayerTag, VisualReference>(entity,
            new RemotePlayerTag(),
            new VisualReference { VisualNode = visual, IsVisible = true });
        RegisterSpatial(entity);
        return entity;
    }
    private bool DestroyPlayer(int networkId)
    {
        if (!PlayerIndex.TryGetEntity(networkId, out var entity))
            return false;
        UnregisterSpatial(entity);
        return World.TryDestroyPlayer(PlayerIndex, networkId);;
    }
    
}