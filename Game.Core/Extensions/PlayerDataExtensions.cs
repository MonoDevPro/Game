using Game.ECS.Entities.Factories;
using Game.Network.Packets.Game;

namespace Game.Core.Extensions;

public static class PlayerDataExtensions
{
    public static PlayerDataPacket ToPlayerDataPacket(this PlayerData playerData)
    {
        return new PlayerDataPacket(
            playerData.PlayerId,
            playerData.NetworkId,
            playerData.Name,
            playerData.Gender,
            playerData.Vocation,
            playerData.SpawnX,
            playerData.SpawnY,
            playerData.SpawnZ,
            playerData.FacingX,
            playerData.FacingY,
            playerData.Hp,
            playerData.MaxHp,
            playerData.HpRegen,
            playerData.Mp,
            playerData.MaxMp,
            playerData.MpRegen,
            playerData.MovementSpeed,
            playerData.AttackSpeed,
            playerData.PhysicalAttack,
            playerData.MagicAttack,
            playerData.PhysicalDefense,
            playerData.MagicDefense,
            playerData.MapId);
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