using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
using Godot;
using GodotClient.Scenes.Game;

namespace GodotClient.Simulation.Systems;

public struct Renderable { public Node2D Node2D; public bool IsVisible; }
public struct AnimatedSprite { public AnimatedSprite2D Sprite2D; }
public struct HealthBar { public ProgressBar ProgressBar; }

/// <summary>
/// Sistema de renderização que sincroniza estado ECS com Godot nodes.
/// Renderiza jogadores locais com predição e jogadores remotos com interpolação.
/// 
/// Autor: MonoDevPro
/// Data: 2025-01-11
/// </summary>
public sealed partial class ClientRenderSystem(World world) : GameSystem(world)
{
    private const float CellSize = 32f; // Pixels por célula do mundo
    private const float InterpolationSpeed = 0.15f; // Lerp alpha para suavidade

    /// <summary>
    /// Renderiza jogadores locais (com predição visível)
    /// </summary>
    [Query]
    [All<LocalPlayer, Position, Renderable, AnimatedSprite>]
    private void RenderLocalPlayer(in Entity e, in Position pos, in Facing facing,
        ref Renderable renderable, ref AnimatedSprite animSprite, in Movement movement)
    {
        if (!renderable.IsVisible)
            return;

        var node = renderable.Node2D;
        if (!node.IsQueuedForDeletion())
            return;

        // Atualiza posição (sem interpolação - movimento é local e responsivo)
        var worldPos = new Vector2(pos.X * CellSize, pos.Y * CellSize);
        node.GlobalPosition = worldPos;

        // Atualiza animação baseado em velocidade
    }

    /// <summary>
    /// Renderiza jogadores remotos (com interpolação)
    /// </summary>
    [Query]
    [All<RemotePlayer, Position, Renderable, AnimatedSprite>]
    private void RenderRemotePlayer(in Entity e, in Position pos, in Facing facing,
        ref Renderable renderable, ref AnimatedSprite animSprite,
        ref InterpolationState interpolation, [Data] float deltaTime)
    {
        if (!renderable.IsVisible)
            return;

        var node = renderable.Node2D;
        if (node.IsQueuedForDeletion())
            return;

        // Interpola entre último frame e atual
        Vector2 targetPos = new Vector2(pos.X * CellSize, pos.Y * CellSize);
        Vector2 currentPos = node.GlobalPosition;

        // Lerp suave para animação do movimento
        Vector2 interpolatedPos = currentPos.Lerp(targetPos, InterpolationSpeed);
        node.GlobalPosition = interpolatedPos;

        // Atualiza animação
    }

    /// <summary>
    /// Sincroniza offset de renderização para efeitos de movimento suave
    /// </summary>
    [Query]
    [All<Position, Movement, Renderable>]
    private void SyncRenderOffset(in Entity e, in Position pos, in Movement movement,
        ref Renderable renderable)
    {
        if (!renderable.IsVisible)
            return;

        var node = renderable.Node2D;
        if (node.IsQueuedForDeletion())
            return;

        // Aplica offset suave baseado no progresso do movimento (0.0 a 1.0)
        float progress = movement.Timer; // 0 a 1
        
    }

    /// <summary>
    /// Limpa nodes quando entidades são destruídas
    /// </summary>
    [Query]
    [All<Renderable>]
    private void CleanupRender(in Entity e, ref Renderable renderable)
    {
        // Verifica se node está ainda vivo
        if (!renderable.Node2D.IsQueuedForDeletion())
        {
            // Marca para deleção quando entidade for removida
        }
    }
}