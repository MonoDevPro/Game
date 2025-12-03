using Arch.Core;
using Arch.System;
using Game.ECS;
using Game.ECS.Entities.Npc;
using Game.ECS.Entities.Player;
using Game.ECS.Schema.Components;
using Game.ECS.Schema.Templates;
using Game.ECS.Services;
using Game.ECS.Services.Index;
using Game.ECS.Services.Map;
using Game.ECS.Systems;
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
    
    // Index para busca rápida de entidades por NetworkId
    private readonly EntityIndex<int> _playerIndex = new();
    private readonly EntityIndex<int> _npcIndex = new();

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
    }

    /// <summary>
    /// Configura sistemas apenas para o cliente.
    /// Ordem: Input → Movement (previsão local) → Sync (recebe correções do servidor)
    /// </summary>
    protected override void ConfigureSystems(World world, Group<float> systems, ILoggerFactory? loggerFactory = null)
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
    
    #region Player Management
    
    /// <summary>
    /// Index de jogadores por NetworkId.
    /// </summary>
    public EntityIndex<int> PlayerIndex => _playerIndex;
    
    /// <summary>
    /// Index de NPCs por NetworkId.
    /// </summary>
    public EntityIndex<int> NpcIndex => _npcIndex;
    
    /// <summary>
    /// Cria uma entidade de jogador a partir de snapshot (usado internamente).
    /// </summary>
    private Entity CreatePlayer(in PlayerSnapshot snapshot)
    {
        var template = new PlayerTemplate(
            Id: snapshot.PlayerId,
            IdentityTemplate: new IdentityTemplate(
                NetworkId: snapshot.NetworkId,
                Name: snapshot.Name,
                Gender: (Game.Domain.Enums.Gender)snapshot.GenderId,
                Vocation: (Game.Domain.Enums.VocationType)snapshot.VocationId
            ),
            LocationTemplate: new LocationTemplate(
                MapId: snapshot.MapId,
                Floor: snapshot.Floor,
                X: snapshot.PosX,
                Y: snapshot.PosY
            ),
            DirectionTemplate: new DirectionTemplate(
                DirX: snapshot.DirX,
                DirY: snapshot.DirY
            ),
            VitalsTemplate: new VitalsTemplate(
                CurrentHp: snapshot.Hp,
                MaxHp: snapshot.MaxHp,
                CurrentMp: snapshot.Mp,
                MaxMp: snapshot.MaxMp,
                HpRegen: 0f,
                MpRegen: 0f
            ),
            StatsTemplate: new StatsTemplate(
                MovementSpeed: snapshot.MovementSpeed,
                AttackSpeed: snapshot.AttackSpeed,
                PhysicalAttack: snapshot.PhysicalAttack,
                MagicAttack: snapshot.MagicAttack,
                PhysicalDefense: snapshot.PhysicalDefense,
                MagicDefense: snapshot.MagicDefense
            )
        );
        
        var entity = World.CreatePlayer(Strings, template);
        _playerIndex.Register(snapshot.NetworkId, entity);
        return entity;
    }
    
    /// <summary>
    /// Destrói uma entidade de jogador pelo NetworkId.
    /// </summary>
    public bool DestroyEntity(int networkId)
    {
        if (!_playerIndex.TryGetEntity(networkId, out var entity))
            return false;
        
        _playerIndex.RemoveByKey(networkId);
        World.Destroy(entity);
        return true;
    }
    
    /// <summary>
    /// Destrói uma entidade de NPC pelo NetworkId.
    /// </summary>
    public bool DestroyNpc(int networkId)
    {
        if (!_npcIndex.TryGetEntity(networkId, out var entity))
            return false;
        
        _npcIndex.RemoveByKey(networkId);
        World.Destroy(entity);
        return true;
    }
    
    #endregion
    
    public bool ApplyPlayerState(StateSnapshot snapshot) => World.ApplyPlayerState(_playerIndex, snapshot);
    public bool ApplyPlayerVitals(VitalsSnapshot snapshot) => World.ApplyPlayerVitals(_playerIndex, snapshot);
    
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
        var entity = CreatePlayer(snapshot);
        World.Add<LocalPlayerTag>(entity);
        _visualSyncSystem?.RegisterPlayerVisual(snapshot.NetworkId, visual);
        visual.UpdateFromSnapshot(in snapshot);
        visual.MakeCamera();
        return entity;
    }
    
    public Entity CreateRemotePlayer(in PlayerSnapshot snapshot, PlayerVisual visual)
    {
        GD.Print($"[GameClient] Spawning player visual for '{snapshot.Name}' (NetID: {snapshot.NetworkId}, Local: {false})");
        var entity = CreatePlayer(snapshot);
        World.Add<RemotePlayerTag>(entity);
        _visualSyncSystem?.RegisterPlayerVisual(snapshot.NetworkId, visual);
        visual.UpdateFromSnapshot(snapshot);
        return entity;
    }
    
    public bool DestroyPlayerEntity(int networkId)
    {
        _visualSyncSystem?.UnregisterPlayerVisual(networkId);
        return DestroyEntity(networkId);
    }

    public Entity CreateNpc(in NpcSnapshot snapshot, NpcVisual visual)
    {
        var entity = World.CreateNPC(in snapshot);
        _npcIndex.Register(snapshot.NetworkId, entity);
        _visualSyncSystem?.RegisterNpcVisual(snapshot.NetworkId, visual);
        visual.UpdateFromSnapshot(snapshot);
        return entity;
    }

    public bool DestroyNpcEntity(int networkId)
    {
        _visualSyncSystem?.UnregisterNpcVisual(networkId);
        return DestroyNpc(networkId);
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