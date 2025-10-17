using System;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Services;
using Game.ECS.Systems;
using Godot;

namespace GodotClient.Simulation.Systems;


public sealed partial class ClientMovementSystem(World world) : GameSystem(world)
{
    private const float StepSize = 1f; // tamanho do passo em unidades do mundo (1 célula)
    
    // --- Cache de Colisão s---
    private static byte[]? _collisionCache;
    private static int _mapWidth;
    private static int _mapHeight;
    public static void LoadCollisionCache(byte[] collisionData, int width, int height)
    {
        _collisionCache = collisionData;
        _mapWidth = width;
        _mapHeight = height;
    }

    private static bool CanMoveTo(Vector3I position)
    {
        if (_collisionCache == null || _collisionCache.Length == 0)
            return true;

        if (position.X < 0 || position.Y < 0 || position.X >= _mapWidth || position.Y >= _mapHeight)
            return false;

        var index = position.Y * _mapWidth + position.X;
        return index < _collisionCache.Length && _collisionCache[index] == 0;
    }

    [Query]
    [All<PlayerControlled, Velocity>]
    private void StartMovement(in Entity e, ref Facing facing,
        ref Velocity velocity, in Walkable speed, in PlayerInput input)
    {
        // Normaliza input diagonal (evita movimento mais rápido na diagonal)
        int normalizedX = Math.Clamp(input.InputX, -1, 1);
        int normalizedY = Math.Clamp(input.InputY, -1, 1);

        float cellsPerSecond = speed.BaseSpeed + speed.CurrentModifier;
        cellsPerSecond *= input.Flags.HasFlag(InputFlags.Sprint) ? 1.5f : 1f;

        velocity.DirectionX = normalizedX;
        velocity.DirectionY = normalizedY;
        velocity.Speed = cellsPerSecond;
        
        // Atualiza a direção
        if (velocity is { DirectionX: 0, DirectionY: 0 }) 
            return;
        facing.DirectionX = velocity.DirectionX;
        facing.DirectionY = velocity.DirectionY;
    }

    [Query]
    [All<Position, Movement, Velocity, Walkable>]
    private void ProcessMovement(in Entity e, ref Position pos, 
        ref Movement movement, in Velocity velocity, [Data] float deltaTime)
    {
        if (velocity is { DirectionX: 0, DirectionY: 0 } || velocity.Speed <= 0)
            return;
        
        movement.Timer += velocity.Speed * deltaTime;
        
        if (movement.Timer < StepSize)
            return; // não se move se o timer não alcançou 1 segundo (ou o intervalo desejado)
        
        movement.Timer -= StepSize; // decrementa o timer, mantendo o excesso
        pos.X += velocity.DirectionX;
        pos.Y += velocity.DirectionY;
    }
}