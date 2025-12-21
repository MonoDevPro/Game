using Game.Domain.Enums;
using Game.ECS.Shared.Components.Entities;
using Game.ECS.Shared.Components.Navigation;
using Game.ECS.Shared.Core.Entities;
using GameECS.Modules.Entities.Server.Persistence;
using GameECS.Modules.Navigation.Shared.Components;
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

    public void UpdateFromSnapshot(in PlayerData snapshot)
    {
        LoadSprite((VocationType)snapshot.Vocation, (Gender)snapshot.Gender);
        UpdateName(snapshot.Name);
        UpdateAnimationState((MovementDirection)snapshot.Direction, false, false, false);
        UpdatePosition(new Vector3I(snapshot.X, snapshot.Y, snapshot.Z));
        UpdateVitals(snapshot.Hp, snapshot.MaxHp, snapshot.Mp, snapshot.MaxMp);
        UpdateAnimationSpeed(snapshot.MovementSpeed, snapshot.AttackSpeed);
    }
}