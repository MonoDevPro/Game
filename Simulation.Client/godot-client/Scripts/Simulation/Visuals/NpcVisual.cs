using Godot;

namespace Game.Visuals;

/// <summary>
/// Representação visual do jogador com predição local e reconciliação de servidor.
/// Autor: MonoDevPro
/// Data da Refatoração: 2025-10-14
/// </summary>
public sealed partial class NpcVisual : DefaultVisual
{
    public static NpcVisual Create()
        => GD.Load<PackedScene>("res://Scenes/Prefabs/NpcVisual.tscn").Instantiate<NpcVisual>();
}