using Arch.Core;
using Arch.LowLevel;
using Game.ECS.Components;

namespace Game.ECS.Services;

/// Token de reserva seguro contra double-free e liberações por entidade errada.
public readonly record struct ReservationToken(Position Position, Entity Reserver, uint Version);

public interface IMapSpatial
{
    // Ocupação de células
    void Insert(SpatialPosition position, in Entity entity);
    bool Remove(SpatialPosition position, in Entity entity);

    // Atualiza a posição do item (equivalente a Remove+Insert, porém atômico/otimizado)
    bool Update(SpatialPosition oldPosition, SpatialPosition newPosition, in Entity entity);

    // Movimento atômico: verifica/aplica numa única chamada (sem expor Remove/Insert separadamente)
    bool TryMove(SpatialPosition from, SpatialPosition to, in Entity entity);

    // Consultas sem alocação: escreve no buffer; retorna o número de itens escritos
    int QueryAt(SpatialPosition position, ref UnsafeStack<Entity> results);
    int QueryArea(SpatialPosition min, SpatialPosition max, Span<Entity> results);

    // Versões por callback (sem buffers, com early-exit retornando false)
    void ForEachAt(SpatialPosition position, Func<Entity, bool> visitor);
    void ForEachArea(SpatialPosition min, SpatialPosition max, Func<Entity, bool> visitor);

    // Fast-path: obtém o primeiro ocupante (comum em checagens simples)
    bool TryGetFirstAt(SpatialPosition position, out Entity entity);

    // Limpeza total
    void Clear();
}