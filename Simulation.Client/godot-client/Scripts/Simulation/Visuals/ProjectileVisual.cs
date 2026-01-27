using Godot;

namespace Game.Simulation.Visuals;

/// <summary>
/// Simple projectile visual (placeholder).
/// </summary>
[Tool]
public sealed partial class ProjectileVisual : Node2D
{
    private Color _color = new(1f, 0.85f, 0.25f);
    private float _radius = 4f;

    public static ProjectileVisual Create(Color? color = null)
    {
        var projectile = new ProjectileVisual();
        if (color.HasValue)
            projectile._color = color.Value;
        return projectile;
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, _radius, _color);
    }
}
