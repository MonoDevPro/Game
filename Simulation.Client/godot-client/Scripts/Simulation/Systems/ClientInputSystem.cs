using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
using Godot;

namespace GodotClient.Simulation.Systems;

public sealed partial class ClientInputSystem(World world) : GameSystem(world)
{
    [Query]
    [All<PlayerControlled, PlayerInput>]
    private void ClearInput(ref PlayerInput input)
    {
        input.InputX = 0;
        input.InputY = 0;
        input.Flags = 0;
    }
    
    [Query]
    [All<PlayerControlled, LocalPlayerTag, PlayerInput>]
    private void ApplyInput(in Entity e, ref PlayerInput input)
    {
        sbyte moveX = 0, moveY = 0;
        if (Input.IsActionPressed("walk_west")) moveX = -1;
        else if (Input.IsActionPressed("walk_east")) moveX = 1;

        if (Input.IsActionPressed("walk_north")) moveY = -1;
        else if (Input.IsActionPressed("walk_south")) moveY = 1;

        var buttons = InputFlags.None;
        if (Input.IsActionPressed("click_left"))  buttons |= InputFlags.ClickLeft;
        if (Input.IsActionPressed("click_right")) buttons |= InputFlags.ClickRight;
        if (Input.IsActionPressed("attack"))      buttons |= InputFlags.Attack;
        if (Input.IsActionPressed("sprint"))      buttons |= InputFlags.Sprint;

        if (moveX != 0 || moveY != 0 || buttons != 0)
        {
            input.InputX = moveX;
            input.InputY = moveY;
            input.Flags = buttons;
        }
    }
}