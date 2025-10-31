using Game.ECS.Entities.Factories;
using Game.Network.Packets.Game;

namespace Game.Core.Extensions;

public static class PlayerDataExtensions
{
    public static PlayerDataPacket ToPlayerDataPacket(this PlayerData playerDataPacket)
    {
        return new PlayerDataPacket(
            playerDataPacket.PlayerId,
            playerDataPacket.NetworkId,
            playerDataPacket.Name,
            playerDataPacket.Gender,
            playerDataPacket.Vocation,
            playerDataPacket.SpawnX,
            playerDataPacket.SpawnY,
            playerDataPacket.SpawnZ,
            playerDataPacket.FacingX,
            playerDataPacket.FacingY,
            playerDataPacket.Hp,
            playerDataPacket.MaxHp,
            playerDataPacket.HpRegen,
            playerDataPacket.Mp,
            playerDataPacket.MaxMp,
            playerDataPacket.MpRegen,
            playerDataPacket.MovementSpeed,
            playerDataPacket.AttackSpeed,
            playerDataPacket.PhysicalAttack,
            playerDataPacket.MagicAttack,
            playerDataPacket.PhysicalDefense,
            playerDataPacket.MagicDefense,
            playerDataPacket.MapId);
    }

    public static PlayerData ToPlayerData(this PlayerDataPacket packet)
    {
        return new PlayerData(
            packet.PlayerId,
            packet.NetworkId,
            packet.Name,
            packet.Gender,
            packet.Vocation,
            packet.PositionX,
            packet.PositionY,
            packet.PositionZ,
            packet.FacingX,
            packet.FacingY,
            packet.Hp,
            packet.MaxHp,
            packet.HpRegen,
            packet.Mp,
            packet.MaxMp,
            packet.MpRegen,
            packet.MovementSpeed,
            packet.AttackSpeed,
            packet.PhysicalAttack,
            packet.MagicAttack,
            packet.PhysicalDefense,
            packet.MagicDefense,
            packet.MapId);
    }
}