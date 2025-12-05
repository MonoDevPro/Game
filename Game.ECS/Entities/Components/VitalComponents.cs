namespace Game.ECS.Schema.Components;

// ============================================
// Vitals - Vida e Mana
// ============================================
public struct Health
{
    public int Current; 
    public int Max; 
    public float RegenerationRate; 
    public float AccumulatedRegeneration;
}

public struct Mana
{
    public int Current; 
    public int Max; 
    public float RegenerationRate; 
    public float AccumulatedRegeneration;
}