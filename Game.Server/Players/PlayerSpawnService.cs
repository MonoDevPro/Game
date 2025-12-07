using Arch.Core;
using Game.DTOs.Game.Player;
using Game.ECS.Entities;
using Game.ECS.Entities.Components;
using Game.Server.ECS;
using Game.Server.Sessions;

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
        
        var playerSnapshot = new PlayerData(
            PlayerId: session.Account.Id,
            NetworkId: session.Peer.Id,
            MapId: 0,
            Name: character.Name,
            Gender: (byte)character.Gender,
            Vocation: (byte)character.Vocation,
            X: character.PositionX,
            Y: character.PositionY,
            Floor: character.Floor,
            DirX: character.FacingX,
            DirY: character.FacingY,
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

        var entity = simulation.CreatePlayer(ref playerSnapshot);
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

    public PlayerData BuildSnapshot(PlayerSession session)
    {
        var character = session.SelectedCharacter 
                        ?? throw new InvalidOperationException("No character selected for session.");
        
        var entity = session.Entity;
        
        if (simulation.World.IsAlive(entity))
        {
            return simulation.World
                .BuildPlayerSnapshot(
                    entity,
                    character.Name);
        }
        
        return new PlayerData(
            PlayerId: session.Account.Id,
            NetworkId: session.Peer.Id,
            MapId: 0,
            Name: character.Name,
            Gender: (byte)character.Gender,
            Vocation: (byte)character.Vocation,
            X: character.PositionX,
            Y: character.PositionY,
            Floor: character.Floor,
            DirX: character.FacingX,
            DirY: character.FacingY,
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