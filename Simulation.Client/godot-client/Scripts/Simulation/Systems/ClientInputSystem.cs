using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
using Godot;
using GodotClient.Systems;

namespace GodotClient.Simulation.Systems;

/// <summary>
/// Recebe PlayerInput (local) e guarda em PendingInputs; inicia movimentos preditivos
/// quando a entidade não estiver se movendo.
/// </summary>
public sealed partial class ClientInputSystem(World world) : GameSystem(world)
{
    [Query]
    [All<LocalPlayer>]
    private void ClearInput(in Entity e, ref PlayerInput input)
    {
        input.InputX = 0;
        input.InputY = 0;
        input.Flags = 0;
    }

    [Query]
    [All<LocalPlayer>]
    private void ApplyInput(in Entity e, ref PlayerInput input)
    {
        // Lê input diretamente como -1, 0, 1
        sbyte moveX = 0;
        sbyte moveY = 0;

        if (Input.IsActionPressed("walk_west"))
            moveX = -1;
        
        else if (Input.IsActionPressed("walk_east"))
            moveX = 1;

        if (Input.IsActionPressed("walk_north"))
            moveY = -1;
        
        else if (Input.IsActionPressed("walk_south"))
            moveY = 1;

        InputFlags buttons = InputFlags.None;

        if (Input.IsActionPressed("click_left"))
            buttons |= InputFlags.ClickLeft;
        
        if (Input.IsActionPressed("click_right"))
            buttons |= InputFlags.ClickRight;

        if (Input.IsActionPressed("attack"))
            buttons |= InputFlags.Attack;

        if (Input.IsActionPressed("sprint"))
            buttons |= InputFlags.Sprint;

        // Envia direto como sbyte (signed byte: -128 a 127)
        // IMPORTANTE: Apenas envia se houver input (evita spam de pacotes vazios)
        if (moveX != 0 || moveY != 0 || buttons != 0)
        {
            input.InputX = moveX;
            input.InputY = moveY;
            input.Flags = buttons;
        }
    }
}
