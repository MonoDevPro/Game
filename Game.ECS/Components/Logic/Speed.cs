namespace Game.ECS.Components;

public partial struct Speed
{
    public static Speed Zero => new Speed { Value = 0f };
    
    public bool IsZero() => Value == 0f;
    
    public override string ToString() => $"Speed(Value: {Value})";
}