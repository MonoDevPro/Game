using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.DTOs.Player;
using Game.ECS.Components;
using Game.ECS.Navigation.Components;
using Game.ECS.Services.Navigation;
using Game.ECS.Services.Snapshot.Sync;
using Game.ECS.Systems;
using Microsoft.Extensions.Logging;

namespace Game.ECS.Services.Snapshot.Systems;

/// <summary>
/// Sistema responsável por sincronizar o movimento de jogadores com os clientes.
/// Coleta dados de navegação e envia pacotes de movimento para broadcast.
/// </summary>
public sealed partial class PlayerNavigationSyncSystem(
    World world,
    INetSync networkManager,
    Dictionary<int, NavigationModule> navigationModules,
    ILogger<PlayerNavigationSyncSystem>? logger = null)
    : GameSystem(world, logger)
{
    // Buffer para batching de movimentos
    private readonly List<PlayerMovementSnapshot> _movementUpdates = new(32);
    
    // Controle de intervalo de sincronização
    private float _syncAccumulator;
    private const float SyncInterval = 0.05f; // 20Hz para atualizações de movimento
    
    // Tick atual do servidor (para calcular ticks restantes)
    private long _currentServerTick;

    public override void BeforeUpdate(in float deltaTime)
    {
        _syncAccumulator += deltaTime;
        _currentServerTick++;
    }

    public override void Update(in float deltaTime)
    {
        if (_syncAccumulator < SyncInterval)
            return;
            
        // Coleta snapshots de movimento de jogadores
        CollectPlayerMovementQuery(World);
        
        SendMovementUpdates();
        _syncAccumulator = 0f;
    }

    /// <summary>
    /// Query para coletar dados de movimento de jogadores que estão navegando.
    /// </summary>
    [Query]
    [All<PlayerControlled, NetworkId, Position, NavMovementState, NavAgent>]
    private void CollectPlayerMovement(
        in NetworkId networkId,
        in Position position,
        in NavMovementState movement)
    {
        _movementUpdates.Add(new PlayerMovementSnapshot(
            NetworkId: networkId.Value,
            CurrentX: (short)position.X,
            CurrentY: (short)position.Y,
            CurrentZ: (short)position.Z,
            TargetX: movement.IsMoving ? (short)movement.TargetCell.X : (short)position.X,
            TargetY: movement.IsMoving ? (short)movement.TargetCell.Y : (short)position.Y,
            TargetZ: movement.IsMoving ? (short)movement.TargetCell.Z : (short)position.Z,
            IsMoving: movement.IsMoving,
            DirectionX: (sbyte)movement.MovementDirection.X,
            DirectionY: (sbyte)movement.MovementDirection.Y,
            TicksRemaining: movement.IsMoving 
                ? (ushort)Math.Max(0, movement.EndTick - _currentServerTick)
                : (ushort)0
        ));
    }

    private void SendMovementUpdates()
    {
        if (_movementUpdates.Count == 0)
            return;
            
        var packet = new PlayerMovementPacket([.._movementUpdates]);
        networkManager.SendToAllUnreliable(ref packet);
        
        LogDebug("Sent player movement update for {Count} entities", _movementUpdates.Count);
        _movementUpdates.Clear();
    }
}
