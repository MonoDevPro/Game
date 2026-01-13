using Arch.Core;
using Game.DTOs.Player;
using Game.ECS.Components;
using Game.ECS.Entities;
using Game.Server.Sessions;
using Game.Server.Simulation;

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
        
        var data = new PlayerSnapshot(
            PlayerId: session.Account.Id,
            NetworkId: session.Peer.Id,
            MapId: character.MapId,
            Name: character.Name,
            Gender: (byte)character.Gender,
            Vocation: (byte)character.Vocation,
            X: character.PosX,
            Y: character.PosY,
            Z: character.PosZ,
            DirX: character.DirX,
            DirY: character.DirY,
            Hp: character.CurrentHp,
            MaxHp: character.MaxHp,
            HpRegen: character.HpRegenPerTick(),
            Mp: character.CurrentMp,
            MaxMp: character.MaxMp,
            MpRegen: character.MpRegenPerTick(),
            PhysicalAttack: character.PhysicalAttack,
            MagicAttack: character.MagicAttack,
            PhysicalDefense: character.PhysicalDefense,
            MagicDefense: character.MagicDefense
        );

        var entity = simulation.CreatePlayer(ref data);
        session.Entity = entity;

        logger.LogInformation("Spawned player {Name} at ({PosX}, {PosY})", character.Name, character.PosX, character.PosY);
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
        
        simulation.DestroyEntity(networkId.Value);
        logger.LogInformation("Despawned player {Name}", character.Name);
        session.Entity = Entity.Null;
    }

    public PlayerSnapshot BuildSnapshot(PlayerSession session)
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
        
        return new PlayerSnapshot(
            PlayerId: session.Account.Id,
            NetworkId: session.Peer.Id,
            MapId: character.MapId,
            Name: character.Name,
            Gender: (byte)character.Gender,
            Vocation: (byte)character.Vocation,
            X: character.PosX,
            Y: character.PosY,
            Z: character.PosZ,
            DirX: character.DirX,
            DirY: character.DirY,
            Hp: character.CurrentHp,
            MaxHp: character.MaxHp,
            HpRegen: character.HpRegenPerTick(),
            Mp: character.CurrentMp,
            MaxMp: character.MaxMp,
            MpRegen: character.MpRegenPerTick(),
            PhysicalAttack: character.PhysicalAttack,
            MagicAttack: character.MagicAttack,
            PhysicalDefense: character.PhysicalDefense,
            MagicDefense: character.MagicDefense
        );
    }
}