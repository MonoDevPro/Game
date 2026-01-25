using Godot;

namespace Game.Visuals;

/// <summary>
/// Representação visual do jogador com predição local e reconciliação de servidor.
/// Autor: MonoDevPro
/// Data da Refatoração: 2025-10-14
/// </summary>
[Tool]
public sealed partial class PlayerVisual : DefaultVisual
{
	public static PlayerVisual Create()
	{
		var playerVisual = GD.Load<PackedScene>("res://Scenes/Prefabs/PlayerVisual.tscn").Instantiate<PlayerVisual>();
		return playerVisual;
	}
}
