using Game.Domain.Enums;
using Game.DTOs.Player;
using Game.ECS.Components;
using Godot;

namespace GodotClient.Simulation.Visuals;

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

    public void UpdateFromSnapshot(in PlayerSnapshot snapshot)
    {
        LoadSprite((VocationType)snapshot.Vocation, (Gender)snapshot.Gender);
        UpdateName(snapshot.Name);
        UpdateAnimationState(new Direction { X = snapshot.DirX, Y = snapshot.DirY }, false, false, false);
        UpdatePosition(new Vector3I(snapshot.X, snapshot.Y, snapshot.Z));
        UpdateVitals(snapshot.Hp, snapshot.MaxHp, snapshot.Mp, snapshot.MaxMp);
        UpdateAnimationSpeed(1f, 1f);
    }
}