using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.DTOs.Game;
using Game.DTOs.Game.Player;
using Game.ECS.Components;
using Game.ECS.Systems;
using Game.Network.Abstractions;
using Godot;
using Input = Game.ECS.Components.Input;

namespace GodotClient.ECS.Systems;

/// <summary>
/// Sistema responsável por coletar componentes marcados como dirty e emitir atualizações de estado.
/// </summary>
public sealed partial class NetworkSyncSystem(World world, INetworkManager sender) : GameSystem(world)
{
    [Query]
    [All<PlayerControlled, LocalPlayerTag, NetworkId, DirtyFlags>]
    private void SyncToServer(
        in Entity entity,
        ref DirtyFlags dirty)
    {
        if (dirty.IsEmpty) return;
        
        var dirtyFlags = dirty.ConsumeSnapshot();
        
        // Capture snapshot of dirty flags
        if (dirtyFlags.HasFlag(DirtyComponentType.Input) && World.TryGet(entity, out Input input))
        {
            var inputPacket = new InputPacket{ Input = 
                new InputData
                {
                    InputX = input.InputX, 
                    InputY = input.InputY, 
                    Flags = input.Flags
                } };
            
            sender.SendToServer(inputPacket, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            GD.Print($"[ClientSyncSystem] Sent PlayerInputPacket: " +
                     $"InputX={inputPacket.Input.InputX}, " +
                     $"InputY={inputPacket.Input.InputY}, " +
                     $"Flags={inputPacket.Input.Flags}");
        }
    }
}