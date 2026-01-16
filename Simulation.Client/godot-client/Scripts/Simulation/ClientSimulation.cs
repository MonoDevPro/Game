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
    
    /// <summary>
    /// Aplica snapshot de movimento de NPC recebido do servidor.
    /// Atualiza posição e direção do NPC, além de iniciar interpolação visual.
    /// </summary>
    public void ApplyNpcMovement(NpcMovementSnapshot movement)
    {
        if (!TryGetEntity(movement.NetworkId, out var entity))
        {
            // NPC pode não existir ainda (spawn ainda não chegou)
            return;
        }
        
        // Atualiza posição no ECS
        ref var position = ref World.Get<Game.ECS.Components.Position>(entity);
        position.X = movement.CurrentX;
        position.Y = movement.CurrentY;
        position.Z = movement.CurrentZ;
        
        // Atualiza direção
        ref var direction = ref World.Get<Game.ECS.Components.Direction>(entity);
        direction.X = movement.DirectionX;
        direction.Y = movement.DirectionY;
        
        // Se estiver movendo, adiciona componente de movimento para interpolação
        if (movement.IsMoving && _visualSyncSystem != null)
        {
            // Adiciona/atualiza componente de movimento local para interpolação
            if (World.Has<NpcMovementData>(entity))
            {
                ref var movementData = ref World.Get<NpcMovementData>(entity);
                movementData.TargetX = movement.TargetX;
                movementData.TargetY = movement.TargetY;
                movementData.TargetZ = movement.TargetZ;
                movementData.IsMoving = true;
                movementData.TicksRemaining = movement.TicksRemaining;
            }
            else
            {
                World.Add(entity, new NpcMovementData
                {
                    TargetX = movement.TargetX,
                    TargetY = movement.TargetY,
                    TargetZ = movement.TargetZ,
                    IsMoving = true,
                    TicksRemaining = movement.TicksRemaining
                });
            }
        }
        else if (World.Has<NpcMovementData>(entity))
        {
            ref var movementData = ref World.Get<NpcMovementData>(entity);
            movementData.IsMoving = false;
        }
    }
    
    /// <summary>
    /// Aplica snapshot de movimento de jogador recebido do servidor.
    /// Atualiza posição e direção do jogador, além de iniciar interpolação visual.
    /// </summary>
    public void ApplyPlayerMovement(PlayerMovementSnapshot movement)
    {
        if (!TryGetEntity(movement.NetworkId, out var entity))
        {
            // Jogador pode não existir ainda (spawn ainda não chegou)
            return;
        }
        
        // Pula atualização para o jogador local (predição local é usada)
        if (World.Has<LocalPlayerTag>(entity))
        {
            // Reconciliação: apenas atualiza se a diferença for significativa
            ref var position = ref World.Get<Game.ECS.Components.Position>(entity);
            var deltaX = Math.Abs(position.X - movement.CurrentX);
            var deltaY = Math.Abs(position.Y - movement.CurrentY);
            
            // Se diferença for grande (mais de 2 tiles), corrige posição
            if (deltaX > 2 || deltaY > 2)
            {
                position.X = movement.CurrentX;
                position.Y = movement.CurrentY;
                position.Z = movement.CurrentZ;
            }
            return;
        }
        
        // Atualiza posição no ECS para outros jogadores
        ref var pos = ref World.Get<Game.ECS.Components.Position>(entity);
        pos.X = movement.CurrentX;
        pos.Y = movement.CurrentY;
        pos.Z = movement.CurrentZ;
        
        // Atualiza direção
        ref var direction = ref World.Get<Game.ECS.Components.Direction>(entity);
        direction.X = movement.DirectionX;
        direction.Y = movement.DirectionY;
        
        // Se estiver movendo, adiciona componente de movimento para interpolação
        if (movement.IsMoving && _visualSyncSystem != null)
        {
            // Adiciona/atualiza componente de movimento local para interpolação
            if (World.Has<PlayerMovementData>(entity))
            {
                ref var movementData = ref World.Get<PlayerMovementData>(entity);
                movementData.TargetX = movement.TargetX;
                movementData.TargetY = movement.TargetY;
                movementData.TargetZ = movement.TargetZ;
                movementData.IsMoving = true;
                movementData.TicksRemaining = movement.TicksRemaining;
            }
            else
            {
                World.Add(entity, new PlayerMovementData
                {
                    TargetX = movement.TargetX,
                    TargetY = movement.TargetY,
                    TargetZ = movement.TargetZ,
                    IsMoving = true,
                    TicksRemaining = movement.TicksRemaining
                });
            }
        }
        else if (World.Has<PlayerMovementData>(entity))
        {
            ref var movementData = ref World.Get<PlayerMovementData>(entity);
            movementData.IsMoving = false;
        }
    }
    
}