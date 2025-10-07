using Godot;

namespace GodotClient;

public partial class GodotInputSystem : Node
{
    private const float ResendIntervalSeconds = 0.2f;

    private GameClient? _gameClient;
    private Vector2I _lastDirection = Vector2I.Zero;
    private float _timeSinceLastSend;

    public void Attach(GameClient client)
    {
        _gameClient = client;
    }

    public void Detach()
    {
        _gameClient = null;
        _lastDirection = Vector2I.Zero;
        _timeSinceLastSend = 0f;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (_gameClient is null || !_gameClient.CanSendInput)
        {
            _lastDirection = Vector2I.Zero;
            _timeSinceLastSend = 0f;
            return;
        }

        var moveX = (sbyte)0;
        if (Input.IsActionPressed("ui_left"))
        {
            moveX -= 1;
        }
        if (Input.IsActionPressed("ui_right"))
        {
            moveX += 1;
        }

        var moveY = (sbyte)0;
        if (Input.IsActionPressed("ui_up"))
        {
            moveY -= 1;
        }
        if (Input.IsActionPressed("ui_down"))
        {
            moveY += 1;
        }

        var direction = new Vector2I(moveX, moveY);
        _timeSinceLastSend += (float)delta;

        if (direction != _lastDirection || _timeSinceLastSend >= ResendIntervalSeconds)
        {
            _gameClient.QueueInput((sbyte)direction.X, (sbyte)direction.Y, 0);
            _lastDirection = direction;
            _timeSinceLastSend = 0f;
        }
    }
}
