using Game.ECS.Components;
using Game.ECS.Entities.Npc;
using Game.ECS.Entities.Player;
using Game.Network.Packets.Game;

namespace Game.Core.Extensions;

/// <summary>
/// Extension methods for converting between ECS snapshots and network packet types.
/// </summary>
public static class SnapshotConversionExtensions
{
    #region Player Conversions (ECS to Network)
    
    /// <summary>
    /// Converts a PlayerSnapshot to a PlayerSpawn network packet.
    /// </summary>
    public static PlayerSpawn ToPlayerSpawn(this PlayerSnapshot snapshot)
    {
        return new PlayerSpawn(
            PlayerId: snapshot.PlayerId,
            NetworkId: snapshot.NetworkId,
            MapId: snapshot.MapId,
            Name: snapshot.Name,
            Gender: snapshot.GenderId,
            Vocation: snapshot.VocationId,
            X: snapshot.PosX,
            Y: snapshot.PosY,
            Floor: snapshot.Floor,
            DirX: snapshot.DirX,
            DirY: snapshot.DirY,
            Hp: snapshot.Hp,
            MaxHp: snapshot.MaxHp,
            Mp: snapshot.Mp,
            MaxMp: snapshot.MaxMp,
            MovementSpeed: snapshot.MovementSpeed,
            AttackSpeed: snapshot.AttackSpeed,
            PhysicalAttack: snapshot.PhysicalAttack,
            MagicAttack: snapshot.MagicAttack,
            PhysicalDefense: snapshot.PhysicalDefense,
            MagicDefense: snapshot.MagicDefense
        );
    }
    
    /// <summary>
    /// Converts a PlayerStateSnapshot to a PlayerStateUpdate network packet.
    /// </summary>
    public static StateUpdate ToPlayerStateSnapshot(this StateSnapshot snapshot)
    {
        return new StateUpdate(
            NetworkId: snapshot.NetworkId,
            X: snapshot.PosX,
            Y: snapshot.PosY,
            Floor: snapshot.Floor,
            Speed: snapshot.Speed,
            DirX: snapshot.DirX,
            DirY: snapshot.DirY
        );
    }
    
    /// <summary>
    /// Converts a PlayerVitalsSnapshot to a PlayerVitalsUpdate network packet.
    /// </summary>
    public static VitalsUpdate ToPlayerVitalsSnapshot(this VitalsSnapshot snapshot)
    {
        return new VitalsUpdate(
            NetworkId: snapshot.NetworkId,
            CurrentHp: snapshot.Hp,
            MaxHp: snapshot.MaxHp,
            CurrentMp: snapshot.Mp,
            MaxMp: snapshot.MaxMp
        );
    }
    
    #endregion
    
    #region Player Conversions (Network to ECS)
    
    /// <summary>
    /// Converts a PlayerSpawn network packet to a PlayerSnapshot.
    /// </summary>
    public static PlayerSnapshot ToPlayerData(this PlayerSpawn spawn)
    {
        return new PlayerSnapshot(
            PlayerId: spawn.PlayerId,
            NetworkId: spawn.NetworkId,
            MapId: spawn.MapId,
            Name: spawn.Name,
            GenderId: spawn.Gender,
            VocationId: spawn.Vocation,
            PosX: spawn.X,
            PosY: spawn.Y,
            Floor: spawn.Floor,
            DirX: spawn.DirX,
            DirY: spawn.DirY,
            Hp: spawn.Hp,
            MaxHp: spawn.MaxHp,
            Mp: spawn.Mp,
            MaxMp: spawn.MaxMp,
            MovementSpeed: spawn.MovementSpeed,
            AttackSpeed: spawn.AttackSpeed,
            PhysicalAttack: spawn.PhysicalAttack,
            MagicAttack: spawn.MagicAttack,
            PhysicalDefense: spawn.PhysicalDefense,
            MagicDefense: spawn.MagicDefense
        );
    }
    
    /// <summary>
    /// Converts a PlayerStateUpdate network packet to a PlayerStateSnapshot.
    /// </summary>
    public static StateSnapshot ToPlayerStateData(this StateUpdate update)
    {
        return new StateSnapshot(
            NetworkId: update.NetworkId,
            PosX: update.X,
            PosY: update.Y,
            Floor: update.Floor,
            Speed: update.Speed,
            DirX: update.DirX,
            DirY: update.DirY
        );
    }
    
    /// <summary>
    /// Converts a PlayerVitalsUpdate network packet to a PlayerVitalsSnapshot.
    /// </summary>
    public static VitalsSnapshot ToPlayerVitalsData(this VitalsUpdate update)
    {
        return new VitalsSnapshot(
            NetworkId: update.NetworkId,
            Hp: update.CurrentHp,
            MaxHp: update.MaxHp,
            Mp: update.CurrentMp,
            MaxMp: update.MaxMp
        );
    }
    
    #endregion
    
    #region NPC Conversions (ECS to Network)
    
    /// <summary>
    /// Converts an NpcSnapshot to an NpcSpawnRequest network packet.
    /// </summary>
    public static NpcSpawnRequest ToNpcSpawnData(this NpcSnapshot snapshot)
    {
        return new NpcSpawnRequest(
            NetworkId: snapshot.NetworkId,
            TemplateId: 0, // Template ID not stored in snapshot, would need lookup
            X: snapshot.X,
            Y: snapshot.Y,
            Floor: snapshot.Floor,
            DirectionX: snapshot.DirX,
            DirectionY: snapshot.DirY,
            CurrentHp: snapshot.Hp,
            MaxHp: snapshot.MaxHp
        );
    }
    
    /// <summary>
    /// Converts an NpcStateSnapshot to an NpcStateUpdate network packet.
    /// </summary>
    public static NpcStateUpdate ToNpcStateSnapshot(this NpcStateSnapshot snapshot)
    {
        return new NpcStateUpdate(
            NetworkId: snapshot.NetworkId,
            X: snapshot.X,
            Y: snapshot.Y,
            Speed: snapshot.Speed,
            DirectionX: snapshot.DirectionX,
            DirectionY: snapshot.DirectionY
        );
    }
    
    /// <summary>
    /// Converts an NpcVitalsSnapshot to an NpcVitalsUpdate network packet.
    /// </summary>
    public static NpcVitalsUpdate ToNpcHealthSnapshot(this NpcVitalsSnapshot snapshot)
    {
        return new NpcVitalsUpdate(
            NetworkId: snapshot.NetworkId,
            CurrentHp: snapshot.CurrentHp,
            CurrentMp: snapshot.CurrentMp
        );
    }
    
    #endregion
    
    #region NPC Conversions (Network to ECS)
    
    /// <summary>
    /// Converts an NpcSpawnRequest network packet to an NpcSnapshot.
    /// </summary>
    public static NpcSnapshot ToNpcData(this NpcSpawnRequest request)
    {
        return new NpcSnapshot(
            NetworkId: request.NetworkId,
            MapId: 0, // Not available in NpcSpawnRequest
            Name: $"NPC_{request.NetworkId}", // Not available in NpcSpawnRequest
            X: request.X,
            Y: request.Y,
            Floor: request.Floor,
            DirX: request.DirectionX,
            DirY: request.DirectionY,
            Hp: request.CurrentHp,
            MaxHp: request.MaxHp,
            Mp: 0, // Not available in NpcSpawnRequest
            MaxMp: 0 // Not available in NpcSpawnRequest
        );
    }
    
    /// <summary>
    /// Converts an NpcStateUpdate network packet to an NpcStateSnapshot.
    /// </summary>
    public static NpcStateSnapshot ToNpcStateData(this NpcStateUpdate update)
    {
        return new NpcStateSnapshot(
            NetworkId: update.NetworkId,
            X: update.X,
            Y: update.Y,
            Floor: 0, // Not available in NpcStateUpdate
            Speed: update.Speed,
            DirectionX: update.DirectionX,
            DirectionY: update.DirectionY
        );
    }
    
    /// <summary>
    /// Converts an NpcVitalsUpdate network packet to an NpcVitalsSnapshot.
    /// </summary>
    public static NpcVitalsSnapshot ToNpcVitalsData(this NpcVitalsUpdate update)
    {
        return new NpcVitalsSnapshot(
            NetworkId: update.NetworkId,
            CurrentHp: update.CurrentHp,
            MaxHp: 0, // Not available in NpcVitalsUpdate
            CurrentMp: update.CurrentMp,
            MaxMp: 0 // Not available in NpcVitalsUpdate
        );
    }
    
    #endregion
    
    #region Input Conversions
    
    /// <summary>
    /// Converts a PlayerInputPacket to an ECS Input component.
    /// </summary>
    public static Input ToPlayerInput(this PlayerInputPacket packet)
    {
        return new Input
        {
            InputX = packet.InputX,
            InputY = packet.InputY,
            Flags = packet.Flags
        };
    }
    
    /// <summary>
    /// Converts an ECS Input component to a PlayerInputPacket.
    /// </summary>
    public static PlayerInputPacket ToPlayerInputPacket(this Input input)
    {
        return new PlayerInputPacket(
            InputX: input.InputX,
            InputY: input.InputY,
            Flags: input.Flags
        );
    }
    
    #endregion
}
