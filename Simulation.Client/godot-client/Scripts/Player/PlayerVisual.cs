using Game.Domain.Enums;
using Game.Domain.VOs;
using Game.Network.Packets;
using Godot;

namespace GodotClient.Player;

public sealed partial class PlayerVisual : Node2D
{
    private const float TileSize = 32f;
    
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

    public void UpdatePosition(Coordinate position)
    {
        Position = new Vector2(position.X * TileSize, position.Y * TileSize);
    }

    public void UpdateFacing(DirectionEnum facing)
    {
        var vector = DirectionToVector(facing);
        if (vector == Vector2.Zero)
            return;

        Rotation = vector.Angle();
    }

    private void UpdateAppearance()
    {
        _body.Color = _isLocal 
            ? Colors.Chartreuse 
            : Colors.CornflowerBlue;
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