using Arch.Core;

namespace Game.ECS.Components;

public struct DamageOverTime
{
    public float DamagePerSecond;
    public float RemainingTime;
    public float TotalDuration;
    public float AccumulatedDamage;
    
    // Opcional: tipo de dano, origem, etc.
    public bool IsMagical;
    public Entity Source; // se quiser saber quem aplicou
}