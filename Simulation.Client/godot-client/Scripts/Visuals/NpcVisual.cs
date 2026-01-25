using Godot;

namespace GodotClient.Visuals;

/// <summary>
/// Representação visual do jogador com predição local e reconciliação de servidor.
/// Autor: MonoDevPro
/// Data da Refatoração: 2025-10-14
/// </summary>
public sealed partial class NpcVisual : GodotClient.Visuals.DefaultVisual
{
    public static NpcVisual Create()
        => GD.Load<PackedScene>("res://Scenes/Prefabs/NpcVisual.tscn").Instantiate<NpcVisual>();
}