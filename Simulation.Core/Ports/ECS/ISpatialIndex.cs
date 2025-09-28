using System.Drawing;
using Arch.Core;
using Simulation.Core.ECS.Components;

namespace Simulation.Core.Ports.ECS;

/// <summary>
/// Interface de acesso ao índice espacial.
/// Mantida pelo SpatialIndexSystem; consumida pelos demais sistemas.
/// </summary>
public interface ISpatialIndex
{
    /// <summary>
    /// Adiciona a entidade ao índice espacial (assumindo ocupação de um tile).
    /// </summary>
    void Add(in Entity entity, in Position pos);

    /// <summary>
    /// Remove a entidade do índice espacial.
    /// </summary>
    void Remove(in Entity entity);
    
    bool CanMove(in Entity entity, in Position newPos);

    /// <summary>
    /// Tenta mover a entidade para a nova posição, respeitando colisões/bordas do mapa.
    /// Retorna true se aplicado; false se inválido.
    /// </summary>
    bool Move(in Entity entity, in Position newPos);

    /// <summary>
    /// Consulta entidades em um raio Manhattan/axis-aligned usando um retângulo centrado.
    /// Resultados são colocados na lista fornecida (limpando-a antes).
    /// </summary>
    void Query(Position center, int radius, List<Entity> results);

    /// <summary>
    /// Consulta entidades em um raio Manhattan/axis-aligned e retorna uma nova lista.
    /// </summary>
    List<Entity> Query(Position center, int radius);
}