using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Services;

/// Token de reserva seguro contra double-free e liberações por entidade errada.
public readonly record struct ReservationToken(Position Position, Entity Reserver, uint Version);

public interface ISpatialService
{
    // Ciclo de frame (opcional)
    void BeginFrame(uint tick);
    void EndFrame();

    // Ocupação de células
    void Insert(in Position position, in Entity entity);
    bool Remove(in Position position, in Entity entity);

    // Atualiza a posição do item (equivalente a Remove+Insert, porém atômico/otimizado)
    bool Update(in Position oldPosition, in Position newPosition, in Entity entity);

    // Movimento atômico: verifica/aplica numa única chamada (sem expor Remove/Insert separadamente)
    bool TryMove(in Position from, in Position to, in Entity entity);

    // Consultas sem alocação: escreve no buffer; retorna o número de itens escritos
    int QueryAt(in Position position, Span<Entity> results);
    int QueryArea(in Position minInclusive, in Position maxInclusive, Span<Entity> results);

    // Versões por callback (sem buffers, com early-exit retornando false)
    void ForEachAt(in Position position, Func<Entity, bool> visitor);
    void ForEachArea(in Position minInclusive, in Position maxInclusive, Func<Entity, bool> visitor);

    // Fast-path: obtém o primeiro ocupante (comum em checagens simples)
    bool TryGetFirstAt(in Position position, out Entity entity);

    // Reservas (antes do movimento): uso de token evita liberações incorretas
    bool TryReserve(in Position position, in Entity reserver, out ReservationToken token);
    bool ReleaseReservation(in ReservationToken token);

    // Limpeza total
    void Clear();
}
