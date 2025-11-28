using Game.ECS.Components;
using Game.ECS.Entities.Npc;
using Game.ECS.Entities.Player;
using Game.Network.Packets.Game;

namespace Game.Core.Extensions;

/// <summary>
/// Extension methods for converting ECS snapshots to network packet types.
/// </summary>
public static class SnapshotConversionExtensions
{
    #region Player Conversions
    
    /// <summary>
    /// Converts a PlayerSnapshot to a PlayerSpawn network packet.
    /// </summary>
    public static PlayerSpawn ToPlayerSpawnSnapshot(this PlayerSnapshot snapshot)
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
    public static PlayerStateUpdate ToPlayerStateSnapshot(this PlayerStateSnapshot snapshot)
    {
        return new PlayerStateUpdate(
            NetworkId: snapshot.NetworkId,
            X: snapshot.PositionX,
            Y: snapshot.PositionY,
            Floor: snapshot.Floor,
            Speed: snapshot.Speed,
            DirX: snapshot.DirX,
            DirY: snapshot.DirY
        );
    }
    
    /// <summary>
    /// Converts a PlayerVitalsSnapshot to a PlayerVitalsUpdate network packet.
    /// </summary>
    public static PlayerVitalsUpdate ToPlayerVitalsSnapshot(this PlayerVitalsSnapshot snapshot)
    {
        return new PlayerVitalsUpdate(
            NetworkId: snapshot.NetworkId,
            CurrentHp: snapshot.Hp,
            MaxHp: snapshot.MaxHp,
            CurrentMp: snapshot.Mp,
            MaxMp: snapshot.MaxMp
        );
    }
    
    #endregion
    
    #region NPC Conversions
    
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
