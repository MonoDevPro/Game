namespace Game.ECS.Components;

public struct Health { public int Current; public int Max; public float RegenerationRate; }
public struct Mana { public int Current; public int Max; public float RegenerationRate; }
public struct MovementSpeed { public float BaseSpeed; public float CurrentModifier; }