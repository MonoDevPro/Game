// MovementSystem.cs

using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Abstractions;
using Game.ECS.Components;
using Game.ECS.Services;
using Game.ECS.Systems;
using Game.ECS.Utils;

namespace Game.Server.Simulation.Systems;

public sealed partial class MovementSystem(World world, MapService map) : GameSystem(world)
{
    private const float StepSize = 1f; // tamanho do passo em unidades do mundo (1 célula)
    
    [Query]
    [All<Position, Movement, Velocity, Walkable>]
    private void ProcessMovement(in Entity e, ref Position pos, 
        ref Movement movement, in Velocity velocity, [Data] float deltaTime)
    {
        if (velocity.DirectionX == 0 && velocity.DirectionY == 0 || velocity.Speed <= 0)
            return;
        
        movement.Timer += velocity.Speed * deltaTime;
        
        if (movement.Timer < StepSize)
            return; // não se move se o timer não alcançou 1 segundo (ou o intervalo desejado)
        
        movement.Timer -= StepSize; // decrementa o timer, mantendo o excesso
        pos.X += velocity.DirectionX;
        pos.Y += velocity.DirectionY;
        World.MarkNetworkDirty(e, SyncFlags.Position);
    }
}