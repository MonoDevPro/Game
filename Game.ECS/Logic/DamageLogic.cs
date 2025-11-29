using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Logic;

public static class DamageLogic
{
    
    
    public static void ApplyDeferredDamage(World world, in Entity targetEntity, int amount, bool isCritical = false, Entity? attacker = null)
    {
        ref var damaged = ref world.AddOrGet<Damaged>(targetEntity);
        
        damaged.Amount += amount;
        damaged.IsCritical = isCritical;
        damaged.SourceEntity = attacker ?? damaged.SourceEntity;
    }
    
    /// <summary>
    /// Aplica dano periódico baseado em taxa (float) + acumulação.
    /// - acumulated recebe a taxa * deltaTime;
    /// - quando acumulated >= 1, converte para dano inteiro;
    /// - aplica no valor atual, até mínimo 0;
    /// - retorna true se o valor foi alterado.
    /// </summary>
    /// <param name="current">Valor atual (HP, por exemplo).</param>
    /// <param name="damagePerSecond">Taxa de dano por segundo (float).</param>
    /// <param name="deltaTime">Delta de tempo.</param>
    /// <param name="accumulated">Acumulador de frações de dano.</param>
    public static bool ApplyPeriodicDamage(
        ref int current,
        float damagePerSecond,
        float deltaTime,
        ref float accumulated)
    {
        if (current <= 0)
        {
            accumulated = 0f;
            return false;
        }

        if (damagePerSecond <= 0f || deltaTime <= 0f)
            return false;

        accumulated += damagePerSecond * deltaTime;

        if (accumulated < 1.0f)
            return false;

        int damageToApply = (int)accumulated;
        if (damageToApply <= 0)
            return false;

        int previous = current;
        current = Math.Max(0, current - damageToApply);

        accumulated -= damageToApply;

        return current != previous;
    }
    
}