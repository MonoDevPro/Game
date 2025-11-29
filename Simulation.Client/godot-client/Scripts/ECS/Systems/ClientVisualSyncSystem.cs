using System.Collections.Generic;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
using GodotClient.Simulation;
using Godot;

namespace GodotClient.ECS.Systems;

/// <summary>
/// Sistema que sincroniza a renderização visual com os componentes ECS.
/// </summary>
public sealed partial class ClientVisualSyncSystem(World world, Node2D entitiesRoot) 
    : GameSystem(world)
{
    private readonly Dictionary<int, PlayerVisual> _playerVisuals = new();
    private readonly Dictionary<int, NpcVisual> _npcVisuals = new();

    public void RegisterPlayerVisual(int networkId, PlayerVisual visual)
    {
        UnregisterPlayerVisual(networkId);
        
        _playerVisuals[networkId] = visual;
        
        entitiesRoot.AddChild(visual);
    }

    public void RegisterNpcVisual(int networkId, NpcVisual visual)
    {
        UnregisterNpcVisual(networkId);
        
        _npcVisuals[networkId] = visual;
        
        entitiesRoot.AddChild(visual);
    }

    public void UnregisterPlayerVisual(int networkId)
    {
        if (_playerVisuals.Remove(networkId, out var visual))
            visual.QueueFree();
    }

    public void UnregisterNpcVisual(int networkId)
    {
        if (_npcVisuals.Remove(networkId, out var visual))
            visual.QueueFree();
    }
    
    public void UnregisterAnyVisual(int networkId)
    {
        UnregisterNpcVisual(networkId);
        UnregisterPlayerVisual(networkId);
    }

    public bool TryGetPlayerVisual(int networkId, out PlayerVisual visual) =>
        _playerVisuals.TryGetValue(networkId, out visual!);

    public bool TryGetNpcVisual(int networkId, out NpcVisual visual) =>
        _npcVisuals.TryGetValue(networkId, out visual!);

    private bool TryGetAnyVisual(int networkId, out DefaultVisual visual)
    {
        if (TryGetPlayerVisual(networkId, out var playerVisual))
        {
            visual = playerVisual;
            return true;
        }

        if (TryGetNpcVisual(networkId, out var npcVisual))
        {
            visual = npcVisual;
            return true;
        }

        visual = null!;
        return false;
    }

    [Query]
    [All<NetworkId>] // ✅ Sem tag específica - aplica a TODOS
    private void SyncAnimations(
        in Entity e, 
        ref CombatState combatState,
        in NetworkId networkId,
        in Speed speed, 
        in Direction direction,
        [Data] in float deltaTime)
    {
        if (!TryGetAnyVisual(networkId.Value, out var visual))
            return;
        
        bool isAttacking = false;

        if (World.Has<AttackCommand>(e))
            if (combatState.CastTimer > 0f)
            {
                isAttacking = true;
                combatState.CastTimer -= deltaTime;
                if (combatState.CastTimer < 0f)
                    combatState.CastTimer = 0f;
            }

        var isDead = World.Has<Dead>(e);
        var isMoving = speed.Value > 0f;
        visual.UpdateAnimationState(direction, isMoving, isAttacking, isDead);
        
        if (isAttacking)
            World.Remove<AttackCommand>(e);
        
    }
    
    [Query]
    [All<NetworkId>]
    private void InterpolatePosition(
        in Position pos, 
        in Speed speed,
        in NetworkId networkId)
    {
        const float pixelsPerCell = 32f;
        const float lerpSpeed = 0.15f;
        
        if (!TryGetAnyVisual(networkId.Value, out var visual))
            return;
        
        Vector2 current = visual.GlobalPosition;
        Vector2 target = new(pos.X * pixelsPerCell, pos.Y * pixelsPerCell);

        // Extrapolação para compensar latência
        if (speed is not { X: 0, Y: 0, Value: > 0f })
        {
            float extrapolation = 0.5f;
            Vector2 direction = new(speed.X, speed.Y);
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