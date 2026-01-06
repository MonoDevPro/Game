using Game.Domain.Entities;

namespace Game.Server.Simulation.Maps;

/// <summary>
/// Loads maps that should be registered into the simulation (collision/spatial).
/// </summary>
public interface IMapLoader
{
    Task<IReadOnlyList<Map>> LoadAllAsync(CancellationToken cancellationToken = default);
}
