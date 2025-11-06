using Game.ECS.Entities.Factories;
using Game.Network.Packets.Game;

namespace Game.Core.Extensions;

public static class PlayerStateExtensions
{
    public static PlayerStateData ToPlayerStateData(this StatePacket packet)
    {
        return new PlayerStateData(
            packet.NetworkId,
            packet.Position.X,
            packet.Position.Y,
            packet.Position.Z,
            packet.Velocity.DirectionX,
            packet.Velocity.DirectionY,
            packet.Velocity.Speed,
            packet.Facing.DirectionX,
            packet.Facing.DirectionY);
    }   
    
    public static StatePacket ToPlayerStatePacket(this PlayerStateData playerStateData)
    {
        return new StatePacket(
            playerStateData.NetworkId,
            new ECS.Components.Position
            {
                X = playerStateData.PositionX,
                Y = playerStateData.PositionY,
                Z = playerStateData.PositionZ
            },
            new ECS.Components.Velocity
            {
                DirectionX = playerStateData.VelocityX,
                DirectionY = playerStateData.VelocityY,
                Speed = playerStateData.Speed
            },
            new ECS.Components.Facing
            {
                DirectionX = playerStateData.FacingX,
                DirectionY = playerStateData.FacingY
            });
    }
}