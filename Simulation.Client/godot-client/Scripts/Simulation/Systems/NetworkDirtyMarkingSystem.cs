using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
using Game.ECS.Utils;

namespace GodotClient.Simulation.Systems;

/// <summary>
/// Sistema que marca automaticamente entidades como "dirty" quando seus componentes mudam.
/// Isso garante que NetworkSendSystem saiba quais entidades precisam sincronização.
/// 
/// Autor: MonoDevPro
/// Data: 2025-01-11
/// </summary>
public sealed partial class NetworkDirtyMarkingSystem(World world) : GameSystem(world)
{
    /// <summary>
    /// Marca movimento como dirty quando posição ou velocidade mudam
    /// </summary>
    [Query]
    [All<LocalPlayer, PlayerControlled, PlayerInput>]
    private void MarkMovementDirty(in Entity entity, in PlayerInput input, ref NetworkDirty dirty)
    {
        if (input.InputX == 0 && input.InputY == 0)
            return; // Não marca se não está se movendo
        
        if (dirty.HasFlags(SyncFlags.Movement))
            return; // Já está marcado como dirty
        
        dirty.AddFlags(SyncFlags.Movement);
    }

    /// <summary>
    /// Marca facing como dirty quando direção muda
    /// </summary>
    [Query]
    [All<LocalPlayer, PlayerControlled>]
    private void MarkFacingDirty(in Entity entity, in Facing facing, ref NetworkDirty dirty)
    {
        if (dirty.HasFlags(SyncFlags.Facing))
            return; // Já está marcado como dirty
        
        if (facing.DirectionX == 0 && facing.DirectionY == 0)
            return; // Não marca se não há direção
        
        dirty.AddFlags(SyncFlags.Facing);
    }
}
