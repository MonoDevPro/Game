using Arch.Core;
using Arch.System;
using Simulation.Core.Network;
using Simulation.Core.Options;

namespace Simulation.Core.ECS.Server.Systems;

/// <summary>
/// NetworkSystem (modo direto)
/// - chama _net.PollEvents() no Update (main thread)
/// - processa pacotes imediatamente no callback OnReceive (sem filas)
/// - mais simples e com menos alocação quando PollEvents roda no main thread
/// </summary>
public sealed class NetworkSystem(World world, NetworkManager manager, DebugOptions? opt = null) : BaseSystem<World, float>(world)
{
    public readonly NetworkManager Manager = manager;

    public override void Initialize()
    {
        if (opt is not null)
            Manager.InitializeDebug(opt);
        
        Manager.StartServer();
    }
    
    public override void Update(in float dt)
    {
        Manager.PollEvents();
    }

    public override void Dispose()
    {
        Manager.Stop();
        base.Dispose();
    }
}