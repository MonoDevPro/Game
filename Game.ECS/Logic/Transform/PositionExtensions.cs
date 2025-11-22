using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Logic;

public static partial class PositionLogic
{
    /// <summary>
    /// Atualiza a posição de uma entidade e marca para sincronização spatial.
    /// NOTE: OldPosition == default indica primeira vez que SetPosition foi chamado
    /// para uma entidade que ainda não tinha Position. Não é spawn inicial.
    /// </summary>
    public static void SetPosition(this World world, Entity entity, Position newPosition)
    {
        if (!world.TryGet(entity, out Position oldPosition))
        {
            // Primeira vez - apenas set
            world.Set(entity, newPosition);
            world.Add(entity, new PositionChanged 
            { 
                OldPosition = default, 
                NewPosition = newPosition 
            });
            return;
        }

        // Só marca se realmente mudou
        if (oldPosition.X == newPosition.X && 
            oldPosition.Y == newPosition.Y && 
            oldPosition.Z == newPosition.Z)
            return;

        world.Set(entity, newPosition);
        
        // Adiciona ou atualiza o componente de mudança
        if (world.Has<PositionChanged>(entity))
        {
            ref var change = ref world.Get<PositionChanged>(entity);
            change.NewPosition = newPosition;
        }
        else
        {
            world.Add(entity, new PositionChanged 
            { 
                OldPosition = oldPosition, 
                NewPosition = newPosition 
            });
        }
    }
}