using Arch.Core;
using Godot;
using Simulation.Core.ECS.Components;

namespace GodotClient;

public partial class PlayerView : Node2D
{
    [Export] public int TileSize = 32;
    [Export] public Color PlayerColor = new Color(0.2f, 0.8f, 0.2f);

    private Entity _entity;
    private bool _hasEntity;
    private Position _lastPos;

    public override void _Process(double delta)
    {
        var gc = GameClient.Instance;
        var world = gc.World;

        if (!_hasEntity)
        {
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

        var pos = world.Get<Position>(_entity);
        if (!_lastPos.Equals(pos))
        {
            _lastPos = pos;
            GlobalPosition = new Vector2(pos.X * TileSize, pos.Y * TileSize);
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 6f, PlayerColor);
    }
}