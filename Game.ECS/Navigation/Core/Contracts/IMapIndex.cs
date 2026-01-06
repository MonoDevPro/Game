using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Navigation.Core.Contracts;

public interface IMapIndex
{
    IMapGrid GetMapGrid(int mapId);
    IMapSpatial GetMapSpatial(int mapId);

    /// <summary>
    /// Registra um novo mapa no serviço.
    /// </summary>
    void RegisterMap(int mapId, IMapGrid grid, IMapSpatial spatial);

    void RegisterMap(int mapId, int width, int height, int layers = 1);

    /// <summary>
    /// Remove um mapa do serviço.
    /// </summary>
    void UnregisterMap(int mapId);

    /// <summary>
    /// Obtém todos os IDs de mapas registrados.
    /// </summary>
    IEnumerable<int> GetRegisteredMapIds();

    /// <summary>
    /// Verifica se um mapa está registrado.
    /// </summary>
    bool HasMap(int mapId);

    MovementResult ValidateMove(int mapId, Position targetPos, Entity movingEntity);
}