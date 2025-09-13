using Simulation.Core.ECS.Shared.Data;

namespace Simulation.Core.ECS.Shared.Staging;

/// <summary>
/// Interface representing a staging area for map-related operations.
/// </summary>
public interface IMapStagingArea
{
    /// <summary>
    /// Stages a map that has been loaded into the staging area.
    /// </summary>
    /// <param name="data">The map data to be staged.</param>
    void StageMapLoaded(MapData data);
    
    /// <summary>
    /// Attempts to dequeue a loaded map from the staging area.
    /// </summary>
    /// <param name="data">
    /// When this method returns, contains the dequeued map data if the operation was successful; otherwise, null.
    /// </param>
    /// <returns>
    /// True if a map was successfully dequeued; otherwise, false.
    /// </returns>
    bool TryDequeueMapLoaded(out MapData data);
    
    /// <summary>
    /// Stages a map for saving into the staging area.
    /// </summary>
    /// <param name="data">The map data to be staged for saving.</param>
    void StageSave(MapData data);
}