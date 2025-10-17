using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
using Game.ECS.Utils;

namespace Game.Server.Simulation.Systems;

public sealed partial class PlayerInputSystem(World world) : GameSystem(world)
{
    [Query]
    [All<PlayerInput>]
    [None<Dead>]
    private void ProcessPlayerInput(
        in Entity e,
        ref PlayerInput input,
        ref Velocity velocity,
        ref Facing facing,
        in Walkable speed,
        [Data] float deltaTime)
    {
        // Se não há input, reseta para zero
        if (input is { InputX: 0, InputY: 0, Flags: InputFlags.None })
            return;

        // Normaliza input diagonal (evita movimento mais rápido na diagonal)
        int normalizedX = input.InputX switch
        {
            < 0 => -1,
            > 0 => 1,
            _ => 0
        };

        int normalizedY = input.InputY switch
        {
            < 0 => -1,
            > 0 => 1,
            _ => 0
        };
        
        // Atualiza facing se houver input
        if (normalizedX != 0 || normalizedY != 0)
        {
            facing.DirectionX = normalizedX;
            facing.DirectionY = normalizedY;
            World.MarkNetworkDirty(e, SyncFlags.Facing);
        }
        
        float cellsPerSecond = speed.BaseSpeed + speed.CurrentModifier;
        cellsPerSecond *= input.Flags.HasFlag(InputFlags.Sprint) ? 1.5f : 1f;

        velocity.DirectionX = normalizedX;
        velocity.DirectionY = normalizedY;
        velocity.Speed = cellsPerSecond;
        World.MarkNetworkDirty(e, SyncFlags.Velocity);
    }
}