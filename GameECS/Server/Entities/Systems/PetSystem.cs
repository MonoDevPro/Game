using Arch.Core;
using GameECS.Shared.Combat.Components;
using GameECS.Shared.Entities.Components;
using GameECS.Shared.Entities.Data;
using GameECS.Shared.Navigation.Components;

namespace GameECS.Server.Entities.Systems;

/// <summary>
/// Sistema de comportamento de Pets.
/// </summary>
public sealed class PetBehaviorSystem : IDisposable
{
    private readonly World _world;
    private readonly QueryDescription _petQuery;

    public PetBehaviorSystem(World world)
    {
        _world = world;
        _petQuery = new QueryDescription()
            .WithAll<PetOwnership, PetBehavior, PetState, GridPosition>()
            .WithNone<Dead>();
    }

    public void Update(long tick)
    {
        _world.Query(in _petQuery, (Entity entity, ref PetOwnership ownership, ref PetBehavior behavior, ref PetState state, ref GridPosition position) =>
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
        });
    }

    public void Dispose() { }
}
