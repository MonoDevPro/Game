using Arch.Core;
using Game.Domain.Enums;
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
        
        var entity = simulation.SpawnPlayer(session.Account.Id, session.Peer.Id, 
            character.PositionX, character.PositionY, character.PositionZ, character.FacingX, character.FacingY, 
            character.Stats);
        session.Entity = entity;

        logger.LogInformation("Spawned player {Name} at ({PosX}, {PosY})", character.Name, character.PositionX, character.PositionY);
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
        
        if (!simulation.TryGetPlayerState(session.Entity, out var position, out var facing, out var speed))
        {
            return new PlayerSnapshot(session.Peer.Id, session.Account.Id, character.Id, character.Name, 
                (byte)character.Gender, (byte)character.Vocation, 
                character.PositionX, character.PositionY, character.PositionZ,
                character.FacingX, character.FacingY, (float)character.Stats.MovementSpeed);
        }

        return new PlayerSnapshot(session.Peer.Id, session.Account.Id, character.Id, character.Name,
            (byte)character.Gender, (byte)character.Vocation, position.X, position.Y, position.Z, facing.DirectionX,
            facing.DirectionY, speed);
    }
}
