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
    [Query]
    [All<Input, Walkable, Direction, Speed>]
    [None<Dead>]
    private void ProcessInput(in Entity entity, ref Input input, ref Direction direction, ref Speed velocity, in Walkable walk)
    {
        NormalizeInput(ref input);

        // No movement input: keep facing direction as-is.
        // This prevents "attack while standing still" from zeroing Direction and breaking melee targeting.
        if (!input.HasInput())
        {
            velocity.Value = 0f;
            return;
        }
        
        // Movement input detected: update direction and speed.
        var previousDir = direction;
        direction.X = input.InputX;
        direction.Y = input.InputY;

        if (previousDir.X != direction.X || previousDir.Y != direction.Y)
        {
            var dirChangedEvent = new DirectionChangedEvent(entity, previousDir, direction);
            EventBus.Send(ref dirChangedEvent);
        }

        velocity.Value = ComputeCellsPerSecond(in walk, in input.Flags);
    }
    
    private static float ComputeCellsPerSecond(in Walkable walkable, in InputFlags flags)
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
    private static void NormalizeInput(ref Input input)
    {
        input.InputX = (sbyte)Math.Sign(input.InputX);
        input.InputY = (sbyte)Math.Sign(input.InputY);
    }
}