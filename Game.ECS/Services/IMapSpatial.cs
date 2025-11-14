using Arch.Core;
using Arch.LowLevel;
using Game.ECS.Components;

namespace Game.ECS.Services;

/// Token de reserva seguro contra double-free e liberações por entidade errada.
public readonly record struct ReservationToken(Position Position, Entity Reserver, uint Version);

public interface IMapSpatial
{
    // Ocupação de células
    void Insert(Position position, in Entity entity);
    bool Remove(Position position, in Entity entity);

    // Atualiza a posição do item (equivalente a Remove+Insert, porém atômico/otimizado)
    bool Update(Position oldPosition, Position newPosition, in Entity entity);

    // Movimento atômico: verifica/aplica numa única chamada (sem expor Remove/Insert separadamente)
    bool TryMove(Position from, Position to, in Entity entity);

    // Consultas sem alocação: escreve no buffer; retorna o número de itens escritos
    int QueryAt(Position position, ref UnsafeStack<Entity> results);
    int QueryArea(AreaPosition area, ref UnsafeStack<Entity> results);

    // Versões por callback (sem buffers, com early-exit retornando false)
    void ForEachAt(Position position, Func<Entity, bool> visitor);
    void ForEachArea(AreaPosition area, Func<Entity, bool> visitor);

    // Fast-path: obtém o primeiro ocupante (comum em checagens simples)
    bool TryGetFirstAt(Position position, out Entity entity);

    // Limpeza total
    void Clear();
}