using Arch.Core;
using Simulation.Core.Ports.ECS;

namespace Simulation.Core.ECS.Systems.Resources;

public class PlayerSaveResource(World world, IWorldSaver saver)
{
    private void SavePlayer(in Entity entity)
    {
        saver.StageSave(PlayerFactoryResource.ExtractPlayerData(world, entity)); 
    }
}
