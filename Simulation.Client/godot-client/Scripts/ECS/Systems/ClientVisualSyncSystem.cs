using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Entities.Components;
using Game.ECS.Schema.Components;
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
    private const float PixelsPerCell = 32f;
    private const float LerpSpeed = 0.15f;
    private const float SnapThreshold = 2f;
    
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
        in NetworkId networkId,
        [Data] in float deltaTime)
    {
        if (!TryGetAnyVisual(networkId.Value, out var visual))
            return;
        
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
        if (!TryGetAnyVisual(networkId.Value, out var visual))
            return;
        
        Vector2 current = visual.GlobalPosition;
        Vector2 target = new(pos.X * PixelsPerCell, pos.Y * PixelsPerCell);
        bool ismoving = false;
        
        // Extrapolação
        if (current != target)
        {
            float extrapolation = 0.5f;
            Vector2 direction = new(dir.X, dir.Y);
            target += direction * (PixelsPerCell * extrapolation);
            
            ismoving = current != target;
        }
        
        // Interpolação suave
        Vector2 next = current.Lerp(target, LerpSpeed);
        visual.GlobalPosition = next;

        // Snap quando muito próximo
        if (current.DistanceSquaredTo(target) <= SnapThreshold * SnapThreshold)
            visual.GlobalPosition = target;
        
        visual.UpdateAnimationState(dir, ismoving, World.Has<AttackCommand>(e), World.Has<Dead>(e));
    }
}