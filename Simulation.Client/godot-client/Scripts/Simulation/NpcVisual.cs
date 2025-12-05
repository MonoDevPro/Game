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
public sealed partial class NpcVisual : DefaultVisual
{
    public static NpcVisual Create()
    {
        return GD.Load<PackedScene>("res://Scenes/Prefabs/NpcVisual.tscn").Instantiate<NpcVisual>();
    }
    
    public void UpdateFromSnapshot(NpcSnapshot snapshot)
    {
        LoadSprite((VocationType)snapshot.Vocation, (Gender)snapshot.Gender);
        UpdateName("NPC " + snapshot.NetworkId);
        UpdateAnimationState(new Direction { X = snapshot.DirX, Y = snapshot.DirY }, false, false, false);
        UpdatePosition(new Vector3I(snapshot.PosX, snapshot.PosY, snapshot.Floor));
        UpdateVitals(snapshot.Hp, snapshot.MaxHp, snapshot.Mp, snapshot.MaxMp);
        if (Sprite is not null) UpdateAnimationSpeed(snapshot.MovementSpeed, snapshot.AttackSpeed);
    }
}