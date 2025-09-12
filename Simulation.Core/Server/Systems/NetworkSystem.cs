using Arch.Core;
using Arch.System;
using LiteNetLib;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.Shared.Network;
using Simulation.Core.Shared.Options;

namespace Simulation.Core.Server.Systems;

/// <summary>
/// NetworkSystem (modo direto)
/// - chama _net.PollEvents() no Update (main thread)
/// - processa pacotes imediatamente no callback OnReceive (sem filas)
/// - mais simples e com menos alocação quando PollEvents roda no main thread
/// </summary>
public sealed class NetworkSystem(World world, NetworkManager manager, DebugOptions opt) : BaseSystem<World, float>(world)
{
    public readonly NetworkManager Manager = manager;

    public override void Initialize()
    {
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