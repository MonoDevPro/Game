using System.Collections.Generic;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
using Godot;
using GodotClient.Simulation.Visuals;

namespace GodotClient.Simulation.Systems;

/// <summary>
/// Sistema que sincroniza a renderização visual com os componentes ECS.
/// </summary>
public sealed partial class ClientVisualSyncSystem(World world, Node2D entitiesRoot) 
    : GameSystem(world)
{
    private readonly Dictionary<int, DefaultVisual> _visuals = new();

    public void RegisterVisual(int networkId, DefaultVisual visual)
    {
        if (_visuals.TryGetValue(networkId, out _))
        {
            UnregisterVisual(networkId);
        }
        
        _visuals[networkId] = visual;
        entitiesRoot.AddChild(visual);
    }
    
    public void UnregisterVisual(int networkId)
    {
        if (!_visuals.TryGetValue(networkId, out var visual)) 
            return;
        
        visual.QueueFree();
        _visuals.Remove(networkId);
    }

    public bool TryGetAnyVisual(int networkId, out DefaultVisual visual)
    {
        return _visuals.TryGetValue(networkId, out visual!);
    }

    [Query]
    [All<NetworkId>] // ✅ Sem tag específica - aplica a TODOS
    private void SyncAnimations(
        in Entity e, 
        in NetworkId networkId,
        in Direction dir,
        [Data] in float deltaTime)
    {
        if (!TryGetAnyVisual(networkId.Value, out var visual))
            return;
        
        bool isMoving = (dir.X != 0 || dir.Y != 0);
        bool isAttacking = World.Has<AttackCommand>(e);
        var isDead = World.Has<Dead>(e);
        visual.UpdateAnimationState(dir, isMoving, isAttacking, isDead);

        if (!World.Has<AttackCommand>(e))
            return;
        
        ref var attackCommand = ref World.Get<AttackCommand>(e);
        attackCommand.ConjureDuration -= deltaTime;
        if (attackCommand.ConjureDuration <= 0f)
            World.Remove<AttackCommand>(e);
    }
    
    [Query]
    [All<NetworkId>]
    private void InterpolatePosition(
        in Entity e,
        in Position pos, 
        in Direction dir,
        in NetworkId networkId)
    {
        const float pixelsPerCell = 32f;
        const float lerpSpeed = 0.15f;
        
        if (!TryGetAnyVisual(networkId.Value, out var visual))
            return;
        
        Vector2 current = visual.GlobalPosition;
        Vector2 target = new(pos.X * pixelsPerCell, pos.Y * pixelsPerCell);

        // Extrapolação para compensar latência (usa Direction para saber a direção do movimento)
        if (dir is not { X: 0, Y: 0 })
        {
            float extrapolation = 0.5f;
            Vector2 direction = new(dir.X, dir.Y);
            target += direction * (pixelsPerCell * extrapolation);
        }

        // Interpolação suave
        Vector2 next = current.Lerp(target, lerpSpeed);
        visual.GlobalPosition = next;
        
        // Snap quando muito próximo
        const float snapThreshold = 2f;
        if (current.DistanceSquaredTo(target) <= snapThreshold * snapThreshold)
            visual.GlobalPosition = target;
    }
}