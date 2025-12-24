using Arch.Core;
using GameECS.Modules.Entities.Shared.Data;
using Game.Server.Sessions;
using GameECS.Server;
using GameECS.Shared.Combat.Components;
using GameECS.Shared.Entities.Components;

namespace Game.Server.Players;

/// <summary>
/// Handles spawning and despawning of player entities within the simulation.
/// </summary>
public sealed class PlayerSpawnService(
    ServerGameSimulation simulation, 
    ILogger<PlayerSpawnService> logger)
{
    public Entity SpawnPlayer(PlayerSession session)
    {
        var character = session.SelectedCharacter 
                        ?? throw new InvalidOperationException("No character selected for session.");
        
        var spawnData = new PlayerSpawnData(
            AccountId: session.Account.Id,
            CharacterId: character.Id,
            NetworkId: session.Peer.Id,
            Name: character.Name,
            X: character.PositionX,
            Y: character.PositionY,
            Level: character.Stats.Level,
            Vocation: (byte)character.Vocation,
            Health: character.Stats.CurrentHp,
            Mana: character.Stats.CurrentMp
        );

        var entity = simulation.CreatePlayerEntity(spawnData);
        session.Entity = entity;

        logger.LogInformation("Spawned player {Name} at ({PosX}, {PosY})", character.Name, character.PositionX, character.PositionY);
        return entity;
    }

    public void DespawnPlayer(PlayerSession session)
    {
        if (session.Entity == Entity.Null || !simulation.World.IsAlive(session.Entity))
            return;
        
        var character = session.SelectedCharacter 
                        ?? throw new InvalidOperationException("No character selected for session.");
        
        if (!simulation.World.TryGet<NetworkId>(session.Entity, out var networkId))
            return;
        
        simulation.DestroyPlayer(networkId.Value);
        logger.LogInformation("Despawned player {Name}", character.Name);
        session.Entity = Entity.Null;
    }

    public GameECS.Shared.Entities.Data.PlayerDto BuildSnapshot(PlayerSession session)
    {
        var character = session.SelectedCharacter 
                        ?? throw new InvalidOperationException("No character selected for session.");
        
        var entity = session.Entity;
        
        if (simulation.World.IsAlive(entity))
        {
            if (simulation.World.TryGet<Health>(entity, out var health) &&
                simulation.World.TryGet<Mana>(entity, out var mana))
            {
                character.Stats.CurrentHp = health.Current;
                character.Stats.CurrentMp = mana.Current;
            }
        }
        
        return new GameECS.Shared.Entities.Data.PlayerDto(
            PlayerId: session.Account.Id,
            NetworkId: session.Peer.Id,
            MapId: 0,
            Name: character.Name,
            Gender: character.Gender,
            Vocation: character.Vocation,
            X: character.PositionX,
            Y: character.PositionY,
            Z: character.PositionZ,
            Direction: character.Direction,
            Hp: character.Stats.CurrentHp,
            MaxHp: character.Stats.MaxHp,
            HpRegen: character.Stats.HpRegenPerTick(),
            Mp: character.Stats.CurrentMp,
            MaxMp: character.Stats.MaxMp,
            MpRegen: character.Stats.MpRegenPerTick(),
            MovementSpeed: (float)character.Stats.MovementSpeed,
            AttackSpeed: (float)character.Stats.AttackSpeed,
            PhysicalAttack: character.Stats.PhysicalAttack,
            MagicAttack: character.Stats.MagicAttack,
            PhysicalDefense: character.Stats.PhysicalDefense,
            MagicDefense: character.Stats.MagicDefense
        );
    }
}