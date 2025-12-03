using Arch.Core;
using Game.Domain.Enums;
using Game.ECS.Entities.Player;
using Game.ECS.Schema.Components;
using Game.ECS.Schema.Templates;
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

        var playerTemplate = new PlayerTemplate(
            Id: session.Account.Id,
            IdentityTemplate: new IdentityTemplate(
                NetworkId: session.Peer.Id,
                Name: character.Name,
                Gender: (Gender)character.Gender,
                Vocation: (VocationType)character.Vocation
            ),
            LocationTemplate: new LocationTemplate(
                MapId: 0,
                Floor: character.Floor,
                X: character.PositionX,
                Y: character.PositionY
            ),
            DirectionTemplate: new DirectionTemplate(
                DirX: character.FacingX,
                DirY: character.FacingY
            ),
            VitalsTemplate: new VitalsTemplate(
                CurrentHp: character.Stats.CurrentHp,
                MaxHp: character.Stats.MaxHp,
                CurrentMp: character.Stats.CurrentMp,
                MaxMp: character.Stats.MaxMp,
                HpRegen: character.Stats.HpRegenPerTick(),
                MpRegen: character.Stats.MpRegenPerTick()
            ),
            StatsTemplate: new StatsTemplate(
                MovementSpeed: (float)character.Stats.MovementSpeed,
                AttackSpeed: (float)character.Stats.AttackSpeed,
                PhysicalAttack: character.Stats.PhysicalAttack,
                MagicAttack: character.Stats.MagicAttack,
                PhysicalDefense: character.Stats.PhysicalDefense,
                MagicDefense: character.Stats.MagicDefense
            )
        );
        
        var entity = simulation.CreatePlayer(playerTemplate);
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
        
        simulation.DestroyEntity(networkId.Value);
        logger.LogInformation("Despawned player {Name}", character.Name);
        session.Entity = Entity.Null;
    }

    public PlayerSnapshot BuildSnapshot(PlayerSession session)
    {
        var character = session.SelectedCharacter 
                        ?? throw new InvalidOperationException("No character selected for session.");
        
        if (simulation.World.IsAlive(session.Entity))
        {
            return simulation.World
                .BuildPlayerSnapshot(session.Entity, simulation.Strings) with { Name = character.Name, };
        }
        return new PlayerSnapshot(
            PlayerId: session.Account.Id,
            NetworkId: session.Peer.Id,
            MapId: 0,
            Name: character.Name,
            GenderId: (byte)character.Gender,
            VocationId: (byte)character.Vocation,
            PosX: character.PositionX,
            PosY: character.PositionY,
            Floor: character.Floor,
            DirX: character.FacingX,
            DirY: character.FacingY,
            Hp: character.Stats.CurrentHp,
            MaxHp: character.Stats.MaxHp,
            Mp: character.Stats.CurrentMp,
            MaxMp: character.Stats.MaxMp,
            MovementSpeed: (float)character.Stats.MovementSpeed,
            AttackSpeed: (float)character.Stats.AttackSpeed,
            PhysicalAttack: character.Stats.PhysicalAttack,
            MagicAttack: character.Stats.MagicAttack,
            PhysicalDefense: character.Stats.PhysicalDefense,
            MagicDefense: character.Stats.MagicDefense
        );
    }
}