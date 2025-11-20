using Game.ECS.Components;
using Game.Network.Packets.Game;

namespace Game.Core.Extensions;

public static class PlayerInputExtensions
{
    public static PlayerInputPacket ToPlayerInputPacket(this Input inputData)
    {
        return new PlayerInputPacket(
            inputData.InputX,
            inputData.InputY,
            inputData.Flags);
    }
    
    public static Input ToPlayerInput(this PlayerInputPacket inputPacket)
    {
        return new Input
        {
            InputX = inputPacket.InputX,
            InputY = inputPacket.InputY,
            Flags = inputPacket.Flags
        };
    }
    
}