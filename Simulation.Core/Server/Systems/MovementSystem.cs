using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.Shared.Components;

namespace Simulation.Core.Server.Systems;

/// <summary>
/// Sistema simples que processa as intenções de movimento dos jogadores.
/// </summary>
public partial class MovementSystem(World world) : BaseSystem<World, float>(world)
{
    [Query]
    [All<MoveIntent, Position>]
    private void Move(in Entity entity, ref Position pos, ref MoveIntent intent)
    {
        // Atualiza a posição com base na direção da intenção
        pos.X += intent.Direction.X;
        pos.Y += intent.Direction.Y;
        // Remove a intenção após processá-la
        World.Remove<MoveIntent>(entity);
        
        // Log para vermos a magia a acontecer no servidor
        Console.WriteLine($"[Server] Processed MoveIntent. New Position: X={pos.X}, Y={pos.Y}");
    }
}