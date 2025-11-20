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
    [All<Input, Walkable>]
    [None<Dead>]
    private void ProcessInput(ref Velocity velocity, in Walkable speed, ref Input input, ref DirtyFlags dirty)
    {
        sbyte newDirX = input.InputX;
        sbyte newDirY = input.InputY;

        // ✅ Se não há input de movimento ou se está atacando, zera a velocidade
        if (newDirX == 0 && newDirY == 0 || (input.Flags & InputFlags.Attack) != 0)
        {
            if (velocity.DirectionX != 0 || velocity.DirectionY != 0 || velocity.Speed != 0f)
            {
                velocity.DirectionX = 0;
                velocity.DirectionY = 0;
                velocity.Speed = 0f;
                dirty.MarkDirty(DirtyComponentType.State);
            }
            return;
        }
        
        // Atualiza velocity baseado em input
        velocity.DirectionX = newDirX;
        velocity.DirectionY = newDirY;
        velocity.Speed = MovementLogic.ComputeCellsPerSecond(in speed, in input.Flags);
        dirty.MarkDirty(DirtyComponentType.State);
    }
}