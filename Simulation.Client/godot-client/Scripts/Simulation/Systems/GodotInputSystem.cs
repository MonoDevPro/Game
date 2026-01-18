using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.DTOs;
using Game.DTOs.Player;
using Game.ECS.Components;
using Game.ECS.Services.Snapshot.Data;
using Game.ECS.Systems;
using Game.Network.Abstractions;
using Godot;
using GodotClient.Simulation.Components;

namespace GodotClient.Simulation.Systems;

public sealed partial class GodotInputSystem(World world, INetworkManager sender)
    : GameSystem(world)
{
    private sbyte _moveX;
    private sbyte _moveY;
    private InputFlags _flags;
    
    [Query]
    [All<PlayerControlled, LocalPlayerTag>]
    [None<Dead>]
    private void ApplyInput(in Entity e)
    {
        if (GameClient.Instance is { IsChatFocused: true })
        {
            if (_moveX == 0 && _moveY == 0 && _flags == InputFlags.None ) 
                return;
            
            _moveX = 0;
            _moveY = 0;
            _flags = InputFlags.None;
            return;
        }

        sbyte moveX = 0, moveY = 0;
        if (Input.IsActionPressed("walk_west"))       moveX = -1;
        else if (Input.IsActionPressed("walk_east"))  moveX = 1;

        if (Input.IsActionPressed("walk_north"))      moveY = -1;
        else if (Input.IsActionPressed("walk_south")) moveY = 1;

        var buttons = InputFlags.None;
        if (Input.IsActionPressed("click_left"))      buttons |= InputFlags.ClickLeft;
        if (Input.IsActionPressed("click_right"))     buttons |= InputFlags.ClickRight;
        if (Input.IsActionPressed("attack"))          buttons |= InputFlags.BasicAttack;
        if (Input.IsActionPressed("sprint"))          buttons |= InputFlags.Sprint;
        
        _moveX = moveX;
        _moveY = moveY;
        _flags = buttons;
        
        if (_moveX == 0 && _moveY == 0 && _flags == InputFlags.None ) 
            return;
        
        var inputPacket = new PlayerInputPacket{ Input = 
            new PlayerInputRequest
            {
                InputX = _moveX, 
                InputY = _moveY, 
                Flags = _flags
            } };
            
        sender.SendToServer(inputPacket, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        GD.Print($"[ClientSyncSystem] Sent PlayerInputPacket: " +
                 $"InputX={inputPacket.Input.InputX}, " +
                 $"InputY={inputPacket.Input.InputY}, " +
                 $"Flags={inputPacket.Input.Flags}");
    }
}