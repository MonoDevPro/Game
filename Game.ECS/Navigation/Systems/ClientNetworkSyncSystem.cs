using Arch.Core;
using Arch.System;
using Game.ECS.Navigation.Components;
using Game.ECS.Navigation.Data;

namespace Game.ECS.Navigation.Systems;

/// <summary>
/// Sistema que processa dados recebidos do servidor e atualiza entidades.
/// </summary>
public sealed class NetworkSyncSystem(
    World world,
    float tickRate = 60f,
    float cellSize = 32f)
    : BaseSystem<World, float>(world)
{
    private readonly Queue<MovementSnapshot> _pendingSnapshots = new(128);
    private readonly Queue<TeleportMessage> _pendingTeleports = new(32);
    private readonly Dictionary<int, Entity> _entityMap = new(256);

    /// <summary>
    /// Chamado pela camada de rede quando recebe snapshot. 
    /// Thread-safe:  pode ser chamado de thread de rede.
    /// </summary>
    public void EnqueueSnapshot(MovementSnapshot snapshot)
    {
        lock (_pendingSnapshots)
        {
            _pendingSnapshots.Enqueue(snapshot);
        }
    }

    /// <summary>
    /// Chamado quando recebe batch update.
    /// </summary>
    public void EnqueueBatch(BatchMovementUpdate batch)
    {
        lock (_pendingSnapshots)
        {
            foreach (var snapshot in batch.Snapshots)
                _pendingSnapshots.Enqueue(snapshot);
        }
    }

    /// <summary>
    /// Chamado quando recebe teleporte.
    /// </summary>
    public void EnqueueTeleport(TeleportMessage teleport)
    {
        lock (_pendingTeleports)
        {
            _pendingTeleports. Enqueue(teleport);
        }
    }

    /// <summary>
    /// Registra mapeamento EntityId do servidor -> Entity local.
    /// </summary>
    public void RegisterEntity(int serverId, Entity localEntity)
    {
        _entityMap[serverId] = localEntity;
    }

    /// <summary>
    /// Remove mapeamento. 
    /// </summary>
    public void UnregisterEntity(int serverId)
    {
        _entityMap.Remove(serverId);
    }

    public override void Update(in float deltaTime)
    {
        ProcessTeleports();
        ProcessSnapshots();
    }

    private void ProcessTeleports()
    {
        lock (_pendingTeleports)
        {
            while (_pendingTeleports. Count > 0)
            {
                var teleport = _pendingTeleports.Dequeue();
                ApplyTeleport(teleport);
            }
        }
    }

    private void ApplyTeleport(TeleportMessage teleport)
    {
        if (! _entityMap.TryGetValue(teleport.EntityId, out var entity))
            return;

        if (! World.IsAlive(entity))
            return;

        // Atualiza posição sincronizada
        ref var syncedPos = ref World.Get<SyncedGridPosition>(entity);
        syncedPos. X = teleport. X;
        syncedPos.Y = teleport.Y;

        // Teleporte instantâneo - atualiza visual diretamente
        ref var visualPos = ref World. Get<VisualPosition>(entity);
        visualPos = VisualPosition.FromGrid(teleport. X, teleport. Y, cellSize);

        // Para qualquer interpolação em andamento
        ref var movement = ref World.Get<ClientMovementState>(entity);
        movement.Reset();
        movement.ToPosition = visualPos;

        // Atualiza direção
        ref var animState = ref World.Get<AnimationState>(entity);
        animState.FacingDirection = teleport.FacingDirection;
        animState.SetAnimation(AnimationType.Idle);
    }

    private void ProcessSnapshots()
    {
        lock (_pendingSnapshots)
        {
            while (_pendingSnapshots. Count > 0)
            {
                var snapshot = _pendingSnapshots. Dequeue();
                ApplySnapshot(snapshot);
            }
        }
    }

    private void ApplySnapshot(MovementSnapshot snapshot)
    {
        if (! _entityMap.TryGetValue(snapshot.EntityId, out var entity))
            return;

        if (!World.IsAlive(entity))
            return;

        // Atualiza posição sincronizada
        ref var syncedPos = ref World.Get<SyncedGridPosition>(entity);
        syncedPos.X = snapshot.CurrentX;
        syncedPos.Y = snapshot.CurrentY;

        ref var movement = ref World.Get<ClientMovementState>(entity);
        ref var visualPos = ref World. Get<VisualPosition>(entity);
        ref var buffer = ref World.Get<MovementBuffer>(entity);
        var settings = World.Get<ClientVisualSettings>(entity);

        if (snapshot.IsMoving)
        {
            // Entidade está se movendo no servidor
            float duration = snapshot.GetDurationSeconds(tickRate);
            
            // Ajusta duração baseado na velocidade de interpolação
            duration /= settings.InterpolationSpeed;

            if (! movement.IsInterpolating)
            {
                // Inicia nova interpolação
                var targetPos = VisualPosition.FromGrid(snapshot.TargetX, snapshot.TargetY, cellSize);
                movement.StartInterpolation(visualPos, targetPos, duration, snapshot.Direction);
            }
            else
            {
                // Já está interpolando - adiciona ao buffer
                buffer. Enqueue(snapshot. TargetX, snapshot.TargetY, duration, snapshot. Direction);
            }
        }
        else
        {
            // Entidade parada
            // Verifica se precisa corrigir posição
            var expectedPos = VisualPosition.FromGrid(snapshot.CurrentX, snapshot.CurrentY, cellSize);
            float distance = visualPos.DistanceTo(expectedPos);

            if (distance > cellSize * 0.1f && !movement.IsInterpolating)
            {
                // Correção suave para posição correta
                movement. StartInterpolation(visualPos, expectedPos, 0.1f, snapshot.Direction);
            }
        }

        // Atualiza animação
        ref var animState = ref World.Get<AnimationState>(entity);
        animState.FacingDirection = snapshot.Direction;
    }
}