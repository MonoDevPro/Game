using Game.ECS.Navigation.Client.Contracts;

namespace GodotClient.Simulation.Contracts;

public sealed class GodotInputProvider : IInputProvider
{
    public bool IsClickPressed()
    {
        return Godot.Input.IsMouseButtonPressed(Godot.MouseButton.Left);
    }

    public (float X, float Y) GetClickWorldPosition()
    {
        var viewport = GameClient.Instance.GetViewport();
        var mousePos = viewport.GetMousePosition();
        var worldPos = viewport.GetCamera2D().GetGlobalMousePosition();
        return (worldPos.X, worldPos.Y);
    }

    public (float X, float Y) GetMovementAxis()
    {
        return (
            Godot.Input.GetActionStrength("move_right") - Godot.Input.GetActionStrength("move_left"),
            Godot.Input.GetActionStrength("move_down") - Godot.Input.GetActionStrength("move_up")
        );
    }
}