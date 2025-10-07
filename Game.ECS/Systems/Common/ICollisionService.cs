using Arch.Core;
using Game.Domain.VOs;

namespace Game.ECS.Systems.Common;

public interface ICollisionService
{
    // Retornar true se a entidade PUDER ocupar essa célula (ou seja, não é bloqueada)
    bool CanEnterCell(Entity e, Coordinate cell);
}