using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Systems;
using Godot;

namespace GodotClient.Simulation.Systems;

public sealed partial class VisualLifecycleSystem(World world) : GameSystem(world)
{
    // Cria Node2D/Sprite quando aparecer VisualPrefab e não tiver NodeRef ainda
    [Query]
    [All<VisualPrefab>]
    [None<NodeRef>]
    private void SpawnVisual(in Entity e, in VisualPrefab prefab)
    {
        if (string.IsNullOrEmpty(prefab.ScenePath)) return;

        var scene = ResourceLoader.Load<PackedScene>(prefab.ScenePath);
        if (scene is null) return;

        var node = scene.Instantiate<Node2D>();
        prefab.Parent.AddChild(node);

        // Tenta pegar AnimatedSprite2D no nó instanciado
        var sprite = node.GetNodeOrNull<AnimatedSprite2D>(".");
        var nodeRef = new NodeRef { Node2D = node, IsVisible = true };
        World.Add(e, nodeRef);
        if (sprite != null)
        {
            World.Add(e, new SpriteRef { Sprite2D = sprite });
        }
    }

    // Limpa Node quando entidade é destruída (se necessário em seu ciclo)
    [Query]
    [All<NodeRef>]
    private void CleanupVisual(in Entity e, ref NodeRef nodeRef)
    {
        // Este método pode ser chamado num sistema de destruição; aqui apenas exemplo:
        // if (!World.IsAlive(e) && nodeRef.Node2D != null) nodeRef.Node2D.QueueFree();
    }
}