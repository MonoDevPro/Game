using Arch.Core;
using Simulation.Core.ECS.Components;
using Simulation.Core.Ports.ECS;
using PlayerData = Simulation.Core.ECS.Components.Data.PlayerData;

namespace Simulation.Core.ECS.Resource;

public class PlayerSaveResource(World world, IWorldSaver saver)
{
    private void SavePlayer(in PlayerData data)
    {
        saver.StageSave(data); 
    }
}
