using Godot;

namespace GodotClient;

public partial class GodotInputSystem : Node
{
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
        int moveX = 0;
        if (Input.IsActionPressed("ui_left"))
            moveX = -1;
        else if (Input.IsActionPressed("ui_right"))
            moveX = 1;

        int moveY = 0;
        if (Input.IsActionPressed("ui_up"))
            moveY = -1;
        else if (Input.IsActionPressed("ui_down"))
            moveY = 1;

        // Envia direto como sbyte (signed byte: -128 a 127)
        // IMPORTANTE: Apenas envia se houver input (evita spam de pacotes vazios)
        if (moveX != 0 || moveY != 0)
        {
            _gameClient.QueueInput((sbyte)moveX, (sbyte)moveY, 0);
        }
    }
}