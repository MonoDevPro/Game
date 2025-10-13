using Arch.Core;
using Game.Abstractions;
using Game.Domain.Enums;
using Game.Domain.VOs;
using Game.ECS.Extensions;
using Game.Network.Packets;
using Game.Network.Packets.DTOs;
using Game.Server.Sessions;
using Game.Server.Simulation;

namespace Game.Server.Players;

/// <summary>
/// Handles spawning and despawning of player entities within the simulation.
/// </summary>
public sealed class PlayerSpawnService(GameSimulation simulation, ILogger<PlayerSpawnService> logger)
{
    public Entity SpawnPlayer(PlayerSession session)
    {
        var character = session.SelectedCharacter 
            ?? throw new InvalidOperationException("No character selected for session.");
        
        Coordinate startPosition = new(character.PositionX, character.PositionY);
        DirectionEnum facing = character.DirectionEnum;

        var entity = simulation.SpawnPlayer(session.Account.Id, session.Peer.Id, startPosition, facing, character.Stats);
        session.Entity = entity;

        logger.LogInformation("Spawned player {Name} at {Position}", character.Name, startPosition);
        return entity;
    }

    public void DespawnPlayer(PlayerSession session)
    {
        if (session.Entity == Entity.Null)
            return;
        
        var character = session.SelectedCharacter 
            ?? throw new InvalidOperationException("No character selected for session.");
        
        simulation.DespawnEntity(session.Entity);
        logger.LogInformation("Despawned player {Name}", character.Name);
        session.Entity = Entity.Null;
    }

    public PlayerSnapshot BuildSnapshot(PlayerSession session)
    {
        var character = session.SelectedCharacter 
            ?? throw new InvalidOperationException("No character selected for session.");
        
        if (!simulation.TryGetPlayerState(session.Entity, out var position, out var direction, out var speed))
        {
            return new PlayerSnapshot(session.Peer.Id, session.Account.Id, character.Id, character.Name,
                character.Gender, character.Vocation, new Coordinate(character.PositionX, 
                    character.PositionY), character.DirectionEnum.ToCoordinate(), 0f);
        }

        return new PlayerSnapshot(session.Peer.Id, session.Account.Id, character.Id, character.Name,
            character.Gender, character.Vocation, position, direction.ToCoordinate(), speed);
    }
}
