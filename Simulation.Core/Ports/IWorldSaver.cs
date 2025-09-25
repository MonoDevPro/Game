using Simulation.Core.ECS.Components;

namespace Simulation.Core.Ports;

public interface IWorldSaver
{
    void StageSave(PlayerData data);
}