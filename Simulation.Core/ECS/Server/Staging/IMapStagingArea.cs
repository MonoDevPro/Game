using Simulation.Core.Models;

namespace Simulation.Core.ECS.Server.Staging;

/// <summary>
/// Interface representing a staging area for map-related operations.
/// </summary>
public interface IMapStagingArea
{
    /// <summary>
    /// Stages a map that has been loaded into the staging area.
    /// </summary>
    /// <param name="model">The map data to be staged.</param>
    void StageMapLoaded(MapModel model);
    
    /// <summary>
    /// Attempts to dequeue a loaded map from the staging area.
    /// </summary>
    /// <param name="template">
    /// When this method returns, contains the dequeued map data if the operation was successful; otherwise, null.
    /// </param>
    /// <returns>
    /// True if a map was successfully dequeued; otherwise, false.
    /// </returns>
    bool TryDequeueMapLoaded(out MapModel? template);
    
    /// <summary>
    /// Stages a map for saving into the staging area.
    /// </summary>
    /// <param name="model">The map data to be staged for saving.</param>
    void StageSave(MapModel model);
}