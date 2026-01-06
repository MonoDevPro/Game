using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Navigation.Core.Contracts;

public interface IMapSpatial
{
    void Insert(Position position, in Entity entity);
    bool Remove(Position position, in Entity entity);
    bool Update(Position oldPosition, Position newPosition, in Entity entity);
    bool TryMove(Position from, Position to, in Entity entity);
    int QueryAt(Position position, Span<Entity> results);
    int QueryArea(Position min, Position max, Span<Entity> results);
    int QueryCircle(Position center, sbyte radius, Span<Entity> results);
    void ForEachAt(Position position, Func<Entity, bool> visitor);
    void ForEachArea(Position min, Position max, Func<Entity, bool> visitor);
    bool TryGetFirstAt(Position position, out Entity entity);
    void Clear();
}