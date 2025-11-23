using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Logic;

public static partial class PositionLogic
{
    public static int ManhattanDistance(this Position pos, Position other) => Math.Abs(pos.X - other.X) + Math.Abs(pos.Y - other.Y);
    public static int EuclideanDistanceSquared(this Position pos, Position other)
    {
        int deltaX = pos.X - other.X;
        int deltaY = pos.Y - other.Y;
        return deltaX * deltaX + deltaY * deltaY;
    }
    public static Position ToPosition(this SpatialPosition pos) => new(pos.X, pos.Y);
    public static SpatialPosition ToSpatialPosition(this Position pos, sbyte floor) => new(pos.X, pos.Y, floor);
    
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
        if (oldPosition == newPosition)
            return;

        world.Set<Position>(entity, newPosition);
        
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