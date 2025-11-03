using Arch.Core;
using Game.ECS.Entities.Factories;
using Game.Server.ECS;
using Game.Server.Sessions;

namespace Game.Server.Players;

/// <summary>
/// Handles spawning and despawning of player entities within the simulation.
/// </summary>
public sealed class PlayerSpawnService(ServerGameSimulation simulation, ILogger<PlayerSpawnService> logger)
{
    public Entity SpawnPlayer(PlayerSession session)
    {
        var character = session.SelectedCharacter 
            ?? throw new InvalidOperationException("No character selected for session.");

        var playerData = new PlayerData(
            session.Account.Id,
            session.Peer.Id,
            character.Name,
            (byte)character.Gender,
            (byte)character.Vocation,
            character.PositionX,
            character.PositionY,
            character.PositionZ,
            character.FacingX,
            character.FacingY,
            character.Stats.CurrentHp,
            character.Stats.MaxHp,
            character.Stats.HpRegenPerTick(),
            character.Stats.CurrentMp,
            character.Stats.MaxMp,
            character.Stats.MpRegenPerTick(),
            (float)character.Stats.MovementSpeed,
            (float)character.Stats.AttackSpeed,
            character.Stats.PhysicalAttack,
            character.Stats.MagicAttack,
            character.Stats.PhysicalDefense,
            character.Stats.MagicDefense);

        var entity = simulation.CreatePlayer(playerData);
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
        
        simulation.DestroyPlayer(session.Entity);
        logger.LogInformation("Despawned player {Name}", character.Name);
        session.Entity = Entity.Null;
    }

    public PlayerData BuildSnapshot(PlayerSession session)
    {
        var character = session.SelectedCharacter 
            ?? throw new InvalidOperationException("No character selected for session.");
        
        if (!simulation.World.TryBuildPlayerSnapshot(session.Entity, out PlayerData snapshot))
        {
            return new PlayerData(
                session.Account.Id,
                session.Peer.Id,
                character.Name,
                (byte)character.Gender,
                (byte)character.Vocation,
                character.PositionX,
                character.PositionY,
                character.PositionZ,
                character.FacingX,
                character.FacingY,
                character.Stats.CurrentHp,
                character.Stats.MaxHp,
                character.Stats.HpRegenPerTick(),
                character.Stats.CurrentMp,
                character.Stats.MaxMp,
                character.Stats.MpRegenPerTick(),
                (float)character.Stats.MovementSpeed,
                (float)character.Stats.AttackSpeed,
                character.Stats.PhysicalAttack,
                character.Stats.MagicAttack,
                character.Stats.PhysicalDefense,
                character.Stats.MagicDefense
            );
        }
        return snapshot;
    }
}
