using Arch.Core;

namespace Game.ECS.Schema.Components;

public struct Invulnerable { }

public struct DeferredDamage
{
    public int Amount;
    public bool IsCritical;
    public Entity SourceEntity;
}

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