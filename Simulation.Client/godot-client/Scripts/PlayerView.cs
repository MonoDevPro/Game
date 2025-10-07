using System;
using System.Collections.Generic;
using Game.Domain.Enums;
using Game.Network.Packets;
using Godot;

namespace GodotClient;

public partial class PlayerView : Node2D
{
    private const float TileSize = 32f;

    private readonly Dictionary<int, PlayerVisual> _players = new();
    private Node2D? _world;
    private int _localNetworkId = -1;

    public override void _Ready()
    {
        base._Ready();
        _world = GetParent()?.GetNode<Node2D>("World");
    }

    public void SetLocalPlayer(PlayerSnapshot snapshot)
    {
        _localNetworkId = snapshot.NetworkId;
        ApplySnapshot(snapshot, true);
    }

    public void ApplySnapshot(PlayerSnapshot snapshot, bool isLocal)
    {
        var visual = GetOrCreateVisual(snapshot.NetworkId);
        var treatAsLocal = isLocal || snapshot.NetworkId == _localNetworkId;
        visual.Update(snapshot, treatAsLocal);
    }

    public void UpdateState(PlayerStatePacket packet)
    {
        if (_players.TryGetValue(packet.NetworkId, out var visual))
        {
            visual.UpdatePosition(packet.Position);
            visual.UpdateFacing(packet.Facing);
        }
    }

    public void RemovePlayer(int networkId)
    {
        if (_players.TryGetValue(networkId, out var visual))
        {
            visual.QueueFree();
            _players.Remove(networkId);
        }
    }

    public void Clear()
    {
        foreach (var visual in _players.Values)
        {
            visual.QueueFree();
        }

        _players.Clear();
        _localNetworkId = -1;
    }

    private PlayerVisual GetOrCreateVisual(int networkId)
    {
        if (_players.TryGetValue(networkId, out var visual))
        {
            return visual;
        }

        var world = EnsureWorld();
        visual = new PlayerVisual
        {
            Name = $"Player_{networkId}"
        };
        world.AddChild(visual);
        _players[networkId] = visual;
        return visual;
    }

    private Node2D EnsureWorld()
    {
        if (_world is null)
        {
            _world = GetParent()?.GetNode<Node2D>("World") ?? throw new InvalidOperationException("World node not found.");
        }

        return _world;
    }

    private sealed partial class PlayerVisual : Node2D
    {
        private readonly Polygon2D _body;
        private readonly Label _label;
        private bool _isLocal;

        public PlayerVisual()
        {
            _body = CreateBody();
            AddChild(_body);

            _label = new Label
            {
                Name = "NameLabel",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Text = string.Empty,
                Position = new Vector2(-TileSize * 0.5f, -TileSize * 1.2f)
            };
            _label.AddThemeColorOverride("font_color", Colors.White);
            AddChild(_label);
        }

        public void Update(PlayerSnapshot snapshot, bool isLocal)
        {
            _isLocal = isLocal;
            UpdateAppearance();
            UpdateLabel(snapshot.Name);
            UpdatePosition(snapshot.Position);
            UpdateFacing(snapshot.Facing);
        }

        public void UpdatePosition(GridPosition position)
        {
            Position = new Vector2(position.X * TileSize, position.Y * TileSize);
        }

        public void UpdateFacing(DirectionEnum facing)
        {
            var vector = DirectionToVector(facing);
            if (vector == Vector2.Zero)
            {
                return;
            }

            Rotation = vector.Angle();
        }

        private void UpdateAppearance()
        {
            _body.Color = _isLocal ? Colors.Chartreuse : Colors.CornflowerBlue;
        }

        private void UpdateLabel(string name)
        {
            _label.Text = name;
        }

        private static Polygon2D CreateBody()
        {
            var polygon = new Polygon2D
            {
                Polygon = new[]
                {
                    new Vector2(0, -TileSize * 0.5f),
                    new Vector2(TileSize * 0.4f, TileSize * 0.5f),
                    new Vector2(-TileSize * 0.4f, TileSize * 0.5f)
                },
                Antialiased = true,
                Color = Colors.CornflowerBlue
            };

            return polygon;
        }

        private static Vector2 DirectionToVector(DirectionEnum direction)
        {
            return direction switch
            {
                DirectionEnum.North => new Vector2(0, -1),
                DirectionEnum.NorthEast => new Vector2(1, -1).Normalized(),
                DirectionEnum.East => new Vector2(1, 0),
                DirectionEnum.SouthEast => new Vector2(1, 1).Normalized(),
                DirectionEnum.South => new Vector2(0, 1),
                DirectionEnum.SouthWest => new Vector2(-1, 1).Normalized(),
                DirectionEnum.West => new Vector2(-1, 0),
                DirectionEnum.NorthWest => new Vector2(-1, -1).Normalized(),
                _ => Vector2.Zero
            };
        }
    }
}
