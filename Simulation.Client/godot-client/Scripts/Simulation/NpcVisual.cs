using Game.Domain.Enums;
using Game.ECS.Entities;
using Game.ECS.Entities.Factories;
using Game.ECS.Entities.Npc;
using Game.ECS.Schema.Components;
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
        UpdatePosition(new Vector3I(snapshot.X, snapshot.Y, snapshot.Floor));
        UpdateVitals(snapshot.Hp, snapshot.MaxHp, snapshot.Mp, snapshot.MaxMp);
        if (Sprite is not null) UpdateAnimationSpeed(snapshot.MovementSpeed, snapshot.AttackSpeed);
    }
}