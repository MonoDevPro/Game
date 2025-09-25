using Arch.Core;
using Arch.System;
using Simulation.Core.Ports.ECS;

namespace Simulation.Core.ECS.Sync;

// Recurso compartilhado entre o handler de rede e o sistema de aplicação no World.
// O handler enfileira; o sistema drena no Update em um estágio definido do pipeline.
public sealed class NetworkInbox<T> : ConcurrentInbox<ComponentSyncPacket<T>> where T : struct, IEquatable<T>;
public sealed class NetworkComponentApplySystem<T>(
    World world,
    NetworkInbox<T> inbox,
    IPlayerIndex playerIndex) : BaseSystem<World, float>(world)
    where T : struct, IEquatable<T>
{
    public override void Update(in float dt)
    {
        while (inbox.TryDequeue(out var pkt))
        {
            if (!playerIndex.TryGetPlayerEntity(pkt.PlayerId, out var e))
                continue;

            ref var comp = ref World.AddOrGet<T>(e);
            comp = pkt.Data;
        }
    }
}