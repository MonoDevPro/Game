using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Domain.AI.Enums;
using Game.Domain.AI.ValueObjects;
using Game.Domain.Combat.ValueObjects;
using Game.Domain.Commons.ValueObjects.Map;

namespace GameECS.Systems;

/// <summary>
/// Sistema de comportamento de Pets.
/// </summary>
public sealed partial class PetBehaviorSystem(World world) : BaseSystem<World, long>(world)
{
    [Query]
    [All<PetOwnership, PetBehavior, PetState, GridPosition>, None<Dead>]
    private void Update([Data] in long tick, in Entity entity, ref PetOwnership ownership, ref PetBehavior behavior, ref PetState state, ref GridPosition position)
    {
        if (!ownership.IsActive) return;

        // TODO: Obter posição do owner e atualizar comportamento
        switch (behavior.Mode)
        {
            case PetMode.Follow:
                // Seguir owner
                state.IsChasing = false;
                state.IsAttacking = false;
                break;

            case PetMode.Attack:
                // Atacar target do owner ou próximo
                break;

            case PetMode.Defend:
                // Defender owner
                break;

            case PetMode.Passive:
                // Ficar parado
                state.IsChasing = false;
                state.IsAttacking = false;
                state.TargetEntityId = 0;
                break;

            case PetMode.Aggressive:
                // Atacar qualquer inimigo próximo
                break;
        }
    }
}
