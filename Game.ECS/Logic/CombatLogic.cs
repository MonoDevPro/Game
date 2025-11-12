using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Services;

namespace Game.ECS.Logic;

public static partial class CombatLogic
{
    // Limites de sanidade para taxa de ataque (ataques por segundo)
    private const float MinAttacksPerSecond = 0.1f;
    private const float MaxAttacksPerSecond = 20f;

    public static float GetAttackTypeSpeedMultiplier(AttackType type) => type switch
    {
        AttackType.Basic    => 1.00f,
        AttackType.Heavy    => 0.60f,
        AttackType.Critical => 0.80f,
        AttackType.Magic    => 0.90f,
        _ => 1.00f
    };

    public static float CalculateAttackCooldownSeconds(in Attackable attackable, AttackType type = AttackType.Basic, float externalMultiplier = 1f)
    {
        float baseSpeed = MathF.Max(0.05f, attackable.BaseSpeed);
        float modifier  = MathF.Max(0.05f, attackable.CurrentModifier);
        float typeMul   = MathF.Max(0.05f, GetAttackTypeSpeedMultiplier(type));
        float extraMul  = MathF.Max(0.05f, externalMultiplier);

        float aps = baseSpeed * modifier * typeMul * extraMul;
        if (aps < MinAttacksPerSecond) aps = MinAttacksPerSecond;
        else if (aps > MaxAttacksPerSecond) aps = MaxAttacksPerSecond;

        return 1f / aps;
    }

    public static void ReduceCooldown(ref CombatState combat, float deltaTime)
    {
        if (combat.LastAttackTime <= 0f) return;
        combat.LastAttackTime = MathF.Max(0f, combat.LastAttackTime - deltaTime);
    }
}

public static partial class CombatLogic
{
    public static bool CanAttack(in CombatState combat) => combat.LastAttackTime <= 0f;
    
    /// <summary>
    /// Calcula o dano total considerando ataque físico/mágico e defesa da vítima.
    /// </summary>
    public static int CalculateDamage(in AttackPower attack, in Defense defense, bool isMagical = false)
    {
        int attackPower = isMagical ? attack.Magical : attack.Physical;
        int defensePower = isMagical ? defense.Magical : defense.Physical;

        int baseDamage = Math.Max(1, attackPower - defensePower);
        float variance = 0.8f + (float)Random.Shared.NextDouble() * 0.4f;
        return (int)(baseDamage * variance);
    }

    /// <summary>
    /// Realiza um ataque corpo-a-corpo com validações de cooldown e alcance.
    /// Agora retorna o dano aplicado via out damage para manter consistência com o que será enviado aos clientes.
    /// </summary>
    public static bool TryAttack(World world, Entity attacker, Entity target, AttackType attackType, out int damage)
    {
        damage = 0;

        if (!world.IsAlive(attacker) || !world.IsAlive(target))
            return false;

        if (!world.TryGet(attacker, out Position attackerPos) ||
            !world.TryGet(target, out Position targetPos) ||
            !world.TryGet(attacker, out AttackPower attackPower) ||
            !world.TryGet(target, out Defense defense) ||
            !world.TryGet(attacker, out CombatState combat) ||
            !world.TryGet(attacker, out Attackable attackable))
            return false;

        if (!CanAttack(in combat))
            return false;

        int distance = attackerPos.ManhattanDistance(targetPos);
        if (distance > SimulationConfig.MaxMeleeAttackRange)
            return false;

        if (world.Has<Dead>(target) || world.Has<Invulnerable>(target))
            return false;

        combat.LastAttackTime = CalculateAttackCooldownSeconds(in attackable, attackType);
        combat.InCombat = true;
        world.Set(attacker, combat);

        damage = CalculateDamage(attackPower, defense);
        return ApplyDamageInternal(world, target, damage, attacker);
    }

    public static bool TryDamage(World world, Entity target, int damage, Entity? attacker = null)
    {
        if (!world.IsAlive(target) || !world.Has<Health>(target))
            return false;

        if (damage <= 0)
            return false;

        return ApplyDamageInternal(world, target, damage, attacker);
    }

    public static bool TryHeal(World world, Entity target, int amount, Entity? healer = null)
    {
        if (!world.IsAlive(target))
            return false;

        if (!world.TryGet(target, out Health health))
            return false;

        int previous = health.Current;
        int newValue = Math.Min(health.Max, previous + amount);

        if (newValue == previous)
            return false;

        health.Current = newValue;
        world.Set(target, health);
        return true;
    }

    public static bool TryRestoreMana(World world, Entity target, int amount, Entity? source = null)
    {
        if (!world.IsAlive(target))
            return false;

        if (!world.TryGet(target, out Mana mana))
            return false;

        int previous = mana.Current;
        int newValue = Math.Min(mana.Max, previous + amount);

        if (newValue == previous)
            return false;

        mana.Current = newValue;
        world.Set(target, mana);
        return true;
    }
    
    private static bool ApplyDamageInternal(World world, Entity target, int damage, Entity? attacker)
    {
        ref Health health = ref world.Get<Health>(target);
        int previous = health.Current;
        int newValue = Math.Max(0, previous - damage);

        if (newValue == previous)
            return false;

        health.Current = newValue;
        world.Set(target, health);

        if (health.Current <= 0 && !world.Has<Dead>(target))
            world.Add<Dead>(target);

        return true;
    }
    
    public static bool TryFindNearestTarget(World world, IMapService mapService, in MapId playerMap, in Position playerPos, in Facing playerFacing, out Entity nearestTarget)
    {
        var spatial = mapService.GetMapSpatial(playerMap.Value);

        nearestTarget = Entity.Null;
        var targetPosition = new Position(
            playerPos.X + playerFacing.DirectionX,
            playerPos.Y + playerFacing.DirectionY,
            playerPos.Z);

        if (!spatial.TryGetFirstAt(targetPosition, out nearestTarget))
            return false;

        if (world.Has<Dead>(nearestTarget) || !world.Has<Attackable>(nearestTarget))
            return false;

        return true;
    }
    
}