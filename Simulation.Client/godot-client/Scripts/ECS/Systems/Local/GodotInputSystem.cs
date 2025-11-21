using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
using Godot;
using GodotClient.Simulation;
using Input = Game.ECS.Components.Input;

namespace GodotClient.ECS.Systems;

public sealed partial class GodotInputSystem(World world)
    : GameSystem(world)
{
    [Query]
    [All<PlayerControlled, LocalPlayerTag, Input>]
    [None<Dead>]
    private void ApplyInput(in Entity e, ref Input input, ref DirtyFlags dirty)
    {
        if (GameClient.Instance is { IsChatFocused: true })
        {
            if (input.InputX != 0 || input.InputY != 0 || input.Flags != InputFlags.None)
            {
                input.InputX = 0;
                input.InputY = 0;
                input.Flags = InputFlags.None;
                dirty.MarkDirty(DirtyComponentType.Input);
            }
            return;
        }

        sbyte moveX = 0, moveY = 0;
        if (Godot.Input.IsActionPressed("walk_west"))       moveX = -1;
        else if (Godot.Input.IsActionPressed("walk_east"))  moveX = 1;

        if (Godot.Input.IsActionPressed("walk_north"))      moveY = -1;
        else if (Godot.Input.IsActionPressed("walk_south")) moveY = 1;

        var buttons = InputFlags.None;
        if (Godot.Input.IsActionPressed("click_left"))      buttons |= InputFlags.ClickLeft;
        if (Godot.Input.IsActionPressed("click_right"))     buttons |= InputFlags.ClickRight;
        if (Godot.Input.IsActionPressed("attack"))          buttons |= InputFlags.Attack;
        if (Godot.Input.IsActionPressed("sprint"))          buttons |= InputFlags.Sprint;
        
        if (input.InputX == moveX && input.InputY == moveY && input.Flags == buttons)
            return; // No change

        // Always apply input to allow stopping movement when keys are released
        input.InputX = moveX;
        input.InputY = moveY;
        input.Flags = buttons;
        dirty.MarkDirty(DirtyComponentType.Input);
    }
    
    
}