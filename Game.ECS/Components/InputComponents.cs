using Game.DTOs.Game.Player;

namespace Game.ECS.Components;

public struct Input
{
    public sbyte InputX; 
    public sbyte InputY; 
    public InputFlags Flags;
    
    public readonly bool HasInput() => InputX != 0 || InputY != 0 || Flags != InputFlags.None;
}