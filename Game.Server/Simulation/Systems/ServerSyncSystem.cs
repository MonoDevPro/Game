using Arch.Core;
using Game.DTOs.Game;
using Game.DTOs.Game.Player;
using Game.ECS.Entities.Components;
using Game.ECS.Events;
using Game.ECS.Schema.Components;
using Game.ECS.Systems;
using Game.Network.Abstractions;

namespace Game.Server.Simulation.Systems;

/// <summary>
/// Sistema responsável por sincronizar o estado das entidades com os clientes conectados.
/// Envia atualizações de posição e vitals para todos os peers.
/// </summary>
public sealed partial class ServerSyncSystem(
    World world,
    INetworkManager networkManager,
    GameEventBus bus,
    ILogger<ServerSyncSystem>? logger = null)
    : GameSystem(world, logger)
{
    // Buffers for batching updates
    private Arch.LowLevel.UnsafeQueue<PositionStateData> _stateQueue = new(16);
    private Arch.LowLevel.UnsafeQueue<VitalsData> _vitalsQueue = new(16);
    private Arch.LowLevel.UnsafeQueue<AttackData> _attackQueue = new(16);
    
    public override void Initialize()
    {
        RegisterEvents();
        
        base.Initialize();
    }

    public override void Update(in float deltaTime)
    {
        SendStateUpdates();
        SendVitalsUpdates();
        SendAttackUpdates();
        
        base.Update(in deltaTime);
    }
    
    public override void Dispose()
    {
        UnregisterEvents();
        
        base.Dispose();
    }
    
    private void RegisterEvents()
    {
        bus.OnAttack += OnAttackHandler;
        bus.OnHealthChanged += OnHealthChangedHandler;
        bus.OnManaChanged += OnManaChangedHandler;
        bus.OnMovement += OnMovementHandler;
        bus.OnDirectionChanged += OnDirectionChangedHandler;
    }
    
    private void UnregisterEvents()
    {
        bus.OnAttack -= OnAttackHandler;
        bus.OnHealthChanged -= OnHealthChangedHandler;
        bus.OnManaChanged -= OnManaChangedHandler;
        bus.OnMovement -= OnMovementHandler;
        bus.OnDirectionChanged -= OnDirectionChangedHandler;
    }
    
    #region Event Handlers
    
    private void OnAttackHandler(AttackEvent evt)
    {
        _attackQueue.Enqueue(new AttackData(
            AttackerNetworkId: World.Get<NetworkId>(evt.Attacker).Value,
            Style: evt.Style,
            AttackDuration: 1.0f,
            CooldownRemaining: 0.0f
        ));
    }
    
    private void OnHealthChangedHandler(HealthChangedEvent evt)
    {
        _vitalsQueue.Enqueue(new VitalsData(
            NetworkId: World.Get<NetworkId>(evt.Entity).Value,
            CurrentHp: evt.NewValue,
            MaxHp: evt.MaxValue,
            CurrentMp: World.Get<Mana>(evt.Entity).Current,
            MaxMp: World.Get<Mana>(evt.Entity).Max
        ));
    }
    
    private void OnManaChangedHandler(ManaChangedEvent evt)
    {
        _vitalsQueue.Enqueue(new VitalsData(
            NetworkId: World.Get<NetworkId>(evt.Entity).Value,
            CurrentHp: World.Get<Health>(evt.Entity).Current,
            MaxHp: World.Get<Health>(evt.Entity).Max,
            CurrentMp: evt.NewValue,
            MaxMp: evt.MaxValue
        ));
    }
    
    private void OnMovementHandler(MovementEvent evt)
    {
        Direction direction;
        (direction.X, direction.Y) = MovementSystem.GetDirectionTowards(evt.OldPosition, evt.NewPosition);
        
        _stateQueue.Enqueue(new PositionStateData(
            NetworkId: World.Get<NetworkId>(evt.Entity).Value,
            Floor: World.Get<Floor>(evt.Entity).Value,
            X: evt.NewPosition.X,
            Y: evt.NewPosition.Y,
            DirX: direction.X,
            DirY: direction.Y
        ));
    }
    
    private void OnDirectionChangedHandler(DirectionChangedEvent evt)
    {
        ref var position = ref World.Get<Position>(evt.Entity);
        ref var floor = ref World.Get<Floor>(evt.Entity);
        
        _stateQueue.Enqueue(new PositionStateData(
            NetworkId: World.Get<NetworkId>(evt.Entity).Value,
            X: position.X,
            Y: position.Y,
            Floor: floor.Value,
            DirX: evt.NewDirection.X,
            DirY: evt.NewDirection.Y
        ));
    }
    
    #endregion
    
    #region Send Updates
    
    private void SendStateUpdates()
    {
        // Send player state updates
        if (_stateQueue.Count > 0)
        {
            var packet = new StatePacket([.._stateQueue]);
            networkManager.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.Unreliable);
            _stateQueue.Clear();
        }
    }
    
    private void SendVitalsUpdates()
    {
        // Send player vitals updates
        if (_vitalsQueue.Count > 0)
        {
            var packet = new VitalsPacket([.._vitalsQueue]);
            networkManager.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            _vitalsQueue.Clear();
        }
    }

    private void SendAttackUpdates()
    {
        // Send attack updates
        if (_attackQueue.Count > 0)
        {
            var packet = new AttackPacket([.._attackQueue]);
            networkManager.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            _attackQueue.Clear();
        }
    }
    
    #endregion
}