using Game.Abstractions;
using Game.Domain.VOs;
using Godot;

namespace GodotClient.Systems;

public partial class InputManager : Node
{
    private const int TileSize = 32; // Tamanho do tile em pixels
    private GameClient? _gameClient;
    private float _accumulator = 0f;
    private const float InputInterval = 0.1f; // 10 inputs por segundo

    public void Attach(GameClient client)
    {
        _gameClient = client;
    }

    public void Detach()
    {
        _gameClient = null;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (_gameClient is null || !_gameClient.CanSendInput)
        {
            return;
        }
        
        _accumulator += (float)delta;
        if (_accumulator < InputInterval)
            return;
        _accumulator = 0f;

        // Lê input diretamente como -1, 0, 1
        sbyte moveX = 0;
        sbyte moveY = 0;
        
        if (Input.IsActionPressed("walk_west"))
            moveX = -1;
        else if (Input.IsActionPressed("walk_east"))
            moveX = 1;

        if (Input.IsActionPressed("walk_north"))
            moveY = -1;
        else if (Input.IsActionPressed("walk_south"))
            moveY = 1;
        
        ushort buttons = 0; // Nenhuma ação por enquanto
        if (Input.IsActionPressed("click_left"))
        {
            buttons |= (ushort)InputFlags.ClickLeft;
        }
        if (Input.IsActionPressed("click_right"))
        {
            buttons |= (ushort)InputFlags.ClickRight;
        }
        
        if (Input.IsActionPressed("attack"))
            buttons |= (ushort)InputFlags.Attack;
        
        if (Input.IsActionPressed("sprint"))
            buttons |= (ushort)InputFlags.Sprint;

        // Envia direto como sbyte (signed byte: -128 a 127)
        // IMPORTANTE: Apenas envia se houver input (evita spam de pacotes vazios)
        if (moveX != 0 || moveY != 0)
        {
            _gameClient.QueueInput(
                new GridOffset(moveX, moveY), 
                GetMouseGridPositionRelative(), 
                buttons);
        }
    }
    
    /// <summary>
    /// Obtém posição ABSOLUTA do mouse no grid (coordenadas mundo).
    /// </summary>
    private Coordinate GetMouseGridPositionAbsolute()
    {
        var mousePos = GetViewport().GetMousePosition();
    
        int gridX = Mathf.FloorToInt(mousePos.X / TileSize);
        int gridY = Mathf.FloorToInt(mousePos.Y / TileSize);
    
        return new Coordinate(gridX, gridY);
    }
    
    /// <summary>
    /// Obtém posição RELATIVA do mouse ao player local.
    /// </summary>
    private GridOffset GetMouseGridPositionRelative()
    {
        var localPlayer = _gameClient?.GetLocalPlayer;
        if (localPlayer is null)
            return GridOffset.Zero;

        var mousePos = localPlayer.GetLocalMousePosition();
        var playerPos = localPlayer.Position;
        var diff = mousePos - playerPos;

        return new GridOffset(
            (sbyte)Mathf.RoundToInt(diff.X / TileSize), 
            (sbyte)Mathf.RoundToInt(diff.Y / TileSize));
    }
}