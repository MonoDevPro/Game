using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameECS.Client.Combat.Components;

namespace GameECS.Client.Combat.Systems;

/// <summary>
/// Sistema que interpola visualmente as barras de vida e mana.
/// </summary>
public sealed partial class ClientHealthBarSystem : BaseSystem<World, float>
{
    public ClientHealthBarSystem(World world) : base(world)
    {
    }

    [Query]
    [All<HealthBar>]
    private void UpdateHealthBar([Data] in float deltaTime, ref HealthBar bar)
    {
        bar.Update(deltaTime);
    }

    [Query]
    [All<ManaBar>]
    private void UpdateManaBar([Data] in float deltaTime, ref ManaBar bar)
    {
        bar.Update(deltaTime);
    }
}

/// <summary>
/// Sistema que atualiza textos flutuantes de dano.
/// </summary>
public sealed partial class ClientFloatingDamageSystem : BaseSystem<World, float>
{
    public ClientFloatingDamageSystem(World world) : base(world)
    {
    }

    [Query]
    [All<FloatingDamageBuffer>]
    private void UpdateFloatingDamage([Data] in float deltaTime, ref FloatingDamageBuffer buffer)
    {
        // Atualiza cada texto flutuante
        for (int i = 0; i < buffer.Count; i++)
        {
            var text = buffer.GetAt(i);
            text.Update(deltaTime);
            buffer.UpdateAt(i, text);
        }

        // Remove textos expirados
        buffer.RemoveExpired();
    }
}

/// <summary>
/// Sistema que atualiza animações de ataque.
/// </summary>
public sealed partial class ClientAttackAnimationSystem : BaseSystem<World, float>
{
    public ClientAttackAnimationSystem(World world) : base(world)
    {
    }

    [Query]
    [All<AttackAnimation>]
    private void UpdateAttackAnimation([Data] in float deltaTime, ref AttackAnimation animation)
    {
        animation.Update(deltaTime);
    }
}
