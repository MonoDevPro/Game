using Simulation.Core.ECS.Components;
using PlayerData = Simulation.Core.ECS.Components.Data.PlayerData;

namespace Simulation.Core.Ports.ECS;

public interface IWorldSaver
{
    void StageSave(PlayerData data);
}