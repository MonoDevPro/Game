using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Resource;

namespace Simulation.Core.Client.ECS.Systems;

/// <summary>
/// Sistema de testes no cliente que publica intents de movimento e ataque periodicamente.
/// Os intents são enviados ao servidor via NetworkOutbox OneShot registrada no builder.
/// </summary>
public sealed class ClientInputSystem(World world, PlayerIndexResource playerIndex, ILogger<ClientInputSystem> logger)
    : BaseSystem<World, float>(world)
{
    private const int TestPlayerId = 999; // Deve bater com o id do player spawnado
    private float _moveTimer;
    private float _attackTimer;

    public override void Update(in float dt)
    {
        _moveTimer += dt;
        _attackTimer += dt;

        if (!playerIndex.TryGetPlayerEntity(TestPlayerId, out var entity))
            return;

        // Envia um movimento a cada 2s alternando direções simples
        if (_moveTimer >= 2f && !World.Has<MoveIntent>(entity) && !World.Has<MoveTarget>(entity))
        {
            _moveTimer = 0f;
            var dir = DateTime.UtcNow.Second % 2 == 0 ? new Direction(1, 0) : new Direction(0, 1);
            World.Add(entity, new MoveIntent(dir));
            logger.LogInformation("ClientInputSystem: solicitando movimento para {X},{Y}", dir.X, dir.Y);
        }

        // Envia um ataque a cada 7s
        if (_attackTimer >= 7f && !World.Has<AttackIntent>(entity) && !World.Has<AttackTimer>(entity))
        {
            _attackTimer = 0f;
            var dir = new Direction(1, 0);
            World.Add(entity, new AttackIntent(dir));
            logger.LogInformation("ClientInputSystem: solicitando ataque na direção {X},{Y}", dir.X, dir.Y);
        }
    }
}