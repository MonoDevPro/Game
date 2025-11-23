using Game.Domain.Enums;
using Game.ECS.Components;
using Game.ECS.Entities.Data;
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
    
    public void UpdateFromSnapshot(NPCData data)
    {
        LoadSprite((VocationType)data.Vocation, (Gender)data.Gender);
        UpdateName("NPC " + data.NetworkId);
        UpdateAnimationState(new Facing { DirectionX = data.FacingX, DirectionY = data.FacingY }, false, false, false);
        UpdatePosition(new Vector3I(data.PositionX, data.PositionY, data.Floor));
        UpdateVitals(data.Hp, data.MaxHp, data.Mp, data.MaxMp);
        if (Sprite is not null) UpdateAnimationSpeed(data.MovementSpeed, data.AttackSpeed);
    }
}