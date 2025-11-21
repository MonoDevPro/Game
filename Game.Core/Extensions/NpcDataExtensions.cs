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
            data.PositionZ,
            data.FacingX,
            data.FacingY,
            data.Hp,
            data.MaxHp,
            data.HpRegen,
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
            PositionZ = snapshot.PositionZ,
            FacingX = snapshot.FacingX,
            FacingY = snapshot.FacingY,
            Hp = snapshot.Hp,
            MaxHp = snapshot.MaxHp,
            HpRegen = snapshot.HpRegen,
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
            data.PositionZ,
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
            PositionZ = snapshot.PositionZ,
            VelocityX = snapshot.VelocityX,
            VelocityY = snapshot.VelocityY,
            FacingX = snapshot.FacingX,
            FacingY = snapshot.FacingY,
            Speed = snapshot.Speed,
        };
    }
    
    public static NpcHealthSnapshot ToNpcHealthSnapshot(this NpcHealthData data)
    {
        return new NpcHealthSnapshot(
            data.NetworkId,
            data.CurrentHp,
            data.MaxHp);
    }
    
    public static NpcHealthData ToNpcHealthData(this NpcHealthSnapshot snapshot)
    {
        return new NpcHealthData
        {
            NetworkId = snapshot.NetworkId,
            CurrentHp = snapshot.CurrentHp,
            MaxHp = snapshot.MaxHp
        };
    }
    
}
