using Arch.System;

namespace Simulation.Core.ECS.Pipeline;

public class PipelineSystems : Group<float>
{
    public PipelineSystems(IServiceProvider provider, bool isServer) : base(isServer ? "ServerSystems" : "ClientSystems")
    {
        this.RegisterAttributedSystems<PipelineSystems, float>(provider, isServer);
    }
}