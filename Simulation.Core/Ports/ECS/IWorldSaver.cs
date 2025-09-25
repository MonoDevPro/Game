using Simulation.Core.ECS.Components;

namespace Simulation.Core.Ports.ECS;

public interface IWorldSaver
{
    void StageSave(PlayerData data);
}