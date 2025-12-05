using Game.ECS.Schema.Components;
using Game.ECS.Schema.Snapshots;
using Game.Network.Packets.Game;

namespace Game.Core.Extensions;

/// <summary>
/// Extension methods for converting between ECS snapshots and network packet types.
/// </summary>
public static class SnapshotConversionExtensions
{
    
    #region State
    
    public static StateSnapshot ToStateSnapshot(this StateData update)
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
    
    public static StateData ToStateData(this StateSnapshot snapshot)
    {
        return new StateData(
            NetworkId: snapshot.NetworkId,
            X: snapshot.PosX,
            Y: snapshot.PosY,
            Floor: snapshot.Floor,
            Speed: snapshot.Speed,
            DirX: snapshot.DirX,
            DirY: snapshot.DirY
        );
    }
    
    #endregion
    
    #region Vitals
    
    public static VitalsSnapshot ToVitalsSnapshot(this VitalsData update)
    {
        return new VitalsSnapshot(
            NetworkId: update.NetworkId,
            Hp: update.CurrentHp,
            MaxHp: update.MaxHp,
            Mp: update.CurrentMp,
            MaxMp: update.MaxMp
        );
    }
    
    public static VitalsData ToVitalsData(this VitalsSnapshot snapshot)
    {
        return new VitalsData(
            NetworkId: snapshot.NetworkId,
            CurrentHp: snapshot.Hp,
            MaxHp: snapshot.MaxHp,
            CurrentMp: snapshot.Mp,
            MaxMp: snapshot.MaxMp
        );
    }
    
    #endregion
    
    #region Spawn Conversions (Player)
    
    /// <summary>
    /// Converts a PlayerSnapshot to a PlayerSpawn network packet.
    /// </summary>
    public static PlayerData ToPlayerData(this PlayerSnapshot snapshot)
    {
        return new PlayerData(
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
            HpRegen: snapshot.HpRegen,
            Mp: snapshot.Mp,
            MaxMp: snapshot.MaxMp,
            MpRegen: snapshot.MpRegen,
            MovementSpeed: snapshot.MovementSpeed,
            AttackSpeed: snapshot.AttackSpeed,
            PhysicalAttack: snapshot.PhysicalAttack,
            MagicAttack: snapshot.MagicAttack,
            PhysicalDefense: snapshot.PhysicalDefense,
            MagicDefense: snapshot.MagicDefense
        );
    }
    
    /// <summary>
    /// Converts a PlayerSpawn network packet to a PlayerSnapshot.
    /// </summary>
    public static PlayerSnapshot ToPlayerSnapshot(this PlayerData data)
    {
        return new PlayerSnapshot(
            PlayerId: data.PlayerId,
            NetworkId: data.NetworkId,
            MapId: data.MapId,
            Name: data.Name,
            GenderId: data.Gender,
            VocationId: data.Vocation,
            PosX: data.X,
            PosY: data.Y,
            Floor: data.Floor,
            DirX: data.DirX,
            DirY: data.DirY,
            Hp: data.Hp,
            MaxHp: data.MaxHp,
            HpRegen: data.HpRegen,
            Mp: data.Mp,
            MaxMp: data.MaxMp,
            MpRegen: data.MpRegen,
            MovementSpeed: data.MovementSpeed,
            AttackSpeed: data.AttackSpeed,
            PhysicalAttack: data.PhysicalAttack,
            MagicAttack: data.MagicAttack,
            PhysicalDefense: data.PhysicalDefense,
            MagicDefense: data.MagicDefense
        );
    }
    
    #endregion
    
    #region Spawn Conversions (NPC)
    
    /// <summary>
    /// Converts an NpcSnapshot to an NpcSpawnRequest network packet.
    /// </summary>
    public static NpcData ToNpcData(this NpcSnapshot snapshot)
    {
        return new NpcData(
            NpcId: snapshot.NpcId,
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
            HpRegen: snapshot.HpRegen,
            Mp: snapshot.Mp,
            MaxMp: snapshot.MaxMp,
            MpRegen: snapshot.MpRegen,
            MovementSpeed: snapshot.MovementSpeed,
            AttackSpeed: snapshot.AttackSpeed,
            PhysicalAttack: snapshot.PhysicalAttack,
            MagicAttack: snapshot.MagicAttack,
            PhysicalDefense: snapshot.PhysicalDefense,
            MagicDefense: snapshot.MagicDefense
        );
            
    }
    /// <summary>
    /// Converts an NpcSpawnRequest network packet to an NpcSnapshot.
    /// </summary>
    public static NpcSnapshot ToNpcSnapshot(this NpcData request)
    {
        return new NpcSnapshot(
            NpcId: request.NpcId,
            NetworkId: request.NetworkId,
            MapId: request.MapId,
            Name: request.Name,
            GenderId: request.Gender,
            VocationId: request.Vocation,
            PosX: request.X,
            PosY: request.Y,
            Floor: request.Floor,
            DirX: request.DirX,
            DirY: request.DirY,
            Hp: request.Hp,
            MaxHp: request.MaxHp,
            HpRegen: request.HpRegen,
            Mp: request.Mp,
            MaxMp: request.MaxMp,
            MpRegen: request.MpRegen,
            MovementSpeed: request.MovementSpeed,
            AttackSpeed: request.AttackSpeed,
            PhysicalAttack: request.PhysicalAttack,
            MagicAttack: request.MagicAttack,
            PhysicalDefense: request.PhysicalDefense,
            MagicDefense: request.MagicDefense
        );
    }
    
    #endregion
    
    #region Input Conversions

    /// <summary>
    /// Converts a PlayerInputPacket to an ECS Input component.
    /// </summary>
    public static Input ToPlayerInput(this InputPacket packet)
    {
        return new Input
        {
            InputX = packet.Input.InputX,
            InputY = packet.Input.InputY,
            Flags = packet.Input.Flags
        };
    }

    /// <summary>
    /// Converts an ECS Input component to a PlayerInputPacket.
    /// </summary>
    public static InputPacket ToPlayerInputPacket(this Input input)
    {
        return new InputPacket(new InputData(
            InputX: input.InputX,
            InputY: input.InputY,
            Flags: input.Flags));
    }
    
    #endregion
}