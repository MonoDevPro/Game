using Game.ECS.Entities.Factories;
using Game.Network.Packets.Game;

namespace Game.Core.Extensions;

public static class PlayerVitalsExtensions
{
    public static PlayerVitalsData ToPlayerVitalsData(this VitalsPacket packet)
    {
        return new PlayerVitalsData(
            packet.NetworkId,
            packet.Health.Current,
            packet.Health.Max,
            packet.Mana.Current,
            packet.Mana.Max);
    }
    
    public static VitalsPacket ToPlayerVitalsPacket(this PlayerVitalsData playerVitalsData)
    {
        return new VitalsPacket(
            playerVitalsData.NetworkId,
            new ECS.Components.Health
            {
                Current = playerVitalsData.CurrentHp,
                Max = playerVitalsData.MaxHp
            },
            new ECS.Components.Mana
            {
                Current = playerVitalsData.CurrentMp,
                Max = playerVitalsData.MaxMp
            });
    }
    
}