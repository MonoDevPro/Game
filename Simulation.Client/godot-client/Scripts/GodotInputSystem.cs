using Arch.Core;
using Godot;
using Simulation.Core.ECS.Components;

namespace GodotClient;

public partial class GodotInputSystem : Node
{
    private Entity _entity;
    private bool _hasEntity;
    
    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
        var gc = GameClient.Instance;
        var world = gc.World;

        if (!_hasEntity)
        {
            // Find local player entity by PlayerId
            var q = world.Query(new QueryDescription().WithAll<PlayerId>());
            foreach (ref var chunk in q.GetChunkIterator())
            {
                ref var entities = ref chunk.Entity(0);
                ref var ids = ref chunk.GetFirst<PlayerId>();
                foreach (var i in chunk)
                {
                    ref readonly var e = ref System.Runtime.CompilerServices.Unsafe.Add(ref entities, i);
                    ref var id = ref System.Runtime.CompilerServices.Unsafe.Add(ref ids, i);
                    if (id.Value == gc.LocalPlayerId)
                    {
                        _entity = e;
                        _hasEntity = true;
                        break;
                    }
                }
                if (_hasEntity) break;
            }
        }

        if (!_hasEntity || !world.IsAlive(_entity))
        {
            _hasEntity = false;
            return;
        }

        int dx = 0, dy = 0;
        // Allow arrows and WASD
        if (Input.IsKeyPressed(Key.Up) || Input.IsKeyPressed(Key.W)) dy -= 1;
        if (Input.IsKeyPressed(Key.Down) || Input.IsKeyPressed(Key.S)) dy += 1;
        if (Input.IsKeyPressed(Key.Left) || Input.IsKeyPressed(Key.A)) dx -= 1;
        if (Input.IsKeyPressed(Key.Right) || Input.IsKeyPressed(Key.D)) dx += 1;

        // Normalize to -1/0/1 for diagonals
        if (dx != 0 && dy != 0)
        {
            // keep as-is; server handles diagonal movement duration
        }

        // Push a MoveIntent once if not already moving
        if ((dx != 0 || dy != 0) && !world.Has<MoveIntent>(_entity) && !world.Has<MoveTarget>(_entity))
            world.Add(_entity, new MoveIntent(new Direction(dx, dy)));

        // Attack with Space
        if (Input.IsKeyPressed(Key.Space) && !world.Has<AttackIntent>(_entity) && !world.Has<AttackTimer>(_entity))
        {
            // Attack in current facing direction if available, otherwise to the right
            var dir = world.Has<Direction>(_entity) ? world.Get<Direction>(_entity) : new Direction(1, 0);
            world.Add(_entity, new AttackIntent(dir));
        }
    }
}