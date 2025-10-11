using Game.Domain.VOs;
using Game.Network.Packets.DTOs;
using Godot;

namespace GodotClient.Player;

public sealed partial class PlayerVisual : Node2D
{
    private const float TileSize = 32f;
    private const float MoveSpeed = 8f; // Velocidade de interpolação visual
    
    private readonly Polygon2D _body;
    private readonly Label _label;
    private bool _isLocal;
    
    private Vector2 _targetPosition; // Posição alvo (do servidor)
    private Vector2 _currentPosition; // Posição visual atual

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
        UpdatePosition(snapshot.Position, forceImmediate: false);
        UpdateFacing(snapshot.Facing);
    }

    public void UpdatePosition(Coordinate position, bool forceImmediate = false)
    {
        _targetPosition = new Vector2(position.X * TileSize, position.Y * TileSize);
        
        // Se é jogador local, teleporta direto (client-side prediction)
        if (forceImmediate)
        {
            _currentPosition = _targetPosition;
            Position = _currentPosition;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        // Interpolação suave apenas para jogadores remotos
        if (!_isLocal && _currentPosition != _targetPosition)
        {
            _currentPosition = _currentPosition.Lerp(_targetPosition, (float)delta * MoveSpeed);
            Position = _currentPosition;
        }
    }

    public void UpdateFacing(Coordinate facing)
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

    private static Vector2 DirectionToVector(Coordinate direction)
    {
        // 8 direções
        if (direction.X == 0 && direction.Y == -1) return new Vector2(0, -1); // Up
        if (direction.X == 1 && direction.Y == -1) return new Vector2(1, -1).Normalized(); // Up-Right
        if (direction.X == 1 && direction.Y == 0) return new Vector2(1, 0); // Right
        if (direction.X == 1 && direction.Y == 1) return new Vector2(1, 1).Normalized(); // Down-Right
        if (direction.X == 0 && direction.Y == 1) return new Vector2(0, 1); // Down
        if (direction.X == -1 && direction.Y == 1) return new Vector2(-1, 1).Normalized(); // Down-Left
        if (direction.X == -1 && direction.Y == 0) return new Vector2(-1, 0); // Left
        if (direction.X == -1 && direction.Y == -1) return new Vector2(-1, -1).Normalized(); // Up-Left

        return Vector2.Zero;
    }
}