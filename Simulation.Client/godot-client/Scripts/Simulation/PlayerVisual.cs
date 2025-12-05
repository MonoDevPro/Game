using Game.Domain.Enums;
using Game.ECS.Schema.Components;
using Game.ECS.Schema.Snapshots;
using Godot;

namespace GodotClient.Simulation;

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
        LoadSprite((VocationType)snapshot.VocationId, (Gender)snapshot.GenderId);
        UpdateName(snapshot.Name);
        UpdateAnimationState(new Direction { X = snapshot.DirX, Y = snapshot.DirY }, false, false, false);
        UpdatePosition(new Vector3I(snapshot.PosX, snapshot.PosY, snapshot.Floor));
        UpdateVitals(snapshot.Hp, snapshot.MaxHp, snapshot.Mp, snapshot.MaxMp);
        UpdateAnimationSpeed(snapshot.MovementSpeed, snapshot.AttackSpeed);
    }
}