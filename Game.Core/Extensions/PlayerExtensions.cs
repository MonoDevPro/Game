using Game.ECS.Entities.Data;
using Game.Network.Packets.Game;

namespace Game.Core.Extensions;

public static class PlayerExtensions
{
    public static PlayerData ToPlayerData(this PlayerSnapshot packet)
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

    public static PlayerSnapshot ToPlayerSpawnSnapshot(this PlayerData playerData)
    {
        return new PlayerSnapshot(
            playerData.PlayerId,
            playerData.NetworkId,
            playerData.Name,
            playerData.Gender,
            playerData.Vocation,
            playerData.PosX,
            playerData.PosY,
            playerData.PosZ,
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

    public static PlayerStateSnapshot ToPlayerStateSnapshot(this PlayerStateData data)
    {
        return new PlayerStateSnapshot(
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

    public static PlayerVitalsSnapshot ToPlayerVitalsSnapshot(this PlayerVitalsData data)
    {
        return new PlayerVitalsSnapshot(
            data.NetworkId,
            data.CurrentHp,
            data.MaxHp,
            data.CurrentMp,
            data.MaxMp);
    }

    public static PlayerStateData ToPlayerStateData(this PlayerStateSnapshot snapshot)
    {
        return new PlayerStateData
        {
            NetworkId = snapshot.NetworkId,
            PositionX = snapshot.PositionX,
            PositionY = snapshot.PositionY,
            PositionZ = snapshot.PositionZ,
            VelocityX = snapshot.VelocityX,
            VelocityY = snapshot.VelocityY,
            Speed = snapshot.Speed,
            FacingX = snapshot.FacingX,
            FacingY = snapshot.FacingY
        };
    }

    public static PlayerStateData[] ToPlayerStateData(this PlayerStatePacket packet)
    {
        PlayerStateData[] stateDataArray = new PlayerStateData[packet.States.Length];
        for (int i = 0; i < packet.States.Length; i++)
        {
            stateDataArray[i] = packet.States[i].ToPlayerStateData();
        }

        return stateDataArray;
    }

    public static PlayerVitalsData ToPlayerVitalsData(this PlayerVitalsSnapshot snapshot)
    {
        return new PlayerVitalsData
        {
            NetworkId = snapshot.NetworkId,
            CurrentHp = snapshot.CurrentHp,
            MaxHp = snapshot.MaxHp,
            CurrentMp = snapshot.CurrentMp,
            MaxMp = snapshot.MaxMp
        };
    }
}