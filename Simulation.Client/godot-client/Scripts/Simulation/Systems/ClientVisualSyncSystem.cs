using System.Collections.Generic;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Navigation.Client.Components;
using Game.ECS.Systems;
using Godot;

namespace GodotClient.Simulation.Systems;

/// <summary>
/// Sistema que sincroniza a renderização visual com os componentes ECS.
/// </summary>
public sealed partial class ClientVisualSyncSystem(World world, Node2D entitiesRoot) 
    : GameSystem(world)
{
    private readonly Dictionary<int, Visuals.PlayerVisual> _playerVisuals = new();
    private readonly Dictionary<int, Visuals.NpcVisual> _npcVisuals = new();

    public void RegisterPlayerVisual(int networkId, Visuals.PlayerVisual visual)
    {
        UnregisterPlayerVisual(networkId);
        
        _playerVisuals[networkId] = visual;
        
        entitiesRoot.AddChild(visual);
    }

    public void RegisterNpcVisual(int networkId, Visuals.NpcVisual visual)
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

    public bool TryGetPlayerVisual(int networkId, out Visuals.PlayerVisual visual) =>
        _playerVisuals.TryGetValue(networkId, out visual!);

    public bool TryGetNpcVisual(int networkId, out Visuals.NpcVisual visual) =>
        _npcVisuals.TryGetValue(networkId, out visual!);

    private bool TryGetAnyVisual(int networkId, out Visuals.DefaultVisual visual)
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
    [All<NetworkId, VisualPosition, SpriteAnimation>] // ✅ Sem tag específica - aplica a TODOS
    private void SyncAnimationVisual(
        in NetworkId networkId,
        in VisualPosition pos,
        in SpriteAnimation animation)
    {
        if (!TryGetAnyVisual(networkId.Value, out var visual))
            return;
        
        visual.GlobalPosition = new Vector2(pos.X, pos.Y);
        
        bool isMoving = animation.Clip is AnimationClip.Walk or AnimationClip.Run;
        bool isAttacking = animation.Clip == AnimationClip.Attack;
        var isDead = animation.Clip == AnimationClip.Death;
        
        visual.UpdateAnimationState(animation.Facing, isMoving, isAttacking, isDead);
    }
}