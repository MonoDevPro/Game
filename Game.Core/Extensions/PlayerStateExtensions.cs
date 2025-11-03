using Game.ECS.Entities.Factories;
using Game.Network.Packets.Game;

namespace Game.Core.Extensions;

public static class PlayerStateExtensions
{
    public static PlayerStateData ToPlayerStateData(this PlayerStatePacket packet)
    {
        return new PlayerStateData(
            packet.NetworkId,
            packet.Position.X,
            packet.Position.Y,
            packet.Position.Z,
            packet.Facing.DirectionX,
            packet.Facing.DirectionY);
    }   
    
    public static PlayerStatePacket ToPlayerStatePacket(this PlayerStateData playerStateData)
    {
        return new PlayerStatePacket(
            playerStateData.NetworkId,
            new ECS.Components.Position
            {
                X = playerStateData.PositionX,
                Y = playerStateData.PositionY,
                Z = playerStateData.PositionZ
            },
            new ECS.Components.Facing
            {
                DirectionX = playerStateData.FacingX,
                DirectionY = playerStateData.FacingY
            });
    }
}