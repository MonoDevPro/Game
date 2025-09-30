using Application.Abstractions;
using Arch.Core;
using Simulation.Core.Ports.ECS;

namespace Simulation.Core.ECS.Resource;

public class PlayerSaveResource(World world, IWorldSaver saver)
{
    public void SavePlayer(PlayerData data)
    {
        saver.StageSave(data); 
    }
}
