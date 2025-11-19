using Game.Domain.Enums;
using Game.ECS.Entities.Data;
using Game.ECS.Entities.Factories;
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
        LoadSprite(VocationType.Archer, Gender.Male);
        UpdateName("NPC " + data.NetworkId);
        UpdateAnimationState(new Vector2I(0, 1), false, false);
        UpdatePosition(new Vector3I(data.PositionX, data.PositionY, data.PositionZ));
        UpdateVitals(data.Hp, data.MaxHp, 0, 0);
        if (Sprite is not null) UpdateAnimationSpeed(1f, 1f);
    }

    public void UpdateFromState(NpcStateData state)
    {
        var facing = new Vector2I(state.FacingX, state.FacingY);
        bool isMoving = state.Speed > 0.05f;
        UpdateAnimationState(facing, isMoving, false);
        UpdatePosition(new Vector3I(state.PositionX, state.PositionY, state.PositionZ));
        UpdateVitals(state.CurrentHp, state.MaxHp, 0, 0);
    }
}