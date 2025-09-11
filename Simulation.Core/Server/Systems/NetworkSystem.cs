using Arch.Core;
using Arch.System;
using Simulation.Core.Shared.Network;

namespace Simulation.Core.Server.Systems;

public partial class NetworkSystem(World world, NetworkManager network) : BaseSystem<World, float>(world)
{
    [Query]
    private void Tick([Data] in float dt)
    {
        // PollEvents deve ser chamado no thread principal
        network.PollEvents();
    }
}