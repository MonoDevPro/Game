using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;

namespace Game.ECS.Systems;

public sealed partial class MovementSystem(World world) : GameSystem(world)
{
    [Query]
    [All<Position, Velocity>]
    private void MoveEntity(ref Position pos, ref Velocity vel, ref Rotation rotation, ref NetworkSync sync,
        float deltaTime)
    {
        if (deltaTime <= 0) return;
        if (vel.Value is { X: 0, Y: 0 }) return;
        
        // Atualiza a rotação com base na direção da velocidade
        rotation.Value = (ushort)(Math.Atan2(vel.Value.Y, vel.Value.X) * (180 / Math.PI));
        // Atualiza a posição com base na velocidade e no tempo decorrido
        pos.Value = new Coordinate(
            pos.Value.X + (int)(vel.Value.X * deltaTime),
            pos.Value.Y + (int)(vel.Value.Y * deltaTime));
        
        // Marca a entidade como "suja" para sincronização de rede, se aplicável
        sync.IsDirty = true;
        sync.Flags |= SyncFlags.Position | SyncFlags.Rotation;
    }
}