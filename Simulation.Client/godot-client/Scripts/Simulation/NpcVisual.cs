using Game.Domain.Enums;
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
        UpdateAnimationState(new Vector2I(data.FacingX, data.FacingY), false, false);
        UpdatePosition(new Vector3I(data.PositionX, data.PositionY, data.PositionZ));
        UpdateVitals(data.Hp, data.MaxHp, 0, 0);
        if (Sprite is not null) UpdateAnimationSpeed(1f, 1f);
    }
}