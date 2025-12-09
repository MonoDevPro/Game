using Arch.Core;
using Arch.System;
using Game.DTOs.Game.Npc;
using Game.DTOs.Game.Player;
using Game.ECS;
using Game.ECS.Entities;
using Game.Network.Abstractions;
using Godot;
using GodotClient.ECS.Systems;
using GodotClient.Simulation;
using Microsoft.Extensions.Logging;

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
        : base(null)
    {
        _networkManager = networkManager;
        
        ConfigureSystems(World, Systems, null);
        
        // Inicializa os sistemas
        Systems.Initialize();
    }

    /// <summary>
    /// Configura sistemas apenas para o cliente.
    /// Ordem: Input → Visual Sync → Network Sync
    /// O cliente não executa sistemas de movimento/combate - recebe estado do servidor.
    /// </summary>
    protected override void ConfigureSystems(World world, Group<float> systems, ILoggerFactory? loggerFactory = null)
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
    
    public bool TryGetAnyEntity(int networkId, out Entity entity)
    {
        if (TryGetPlayerEntity(networkId, out entity))
            return true;
        if (TryGetNpcEntity(networkId, out entity))
            return true;
        entity = default;
        return false;
    }

    public Entity CreateLocalPlayer(ref PlayerData snapshot, PlayerVisual visual)
    {
        GD.Print($"[GameClient] Spawning player visual for '{snapshot.Name}' (NetID: {snapshot.NetworkId}, Local: {true})");
        var entity = CreatePlayer(ref snapshot);
        World.Add<LocalPlayerTag, DirtyFlags>(entity);
        _visualSyncSystem?.RegisterPlayerVisual(snapshot.NetworkId, visual);
        visual.UpdateFromSnapshot(in snapshot);
        visual.MakeCamera();
        return entity;
    }
    
    public Entity CreateRemotePlayer(ref PlayerData snapshot, PlayerVisual visual)
    {
        GD.Print($"[GameClient] Spawning player visual for '{snapshot.Name}' (NetID: {snapshot.NetworkId}, Local: {false})");
        var entity = CreatePlayer(ref snapshot);
        World.Add<RemotePlayerTag>(entity);
        _visualSyncSystem?.RegisterPlayerVisual(snapshot.NetworkId, visual);
        visual.UpdateFromSnapshot(snapshot);
        return entity;
    }
    
    public override bool DestroyPlayer(int networkId)
    {
        _visualSyncSystem?.UnregisterPlayerVisual(networkId);
        return base.DestroyPlayer(networkId);
    }

    public Entity CreateNpc(ref NpcData snapshot, NpcVisual visual)
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
    
    public void ApplyState(ref PositionStateData positionStateSnapshot)
    {
        if (!TryGetAnyEntity(positionStateSnapshot.NetworkId, out Entity entity))
        {
            GD.PrintErr($"[GameClient] Cannot apply state: entity with NetworkId {positionStateSnapshot.NetworkId} not found.");
            return;
        }
        
        World.UpdateState(entity, ref positionStateSnapshot);
    }
    
    public void ApplyVitals(ref VitalsData vitalsSnapshot)
    {
        if (!TryGetPlayerEntity(vitalsSnapshot.NetworkId, out var entity) &&
            !TryGetNpcEntity(vitalsSnapshot.NetworkId, out entity))
        {
            GD.PrintErr($"[GameClient] Cannot apply vitals: entity with NetworkId {vitalsSnapshot.NetworkId} not found.");
            return;
        }
        World.UpdateVitals(entity, ref vitalsSnapshot);
    }
    
}