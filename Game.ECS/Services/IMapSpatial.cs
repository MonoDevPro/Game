using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Services;

public interface IMapSpatial
{
    // Ocupação de células
    void Insert(Position position, sbyte floor, in Entity entity);
    bool Remove(Position position, sbyte floor, in Entity entity);

    // Atualiza a posição do item (equivalente a Remove+Insert, porém atômico/otimizado)
    bool Update(Position oldPosition, sbyte oldFloor, Position newPosition, sbyte newFloor, in Entity entity);

    // Movimento atômico: verifica/aplica numa única chamada (sem expor Remove/Insert separadamente)
    bool TryMove(Position from, sbyte fromFloor, Position to, sbyte toFloor, in Entity entity);

    // Consultas sem alocação: escreve no buffer; retorna o número de itens escritos
    int QueryAt(Position position, sbyte floor, Span<Entity> results);
    int QueryArea(Position min, Position max, sbyte minFloor, sbyte maxFloor, Span<Entity> results);
    
    /// <summary>
    /// Query otimizada para área circular (ideal para percepção de NPCs).
    /// </summary>
    int QueryCircle(Position center, sbyte centerFloor, sbyte radius, Span<Entity> results);

    // Versões por callback (sem buffers, com early-exit retornando false)
    void ForEachAt(Position position, sbyte floor, Func<Entity, bool> visitor);
    void ForEachArea(Position min, Position max, sbyte minFloor, sbyte maxFloor, Func<Entity, bool> visitor);

    // Fast-path: obtém o primeiro ocupante (comum em checagens simples)
    bool TryGetFirstAt(Position position, sbyte floor, out Entity entity);

    // Limpeza total
    void Clear();
}