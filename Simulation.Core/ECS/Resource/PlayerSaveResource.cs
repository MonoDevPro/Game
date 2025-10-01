using Arch.Core;
using GameWeb.Application.Players.Models;
using Simulation.Core.Ports.ECS;

namespace Simulation.Core.ECS.Resource;

public class PlayerSaveResource(World world, IWorldSaver saver)
{
    public void SavePlayer(PlayerDto dto)
    {
        saver.StageSave(dto); 
    }
}
