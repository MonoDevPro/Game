using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.DTOs.Game.Player;
using Game.ECS.Components;
using Game.ECS.Events;

namespace Game.ECS.Systems;

/// <summary>
/// Sistema responsável por processar input do jogador local.
/// Converte input em ações (movimento, ataque, habilidades, etc).
/// </summary>
public sealed partial class InputSystem(World world)
    : GameSystem(world)
{
    /*[Query]
    [All<Input, Walkable, Direction, Speed>]
    [None<Dead>]
    private void ProcessInput(in Entity entity, in Input input, ref Direction direction, ref Speed velocity, in Walkable walk)
    {
        var (nx, ny) = NormalizeInput(input.InputX, input.InputY);

        // No movement input: keep facing direction as-is.
        // This prevents "attack while standing still" from zeroing Direction and breaking melee targeting.
        if (nx == 0 && ny == 0)
        {
            velocity.Value = 0f;
            return;
        }

        var previousDir = direction;
        direction = new Direction { X = nx, Y = ny };

        if (previousDir.X != direction.X || previousDir.Y != direction.Y)
        {
            var dirChangedEvent = new DirectionChangedEvent(entity, previousDir, direction);
            EventBus.Send(ref dirChangedEvent);
        }

        velocity.Value = ComputeCellsPerSecond(in walk, in input.Flags);
    }
    
    public static float ComputeCellsPerSecond(in Walkable walkable, in InputFlags flags)
    {
        float speed = walkable.BaseSpeed + walkable.CurrentModifier;
        if (flags.HasFlag(InputFlags.Sprint))
            speed *= 1.5f;
        return speed;
    }
    
    /// <summary>
    /// Calcula o novo position e avalia se o movimento é permitido.
    /// Não realiza side-effects.
    /// </summary>
    public static (sbyte x, sbyte y) NormalizeInput(sbyte inputX, sbyte inputY)
    {
        sbyte nx = inputX switch { < 0 => -1, > 0 => 1, _ => 0 };
        sbyte ny = inputY switch { < 0 => -1, > 0 => 1, _ => 0 };
        return (nx, ny);
    }*/
}