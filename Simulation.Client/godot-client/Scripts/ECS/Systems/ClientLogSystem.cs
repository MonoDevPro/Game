using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Resource;

namespace GodotClient.ECS.Systems;

/// <summary>
/// Sistema de testes no cliente que registra mudanças de estado, vida e posição do jogador.
/// Útil para validar sincronização de PlayerState, Health e Position sem perder estados transitórios.
/// </summary>
public sealed class ClientLogSystem(World world, PlayerIndexResource playerIndex, ILogger<ClientLogSystem> logger)
    : BaseSystem<World, float>(world)
{
    private const int TestPlayerId = 999;

    private bool _hasSnapshot;
    private Position _lastPos;
    private Health _lastHealth;
    private PlayerState _lastState;

    public override void Update(in float dt)
    {
        if (!playerIndex.TryGetPlayerEntity(TestPlayerId, out var entity))
            return;

        var pos = World.Get<Position>(entity);
        var hp = World.Get<Health>(entity);
        var st = World.Get<PlayerState>(entity);

        if (!_hasSnapshot)
        {
            _hasSnapshot = true;
            _lastPos = pos; _lastHealth = hp; _lastState = st;
            logger.LogInformation("ClientLog[Init]: Pos=({X},{Y}) Health={HP}/{Max} State={State}", pos.X, pos.Y, hp.Current, hp.Max, st.Flags);
            return;
        }

        if (!pos.Equals(_lastPos))
        {
            logger.LogInformation("ClientLog[Pos]: Pos=({X},{Y})", pos.X, pos.Y);
            _lastPos = pos;
        }

        if (!hp.Equals(_lastHealth))
        {
            logger.LogInformation("ClientLog[HP]: Health={HP}/{Max}", hp.Current, hp.Max);
            _lastHealth = hp;
        }

        if (!st.Equals(_lastState))
        {
            logger.LogInformation("ClientLog[State]: State={State}", st.Flags);
            _lastState = st;
        }
    }
}