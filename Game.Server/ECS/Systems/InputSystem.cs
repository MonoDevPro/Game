using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Logic;
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
        ref Velocity velocity, in Walkable speed, ref PlayerInput input, [Data] in float _)
    {
        (velocity.DirectionX, velocity.DirectionY) = MovementLogic.NormalizeInput(input.InputX, input.InputY);
        if (velocity is not { DirectionX: 0, DirectionY: 0 })
            velocity.Speed = MovementLogic.ComputeCellsPerSecond(in speed, input.Flags);
    }
}