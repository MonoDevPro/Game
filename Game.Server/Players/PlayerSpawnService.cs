using Arch.Core;
using Game.ECS.Components;
using Game.Network.Packets.Simulation;
using Game.Server.Sessions;
using Game.Server.Simulation;

namespace Game.Server.Players;

/// <summary>
/// Handles spawning and despawning of player entities within the simulation.
/// </summary>
public sealed class PlayerSpawnService(ServerSimulation simulation, ILogger<PlayerSpawnService> logger)
{
    public Entity SpawnPlayer(PlayerSession session)
    {
        var character = session.SelectedCharacter 
            ?? throw new InvalidOperationException("No character selected for session.");

        var entity = simulation.SpawnPlayer(session.Account.Id, session.Peer.Id,
            character.PositionX, character.PositionY, character.PositionZ,
            character.FacingX, character.FacingY,
            character.Stats.CurrentHp, character.Stats.MaxHp, character.Stats.HpRegenPerTick(),
            character.Stats.CurrentMp, character.Stats.MaxMp, character.Stats.MpRegenPerTick(),
            (float)character.Stats.MovementSpeed, (float)character.Stats.AttackSpeed,
            character.Stats.PhysicalAttack, character.Stats.MagicAttack,
            character.Stats.PhysicalDefense, character.Stats.MagicDefense);
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
        
        if (!simulation.TryGetPlayerState(session.Entity, out PlayerStateSnapshot snapshot))
        {
            return new PlayerSnapshot(session.Peer.Id, session.Account.Id, character.Id, character.Name, 
                (byte)character.Gender, (byte)character.Vocation, 
                character.PositionX, character.PositionY, character.PositionZ,
                character.FacingX, character.FacingY, (float)character.Stats.MovementSpeed,
                character.Stats.CurrentHp, character.Stats.CurrentMp, character.Stats.MaxHp, character.Stats.MaxMp,
                character.Stats.HpRegenPerTick(), character.Stats.MpRegenPerTick(),
                character.Stats.PhysicalAttack, character.Stats.MagicAttack,
                character.Stats.PhysicalDefense, character.Stats.MagicDefense,
                character.Stats.AttackSpeed, character.Stats.MovementSpeed);
        }
        
        if (!simulation.TryGetPlayerVitals(session.Entity, out PlayerVitalsSnapshot vitals))
        {
            vitals = new PlayerVitalsSnapshot(
                session.Peer.Id,
                character.Stats.CurrentHp,
                character.Stats.MaxHp,
                character.Stats.CurrentMp,
                character.Stats.MaxMp
            );
        }

        return new PlayerSnapshot(session.Peer.Id, session.Account.Id, character.Id, character.Name,
            (byte)character.Gender, (byte)character.Vocation, snapshot.PositionX, snapshot.PositionY, snapshot.PositionZ, snapshot.FacingX,
            snapshot.FacingY, snapshot.Speed,
            vitals.CurrentHp, vitals.CurrentMp, vitals.MaxHp, vitals.MaxMp,
            character.Stats.HpRegenPerTick(), character.Stats.MpRegenPerTick(),
            character.Stats.PhysicalAttack, character.Stats.MagicAttack,
            character.Stats.PhysicalDefense, character.Stats.MagicDefense,
            character.Stats.AttackSpeed, character.Stats.MovementSpeed);
    }
}
