namespace Game.Domain.AOI.Interfaces;

/// <summary>
/// Interface para gerenciamento de Área de Interesse.
/// Implementado na camada ECS.
/// </summary>
public interface IAOIManager
{
    /// <summary>
    /// Obtém entidades visíveis por um observador.
    /// </summary>
    IReadOnlySet<int> GetVisibleEntities(int observerId);

    /// <summary>
    /// Verifica se uma entidade é visível para outra.
    /// </summary>
    bool IsVisible(int observerId, int targetId);

    /// <summary>
    /// Remove entidade do tracking de visibilidade.
    /// </summary>
    void RemoveEntity(int entityId);
}
