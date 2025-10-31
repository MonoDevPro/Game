using Game.ECS.Entities.Factories;
using Game.Network.Packets.Game;

namespace Game.Core.Extensions;

public static class PlayerVitalsExtensions
{
    public static PlayerVitalsData ToPlayerVitalsData(this PlayerVitalsPacket packet)
    {
        return new PlayerVitalsData(
            packet.PlayerId,
            packet.Health.Current,
            packet.Health.Max,
            packet.Mana.Current,
            packet.Mana.Max);
    }
    
    public static PlayerVitalsPacket ToPlayerVitalsPacket(this PlayerVitalsData playerVitalsData)
    {
        return new PlayerVitalsPacket(
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