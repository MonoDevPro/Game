using Application.Abstractions;

namespace Simulation.Core.Ports.ECS;

public interface IWorldSaver
{
    void StageSave(PlayerData data);
}