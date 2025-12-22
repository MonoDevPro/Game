using GameECS.Client;
using GameECS.Modules.Combat.Shared.Data;
using GameECS.Modules.Entities.Shared.Data;
using GameECS.Modules.Navigation.Shared.Components;
using Godot;

namespace GodotClient.Simulation.Visuals;

/// <summary>
/// Representação visual do jogador com predição local e reconciliação de servidor.
/// Autor: MonoDevPro
/// Data da Refatoração: 2025-10-14
/// </summary>
public sealed partial class NpcVisual : DefaultVisual, IEntityVisual
{
    public static NpcVisual Create()
        => GD.Load<PackedScene>("res://Scenes/Prefabs/NpcVisual.tscn").Instantiate<NpcVisual>();

    public void Initialize(int networkId, int x, int y)
    {
        UpdatePosition(new Vector3I(x, y, 0));
    }

    public void Destroy()
    {
        QueueFree();
    }
    
    public void UpdateFromSnapshot(NpcData snapshot)
    {
        LoadSprite(VocationType.Knight, Game.Domain.Enums.Gender.Male); // TODO: Default sprite for NPCs, need load prefabs based on NPC type
        UpdateName(snapshot.Name);
        UpdateAnimationState((GameECS.Modules.Navigation.Shared.Components.MovementDirection)snapshot.Direction, false, false, false);
        UpdatePosition(new Vector3I(snapshot.X, snapshot.Y, snapshot.Z));
        UpdateVitals(snapshot.Hp, snapshot.MaxHp, snapshot.Mp, snapshot.MaxMp);
        if (Sprite is not null) UpdateAnimationSpeed(snapshot.MovementSpeed, snapshot.AttackSpeed);
    }
}