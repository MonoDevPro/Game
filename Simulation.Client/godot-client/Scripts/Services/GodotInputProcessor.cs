using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
using Godot;

namespace GodotClient.Services;

public sealed partial class GodotInputSystem(World world) : GameSystem(world, null, null)
{
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