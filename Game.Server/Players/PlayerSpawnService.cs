using Arch.Core;
using Game.Core;
using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.Domain.VOs;
using Game.Network.Packets;
using Game.Server.Sessions;
using Microsoft.Extensions.Logging;

namespace Game.Server.Players;

/// <summary>
/// Handles spawning and despawning of player entities within the simulation.
/// </summary>
public sealed class PlayerSpawnService
{
    private readonly GameSimulation _simulation;
    private readonly ILogger<PlayerSpawnService> _logger;

    public PlayerSpawnService(GameSimulation simulation, ILogger<PlayerSpawnService> logger)
    {
        _simulation = simulation;
        _logger = logger;
    }

    public Entity SpawnPlayer(PlayerSession session)
    {
        var character = session.Character;
        Coordinate startPosition = new(character.PositionX, character.PositionY);
        DirectionEnum facing = character.DirectionEnum;

        var entity = _simulation.SpawnPlayer(session.Account.Id, session.Peer.Id, startPosition, facing, session.Character.Stats);
        session.Entity = entity;

        _logger.LogInformation("Spawned player {Name} at {Position}", character.Name, startPosition);
        return entity;
    }

    public void DespawnPlayer(PlayerSession session)
    {
        if (session.Entity == Entity.Null)
            return;

        _simulation.DespawnEntity(session.Entity);
        _logger.LogInformation("Despawned player {Name}", session.Character.Name);
        session.Entity = Entity.Null;
    }

    public PlayerSnapshot BuildSnapshot(PlayerSession session)
    {
        if (!_simulation.TryGetPlayerState(session.Entity, out var position, out var direction))
        {
            return new PlayerSnapshot(session.Peer.Id, session.Account.Id, session.Character.Id, session.Character.Name,
                new GridPosition(session.Character.PositionX, session.Character.PositionY), session.Character.DirectionEnum);
        }

        return new PlayerSnapshot(session.Peer.Id, session.Account.Id, session.Character.Id, session.Character.Name,
            GridPosition.FromCoordinate(position), direction);
    }
}
