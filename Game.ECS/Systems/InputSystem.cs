using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Entities.Factories;

namespace Game.ECS.Systems;

/// <summary>
/// Sistema responsável por processar input do jogador local.
/// Converte input em ações (movimento, ataque, habilidades, etc).
/// </summary>
public sealed partial class InputSystem(World world, GameEventSystem events, EntityFactory factory) : GameSystem(world, events, factory)
{
    [Query]
    [All<PlayerControlled, Velocity>]
    private void ProcessPlayerInput(in Entity e,
        ref Velocity velocity, in Walkable speed, ref PlayerInput input, [Data] float _)
    {
        (input.InputX, input.InputY) = NormalizeInput(input.InputX, input.InputY);
        velocity.DirectionX = input.InputX;
        velocity.DirectionY = input.InputY;
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

    /// <summary>
    /// Aplica input a um jogador local (normalmente chamado a partir da camada de input).
    /// </summary>
    public bool ApplyPlayerInput(Entity entity, sbyte inputX, sbyte inputY, InputFlags flags)
    {
        if (!World.IsAlive(entity) || !World.Has<LocalPlayerTag>(entity))
            return false;

        var input = new PlayerInput
        {
            InputX = inputX,
            InputY = inputY,
            Flags = flags
        };

        World.Set(entity, input);
        Events.RaisePlayerInput(entity, inputX, inputY, flags);
        return true;
    }

    /// <summary>
    /// Limpa o input de um jogador.
    /// </summary>
    public bool ClearPlayerInput(Entity entity)
    {
        return ApplyPlayerInput(entity, 0, 0, InputFlags.None);
    }

    /// <summary>
    /// Obtém o input atual de um jogador.
    /// </summary>
    public bool TryGetPlayerInput(Entity entity, out PlayerInput input)
    {
        input = default;
        
        if (!World.IsAlive(entity) || !World.Has<PlayerInput>(entity))
            return false;

        input = World.Get<PlayerInput>(entity);
        return true;
    }
}