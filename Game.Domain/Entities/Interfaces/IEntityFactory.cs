using Game.Domain.Player;

namespace Game.Domain.Entities.Interfaces;

/// <summary>
/// Interface para fábrica de entidades.
/// Implementado na camada ECS.
/// </summary>
public interface IEntityFactory
{
    /// <summary>
    /// Cria um Player no mundo.
    /// </summary>
    int CreatePlayer(PlayerSimulationAttributes attributes);

    /// <summary>
    /// Cria um NPC a partir de template.
    /// </summary>
    int CreateNpc(string templateId, int x, int y);

    /// <summary>
    /// Cria um Pet para um owner.
    /// </summary>
    int CreatePet(string templateId, int ownerEntityId, int x, int y);

    /// <summary>
    /// Cria um item no chão.
    /// </summary>
    int CreateDroppedItem(int itemId, int quantity, int x, int y, int? ownerEntityId = null);

    /// <summary>
    /// Obtém o próximo ID disponível.
    /// </summary>
    int PeekNextId();

    /// <summary>
    /// Define o próximo ID (para carregar de persistência).
    /// </summary>
    void SetNextId(int nextId);
}
