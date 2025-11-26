using Arch.Core;
using Game.Domain.Enums;
using Game.ECS.Components;

namespace Game.ECS.Logic;

public static partial class CombatLogic
{
    private const float MinAttacksPerSecond = 0.1f;
    private const float MaxAttacksPerSecond = 20f;
    private const int CriticalDamageMultiplier = 2;
    
    // ============================================
    // Constantes de Range por Vocação
    // ============================================
    private const int WarriorAttackRange = 1;   // Melee: 1-2 tiles
    private const int ArcherAttackRange = 7;    // Ranged físico: 5-8 tiles
    private const int MageAttackRange = 8;      // Ranged mágico: 6-10 tiles
    private const float ProjectileSpeed = 8f;   // Velocidade padrão de projéteis (tiles/s)
    private const float ProjectileLifetime = 3f; // Tempo máximo de vida do projétil (s)
    
    /// <summary>
    /// Calcula o dano total considerando ataque físico/mágico e defesa da vítima.
    /// </summary>
    public static int CalculateDamage(World world, in Entity attacker, in Entity targetEntity, AttackType attackType, bool isCritical)
    {
        if (!world.TryGet<CombatStats>(attacker, out var attackerStats) || !world.TryGet<CombatStats>(targetEntity, out var targetStats))
            return 0;
        
        bool isMagical = attackType == AttackType.Magic;
        
        int attackPower = isMagical ? attackerStats.MagicPower : attackerStats.AttackPower;
        int defensePower = isMagical ? targetStats.MagicDefense : targetStats.Defense;
        
        int multiplier = isCritical ? CriticalDamageMultiplier : 1;

        int baseDamage = Math.Max(1, attackPower - defensePower) * multiplier;
        float variance = 0.8f + (float)Random.Shared.NextDouble() * 0.4f;
        return (int)(baseDamage * variance);
    }
    
    /// <summary>
    /// Calcula dano para projétil usando apenas AttackPower (defesa aplicada no impacto).
    /// </summary>
    public static int CalculateProjectileDamage(World world, in Entity attacker, bool isMagical, bool isCritical)
    {
        if (!world.TryGet<CombatStats>(attacker, out var attackerStats))
            return 1;
        
        int attackPower = isMagical ? attackerStats.MagicPower : attackerStats.AttackPower;
        int multiplier = isCritical ? CriticalDamageMultiplier : 1;

        int baseDamage = Math.Max(1, attackPower) * multiplier;
        float variance = 0.8f + (float)Random.Shared.NextDouble() * 0.4f;
        return (int)(baseDamage * variance);
    }
    
    /// <summary>
    /// Retorna o estilo de ataque básico baseado na vocação.
    /// </summary>
    public static AttackStyle GetAttackStyleForVocation(VocationType vocation) => vocation switch
    {
        VocationType.Warrior => AttackStyle.Melee,
        VocationType.Archer  => AttackStyle.Ranged,
        VocationType.Mage    => AttackStyle.Magic,
        _ => AttackStyle.Melee // Default para vocações desconhecidas
    };
    
    /// <summary>
    /// Retorna o estilo de ataque básico baseado no ID de vocação (byte).
    /// </summary>
    public static AttackStyle GetAttackStyleForVocation(byte vocationId) 
        => GetAttackStyleForVocation((VocationType)vocationId);
    
    /// <summary>
    /// Retorna o tipo de ataque básico baseado na vocação.
    /// </summary>
    public static AttackType GetBasicAttackTypeForVocation(VocationType vocation) => vocation switch
    {
        VocationType.Warrior => AttackType.Basic,  // Melee básico
        VocationType.Archer  => AttackType.Basic,  // Ranged físico (projétil)
        VocationType.Mage    => AttackType.Magic,  // Ranged mágico (projétil)
        _ => AttackType.Basic
    };
    
    /// <summary>
    /// Retorna o tipo de ataque básico baseado no ID de vocação (byte).
    /// </summary>
    public static AttackType GetBasicAttackTypeForVocation(byte vocationId) 
        => GetBasicAttackTypeForVocation((VocationType)vocationId);
    
    /// <summary>
    /// Retorna o range de ataque baseado na vocação.
    /// </summary>
    public static int GetAttackRangeForVocation(VocationType vocation) => vocation switch
    {
        VocationType.Warrior => WarriorAttackRange,
        VocationType.Archer  => ArcherAttackRange,
        VocationType.Mage    => MageAttackRange,
        _ => WarriorAttackRange
    };
    
    /// <summary>
    /// Retorna o range de ataque baseado no ID de vocação (byte).
    /// </summary>
    public static int GetAttackRangeForVocation(byte vocationId) 
        => GetAttackRangeForVocation((VocationType)vocationId);
    
    /// <summary>
    /// Retorna se o estilo de ataque usa projétil (ranged).
    /// </summary>
    public static bool IsRangedAttackStyle(AttackStyle style) 
        => style == AttackStyle.Ranged || style == AttackStyle.Magic;
    
    /// <summary>
    /// Retorna se a vocação usa ataque ranged.
    /// </summary>
    public static bool IsRangedVocation(VocationType vocation) 
        => vocation == VocationType.Archer || vocation == VocationType.Mage;
    
    /// <summary>
    /// Retorna se a vocação usa ataque ranged (por ID).
    /// </summary>
    public static bool IsRangedVocation(byte vocationId) 
        => IsRangedVocation((VocationType)vocationId);
    
    /// <summary>
    /// Retorna se o ataque de uma vocação usa dano mágico.
    /// </summary>
    public static bool IsMagicalAttackForVocation(VocationType vocation) 
        => vocation == VocationType.Mage;
    
    /// <summary>
    /// Retorna se o ataque de uma vocação usa dano mágico (por ID).
    /// </summary>
    public static bool IsMagicalAttackForVocation(byte vocationId) 
        => IsMagicalAttackForVocation((VocationType)vocationId);
    
    /// <summary>
    /// Retorna a velocidade padrão de projétil.
    /// </summary>
    public static float GetProjectileSpeed() => ProjectileSpeed;
    
    /// <summary>
    /// Retorna o tempo de vida padrão de projétil.
    /// </summary>
    public static float GetProjectileLifetime() => ProjectileLifetime;
    
    /// <summary>
    /// Calcula o range de ataque com base no tipo de ataque (legado - mantido para compatibilidade).
    /// </summary>
    private static int GetAttackRange(AttackType type) => type switch
    {
        AttackType.Basic    => 1,
        AttackType.Heavy    => 1,
        AttackType.Critical => 1,
        AttackType.Magic    => 10,
        _ => 1
    };
    
    public static float GetAttackTypeSpeedMultiplier(AttackType type) => type switch
    {
        AttackType.Basic    => 1.00f,
        AttackType.Heavy    => 0.60f,
        AttackType.Critical => 0.80f,
        AttackType.Magic    => 0.90f,
        _ => 1.00f
    };
    
    private static DamageTimingPhase GetDamageTimingPhase(AttackType type) => type switch
    {
        AttackType.Basic    => DamageTimingPhase.Late,   // Dano no meio do golpe
        AttackType.Heavy    => DamageTimingPhase.Late,  // Dano no impacto final
        AttackType.Critical => DamageTimingPhase.Mid,   // Dano no momento crítico
        AttackType.Magic    => DamageTimingPhase.Early, // Dano quando lança o feitiço
        _ => DamageTimingPhase.Mid
    };
    
}