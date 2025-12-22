using GameECS.Client;
using GameECS.Modules.Entities.Shared.Components;
using GameECS.Modules.Entities.Shared.Data;
using GameECS.Modules.Navigation.Shared.Components;
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

    public void UpdateFromSnapshot(in PlayerData snapshot)
    {
        LoadSprite((GameECS.Modules.Combat.Shared.Data.VocationType)snapshot.Vocation, (Game.Domain.Enums.Gender)snapshot.Gender);
        UpdateName(snapshot.Name);
        UpdateAnimationState((GameECS.Modules.Navigation.Shared.Components.MovementDirection)snapshot.Direction, false, false, false);
        UpdatePosition(new Vector3I(snapshot.X, snapshot.Y, snapshot.Z));
        UpdateVitals(snapshot.Hp, snapshot.MaxHp, snapshot.Mp, snapshot.MaxMp);
        UpdateAnimationSpeed(snapshot.MovementSpeed, snapshot.AttackSpeed);
    }
}