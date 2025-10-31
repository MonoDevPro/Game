using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

/// <summary>
/// Sistema responsável por processar input do jogador local.
/// Converte input em ações (movimento, ataque, habilidades, etc).
/// </summary>
public sealed partial class InputSystem(World world)
    : GameSystem(world)
{
    [Query]
    [All<PlayerControlled, Velocity>]
    [None<Dead>]
    private void ProcessPlayerInput(in Entity e,
        ref Velocity velocity, in Walkable speed, in PlayerInput input, [Data] in float _)
    {
        if (!input.HasInput())
        {
            velocity.Stop();
            return;
        }
        var (normalizedX, normalizedY) = NormalizeInput(input.InputX, input.InputY);
        velocity.DirectionX = normalizedX;
        velocity.DirectionY = normalizedY;
        velocity.Speed = ComputeCellsPerSecond(in speed, input.Flags);
    }
    
    private (sbyte x, sbyte y) NormalizeInput(sbyte inputX, sbyte inputY)
    {
        sbyte nx = inputX switch { < 0 => -1, > 0 => 1, _ => 0 };
        sbyte ny = inputY switch { < 0 => -1, > 0 => 1, _ => 0 };
        return (nx, ny);
    }
    
    private float ComputeCellsPerSecond(in Walkable walkable, in InputFlags flags)
    {
        float speed = walkable.BaseSpeed + walkable.CurrentModifier;
        if (flags.HasFlag(InputFlags.Sprint))
            speed *= 1.5f;
        return speed;
    }
}