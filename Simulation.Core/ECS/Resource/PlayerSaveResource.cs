using Arch.Core;
using Simulation.Core.ECS.Components.Data;
using Simulation.Core.Ports.ECS;

namespace Simulation.Core.ECS.Resource;

public class PlayerSaveResource(World world, IWorldSaver saver)
{
    public void SavePlayer(in PlayerData data)
    {
        saver.StageSave(data); 
    }
}
