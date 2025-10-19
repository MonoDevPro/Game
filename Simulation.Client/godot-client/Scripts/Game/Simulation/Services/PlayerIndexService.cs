using System.Collections.Generic;
using Arch.Core;
using Game.ECS.Components;

namespace GodotClient.Game.Simulation.Services;

public sealed class PlayerIndexService(World world)
{
    private readonly Dictionary<int, Entity> _byId = new();

    public void RegisterPlayer(Entity e, int playerId)
    {
        if (world.IsAlive(e))
            _byId[playerId] = e;
    }

    public void UnregisterByEntity(Entity e)
    {
        if (!world.IsAlive(e)) return;
        // se tiver componente com id, remove
        if (world.Has<PlayerId>(e))
            _byId.Remove(world.Get<PlayerId>(e).Value);
    }

    public bool TryGet(int playerId, out Entity e) => _byId.TryGetValue(playerId, out e);
}