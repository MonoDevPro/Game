using Game.DTOs.Player;

namespace Game.ECS.Components;

public partial struct Input 
{ 
    public readonly bool HasInput() => InputX != 0 || InputY != 0 || Flags != InputFlags.None;
}