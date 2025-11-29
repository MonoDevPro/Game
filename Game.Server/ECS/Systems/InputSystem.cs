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
    [All<Input, Walkable, Direction, Speed, DirtyFlags>]
    [None<Dead>]
    private void ProcessInput(ref Speed velocity, in Walkable walk, in Input input, ref DirtyFlags dirty)
    {
        sbyte newDirX = input.InputX;
        sbyte newDirY = input.InputY;

        // ✅ Se não há input de movimento ou se está atacando, zera a velocidade
        if (newDirX == 0 && newDirY == 0 || (input.Flags & InputFlags.BasicAttack) != 0)
        {
            velocity.Value = 0f;
            dirty.MarkDirty(DirtyComponentType.State);
            return;
        }
        
        // Atualiza velocity baseado em input
        velocity.X = newDirX;
        velocity.Y = newDirY;
        velocity.Value = MovementLogic.ComputeCellsPerSecond(in walk, in input.Flags);
        dirty.MarkDirty(DirtyComponentType.State);
    }
}