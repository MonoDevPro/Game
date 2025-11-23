using Game.ECS.Entities.Data;
using Game.Network.Packets.Game;

namespace Game.Core.Extensions;

public static class NpcDataExtensions
{
    public static NpcSpawnSnapshot ToNpcSpawnData(this NPCData data)
    {
        return new NpcSpawnSnapshot(
            data.NetworkId,
            data.MapId,
            data.Name,
            data.Gender,
            data.Vocation,
            data.PositionX,
            data.PositionY,
            data.Floor,
            data.FacingX,
            data.FacingY,
            data.Hp,
            data.MaxHp,
            data.HpRegen,
            data.Mp,
            data.MaxMp,
            data.MpRegen,
            data.MovementSpeed,
            data.AttackSpeed,
            data.PhysicalAttack,
            data.MagicAttack,
            data.PhysicalDefense,
            data.MagicDefense);
    }

    public static NPCData ToNpcData(this NpcSpawnSnapshot snapshot)
    {
        return new NPCData
        {
            NetworkId = snapshot.NetworkId,
            MapId = snapshot.MapId,
            Name = snapshot.Name,
            Gender = snapshot.Gender,
            Vocation = snapshot.Vocation,
            PositionX = snapshot.PositionX,
            PositionY = snapshot.PositionY,
            Floor = snapshot.Floor,
            FacingX = snapshot.FacingX,
            FacingY = snapshot.FacingY,
            Hp = snapshot.Hp,
            MaxHp = snapshot.MaxHp,
            HpRegen = snapshot.HpRegen,
            Mp = snapshot.Mp,
            MaxMp = snapshot.MaxMp,
            MpRegen = snapshot.MpRegen,
            MovementSpeed = snapshot.MovementSpeed,
            AttackSpeed = snapshot.AttackSpeed,
            PhysicalAttack = snapshot.PhysicalAttack,
            MagicAttack = snapshot.MagicAttack,
            PhysicalDefense = snapshot.PhysicalDefense,
            MagicDefense = snapshot.MagicDefense
        };
    }

    public static NpcStateSnapshot ToNpcStateSnapshot(this NpcStateData data)
    {
        return new NpcStateSnapshot(
            data.NetworkId,
            data.PositionX,
            data.PositionY,
            data.Floor,
            data.VelocityX,
            data.VelocityY,
            data.Speed,
            data.FacingX,
            data.FacingY);
    }

    public static NpcStateData ToNpcStateData(this NpcStateSnapshot snapshot)
    {
        return new NpcStateData
        {
            NetworkId = snapshot.NetworkId,
            PositionX = snapshot.PositionX,
            PositionY = snapshot.PositionY,
            Floor = snapshot.Floor,
            VelocityX = snapshot.VelocityX,
            VelocityY = snapshot.VelocityY,
            Speed = snapshot.Speed,
            FacingX = snapshot.FacingX,
            FacingY = snapshot.FacingY,
        };
    }
    
    public static NpcHealthSnapshot ToNpcHealthSnapshot(this NpcVitalsData data)
    {
        return new NpcHealthSnapshot(
            data.NetworkId,
            data.CurrentHp,
            data.MaxHp, 
            data.CurrentMp,
            data.MaxMp);
    }
    
    public static NpcVitalsData ToNpcHealthData(this NpcHealthSnapshot snapshot)
    {
        return new NpcVitalsData
        {
            NetworkId = snapshot.NetworkId,
            CurrentHp = snapshot.CurrentHp,
            MaxHp = snapshot.MaxHp,
            CurrentMp = snapshot.CurrentMp,
            MaxMp = snapshot.MaxMp
        };
    }
    
}
