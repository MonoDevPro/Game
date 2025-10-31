using Arch.Core;
using Arch.System;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Entities.Factories;
using Game.ECS.Entities.Repositories;
using Game.ECS.Entities.Updates;
using Game.ECS.Services;
using Game.ECS.Systems;
using Godot;
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
    public ClientGameSimulation(IMapService? mapService = null)
        : base(mapService ?? new MapService())
    {
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
        
        // Sistemas de sincronização com o servidor
        systems.Add(new RemoteInterpolationSystem(world));
    }
    
    public bool TryGetPlayerEntity(int playerId, out Entity entity) => PlayerIndex.TryGetEntity(playerId, out entity);
    public bool ApplyPlayerState(PlayerStateData data) => World.ApplyPlayerState(PlayerIndex, data);
    public bool ApplyPlayerVitals(PlayerVitalsData data) => World.ApplyPlayerVitals(PlayerIndex, data);
    public bool DespawnPlayer(int playerId) => World.TryDestroyPlayer(PlayerIndex, playerId);
    public Entity SpawnLocalPlayer(PlayerData data, PlayerVisual visual) 
        => CreateLocalPlayer(World, PlayerIndex, data, visual);
    public Entity SpawnRemotePlayer(PlayerData data, PlayerVisual visual) 
        => CreateRemotePlayer(World, PlayerIndex, data, visual);
    
    private static Entity CreateLocalPlayer(World world, PlayerIndex index, in PlayerData data, PlayerVisual visual)
    {
        var entity = world.CreatePlayer(index, data);
        world.Add<LocalPlayerTag, VisualReference>(entity,
            new LocalPlayerTag(),
            new VisualReference { VisualNode = visual, IsVisible = true });
        return entity;
    }
    private static Entity CreateRemotePlayer(World world, PlayerIndex index, in PlayerData data, PlayerVisual visual)
    {
        var entity = world.CreatePlayer(index, data);
        world.Add<RemotePlayerTag, RemoteInterpolation, VisualReference>(entity,
            new RemotePlayerTag(),
            new RemoteInterpolation { LerpAlpha = 0.15f, ThresholdPx = 2f },
            new VisualReference { VisualNode = visual, IsVisible = true });
        return entity;
    }
}