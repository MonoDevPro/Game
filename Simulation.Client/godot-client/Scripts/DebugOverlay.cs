using Arch.Core;
using Godot;
using Simulation.Core.ECS.Components;

namespace GodotClient;

public partial class DebugOverlay : Control
{
    private Label _label = null!;
    private bool _hasEntity;
    private Entity _entity;

    public override void _Ready()
    {
        _label = new Label
        {
            Position = new Vector2(8, 8),
            Size = new Vector2(400, 100)
        };
        AddChild(_label);
    }

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
            _label.Text = "Player not found";
            return;
        }

        var pos = world.Get<Position>(_entity);
        var hp = world.Get<Health>(_entity);
        var st = world.Get<PlayerState>(_entity);
        _label.Text = $"Pos=({pos.X},{pos.Y})  HP={hp.Current}/{hp.Max}  State={st.Flags}";
    }
}