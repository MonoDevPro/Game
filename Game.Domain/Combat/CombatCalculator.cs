using Game.Domain.Enums;
using Game.Domain.ValueObjects.Combat;

namespace Game.Domain.Combat;

/// <summary>
/// Centraliza todas as fórmulas de cálculo de combate do domínio.
/// </summary>
public static class CombatCalculator
{
    private const float BaseCriticalDamageMultiplier = 1.5f;
    private const float MinDamageMultiplier = 0.1f;
    private const int MinDamage = 1;
    
    /// <summary>
    /// Calcula o dano físico final após aplicar defesa.
    /// </summary>
    public static DamageResult CalculatePhysicalDamage(
        int attackerPhysicalAttack,
        int defenderPhysicalDefense,
        float criticalChance,
        float criticalDamageMultiplier = BaseCriticalDamageMultiplier,
        Random? random = null)
    {
        random ??= Random.Shared;
        
        // Rola crítico
        bool isCritical = random.NextDouble() * 100 < criticalChance;
        
        // Dano base (ataque - defesa/2, mínimo 10% do ataque)
        int baseDamage = Math.Max(
            (int)(attackerPhysicalAttack * MinDamageMultiplier),
            attackerPhysicalAttack - defenderPhysicalDefense / 2);
        
        // Aplica crítico
        int finalDamage = isCritical
            ? (int)(baseDamage * criticalDamageMultiplier)
            : baseDamage;
        
        // Variação de dano ±10%
        float variance = 0.9f + (float)random.NextDouble() * 0.2f;
        finalDamage = Math.Max(MinDamage, (int)(finalDamage * variance));
        
        return new DamageResult(finalDamage, DamageType.Physical, isCritical);
    }
    
    /// <summary>
    /// Calcula o dano mágico final após aplicar resistência.
    /// </summary>
    public static DamageResult CalculateMagicDamage(
        int attackerMagicAttack,
        int defenderMagicDefense,
        float criticalChance,
        float criticalDamageMultiplier = BaseCriticalDamageMultiplier,
        Random? random = null)
    {
        random ??= Random.Shared;
        
        bool isCritical = random.NextDouble() * 100 < criticalChance;
        
        // Dano mágico: ataque - defesa/3 (defesa mágica é menos efetiva)
        int baseDamage = Math.Max(
            (int)(attackerMagicAttack * MinDamageMultiplier),
            attackerMagicAttack - defenderMagicDefense / 3);
        
        int finalDamage = isCritical
            ? (int)(baseDamage * criticalDamageMultiplier)
            : baseDamage;
        
        float variance = 0.9f + (float)random.NextDouble() * 0.2f;
        finalDamage = Math.Max(MinDamage, (int)(finalDamage * variance));
        
        return new DamageResult(finalDamage, DamageType.Magical, isCritical);
    }
    
    /// <summary>
    /// Calcula dano verdadeiro (ignora defesas).
    /// </summary>
    public static DamageResult CalculateTrueDamage(int damage)
    {
        return new DamageResult(Math.Max(MinDamage, damage), DamageType.True, false);
    }
    
    /// <summary>
    /// Verifica se um ataque acerta baseado em destreza/evasão.
    /// </summary>
    public static bool RollHit(int attackerDexterity, int defenderDexterity, Random? random = null)
    {
        random ??= Random.Shared;
        
        // Base: 90% de acerto, modificado por diferença de destreza
        float hitChance = 90f + (attackerDexterity - defenderDexterity) * 2f;
        hitChance = Math.Clamp(hitChance, 20f, 99f);
        
        return random.NextDouble() * 100 < hitChance;
    }
    
    /// <summary>
    /// Calcula a quantidade de cura baseada em Spirit/Intelligence.
    /// </summary>
    public static int CalculateHealing(int spirit, int intelligence, int baseHeal)
    {
        // Cura = baseHeal + 50% do spirit + 25% da intelligence
        return baseHeal + spirit / 2 + intelligence / 4;
    }
    
    /// <summary>
    /// Calcula intervalo entre ataques em segundos.
    /// </summary>
    public static float CalculateAttackInterval(float baseAttackSpeed, float attackSpeedModifier)
    {
        // Base: 1 ataque por segundo
        // attackSpeed 1.0 = 1s, 2.0 = 0.5s, 0.5 = 2s
        float effectiveSpeed = baseAttackSpeed * (1f + attackSpeedModifier);
        return effectiveSpeed > 0 ? 1f / effectiveSpeed : 1f;
    }
    
    /// <summary>
    /// Calcula velocidade de movimento em tiles por segundo.
    /// </summary>
    public static float CalculateMovementSpeed(float baseSpeed, float speedModifier)
    {
        // Base: 4 tiles/segundo
        return 4f * baseSpeed * (1f + speedModifier);
    }
}
