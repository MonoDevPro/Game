using System;
using Game.Domain.Enums;
using Game.ECS.Components;
using Game.ECS.Entities.Factories;
using Godot;
using GodotClient.Core.Autoloads;

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

    public void UpdateFromSnapshot(PlayerData data)
    {
        LoadSprite((VocationType)data.Vocation, (Gender)data.Gender);
        UpdateName(data.Name);
        UpdateAnimationState(new Vector2I(data.FacingX, data.FacingY), false, false);
        UpdatePosition(new Vector3I(data.PosX, data.PosY, data.PosZ));
        UpdateVitals(data.Hp, data.MaxHp, data.Mp, data.MaxMp);
        UpdateAnimationSpeed(data.MovementSpeed, data.AttackSpeed);
    }
}