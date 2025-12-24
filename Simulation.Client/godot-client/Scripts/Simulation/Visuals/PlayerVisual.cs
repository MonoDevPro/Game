using GameECS.Client;
using GameECS.Modules.Entities.Shared.Data;
using GameECS.Shared.Combat.Data;
using GameECS.Shared.Entities.Data;
using GameECS.Shared.Navigation.Components;
using Godot;

namespace GodotClient.Simulation.Visuals;

/// <summary>
/// Representação visual do jogador com predição local e reconciliação de servidor.
/// Autor: MonoDevPro
/// Data da Refatoração: 2025-10-14
/// </summary>
[Tool]
public sealed partial class PlayerVisual : DefaultVisual, IEntityVisual
{
    public static PlayerVisual Create()
    {
        var playerVisual = GD.Load<PackedScene>("res://Scenes/Prefabs/PlayerVisual.tscn").Instantiate<PlayerVisual>();
        return playerVisual;
    }

    public void Initialize(int networkId, int x, int y)
    {
        UpdatePosition(new Vector3I(x, y, 0));
    }

    public void Destroy()
    {
        QueueFree();
    }

    public void UpdateFromSnapshot(in PlayerDto snapshot)
    {
        LoadSprite((VocationType)snapshot.Vocation, (Game.Domain.Enums.GenderType)snapshot.Gender);
        UpdateName(snapshot.Name);
        UpdateAnimationState((MovementDirection)snapshot.Direction, false, false, false);
        UpdatePosition(new Vector3I(snapshot.X, snapshot.Y, snapshot.Z));
        UpdateVitals(snapshot.Hp, snapshot.MaxHp, snapshot.Mp, snapshot.MaxMp);
        UpdateAnimationSpeed(snapshot.MovementSpeed, snapshot.AttackSpeed);
    }
}