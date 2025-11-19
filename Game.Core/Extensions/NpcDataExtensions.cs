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
            data.PositionX,
            data.PositionY,
            data.PositionZ,
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
            PositionX = snapshot.PositionX,
            PositionY = snapshot.PositionY,
            PositionZ = snapshot.PositionZ,
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
            data.FacingX,
            data.FacingY,
            data.Speed,
            data.CurrentHp,
            data.MaxHp);
    }

    public static NpcStateData ToNpcStateData(this NpcStateSnapshot snapshot)
    {
        return new NpcStateData
        {
            NetworkId = snapshot.NetworkId,
            PositionX = snapshot.PositionX,
            PositionY = snapshot.PositionY,
            PositionZ = snapshot.PositionZ,
            FacingX = snapshot.FacingX,
            FacingY = snapshot.FacingY,
            Speed = snapshot.Speed,
            CurrentHp = snapshot.CurrentHp,
            MaxHp = snapshot.MaxHp
        };
    }
}
