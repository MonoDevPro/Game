
using GameWeb.Application.Players.Models;

namespace Simulation.Core.Ports.ECS;

public interface IWorldSaver
{
    void StageSave(PlayerDto dto);
}