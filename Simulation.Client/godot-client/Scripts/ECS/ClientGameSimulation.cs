using Arch.Core;
using Arch.System;
using Game.Domain.Templates;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Entities;
using Game.ECS.Entities.Factories;
using Game.ECS.Entities.Npc;
using Game.ECS.Entities.Player;
using Game.ECS.Logic;
using Game.ECS.Services;
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
        : base(new GameServices(), Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance)
    {
        _networkManager = networkManager;
        ConfigureSystems(World, Services!, Systems);
    }

    /// <summary>
    /// Configura sistemas apenas para o cliente.
    /// Ordem: Input → Movement (previsão local) → Sync (recebe correções do servidor)
    /// </summary>
    protected override void ConfigureSystems(World world, GameServices services, Group<float> systems)
    {
        // Sistemas de entrada do jogador
        systems.Add(new GodotInputSystem(world));
        
        // Sync de nós visuais
        _visualSyncSystem = new ClientVisualSyncSystem(world, GameClient.Instance.EntitiesRoot);
        systems.Add(_visualSyncSystem);
        
        // Spatial updates
        systems.Add(new SpatialService(world, MapIndex));
        
        // Sincronização com o servidor
        systems.Add(new NetworkSyncSystem(world, _networkManager));
    }
    
    public bool ApplyPlayerState(StateSnapshot snapshot) => World.ApplyPlayerState(PlayerIndex, snapshot);
    public bool ApplyPlayerVitals(VitalsSnapshot snapshot) => World.ApplyPlayerVitals(PlayerIndex, snapshot);
    
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

    public Entity CreateLocalPlayer(in PlayerSnapshot snapshot, PlayerVisual visual)
    {
        GD.Print($"[GameClient] Spawning player visual for '{snapshot.Name}' (NetID: {snapshot.NetworkId}, Local: {true})");
        var entity = base.CreatePlayer(snapshot);
        World.Add<LocalPlayerTag>(entity);
        _visualSyncSystem?.RegisterPlayerVisual(snapshot.NetworkId, visual);
        visual.UpdateFromSnapshot(in snapshot);
        visual.MakeCamera();
        return entity;
    }
    
    public Entity CreateRemotePlayer(in PlayerSnapshot snapshot, PlayerVisual visual)
    {
        GD.Print($"[GameClient] Spawning player visual for '{snapshot.Name}' (NetID: {snapshot.NetworkId}, Local: {false})");
        var entity = base.CreatePlayer(snapshot);
        World.Add<RemotePlayerTag>(entity);
        _visualSyncSystem?.RegisterPlayerVisual(snapshot.NetworkId, visual);
        visual.UpdateFromSnapshot(snapshot);
        return entity;
    }
    
    public override bool DestroyEntity(int networkId)
    {
        _visualSyncSystem?.UnregisterPlayerVisual(networkId);
        return base.DestroyEntity(networkId);
    }

    public Entity CreateNpc(in NpcSnapshot snapshot, NpcVisual visual)
    {
        var entity = base.World.CreateNPC(in snapshot);
        _visualSyncSystem?.RegisterNpcVisual(snapshot.NetworkId, visual);
        visual.UpdateFromSnapshot(snapshot);
        return entity;
    }

    public override bool DestroyNpc(int networkId)
    {
        _visualSyncSystem?.UnregisterNpcVisual(networkId);
        return base.DestroyNpc(networkId);
    }

    public bool UpdateNpcState(in NpcStateSnapshot state)
    {
        return NpcIndex.TryGetEntity(state.NetworkId, out var entity) && 
               World.ApplyNpcUpdate(entity, state);
    }
    
    public bool UpdateNpcVitals(in NpcVitalsSnapshot snapshot)
    {
        return NpcIndex.TryGetEntity(snapshot.NetworkId, out var entity) && 
               World.ApplyNpcVitals(entity, snapshot);
    }
}