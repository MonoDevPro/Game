using Game.Domain.Enums;
using Godot;

namespace GodotClient.Simulation.Visuals;

/// <summary>
/// Representação visual do jogador com predição local e reconciliação de servidor.
/// Autor: MonoDevPro
/// Data da Refatoração: 2025-10-14
/// </summary>
public sealed partial class NpcVisual : DefaultVisual
{
    public static NpcVisual Create()
        => GD.Load<PackedScene>("res://Scenes/Prefabs/NpcVisual.tscn").Instantiate<NpcVisual>();
    
    public void UpdateFromSnapshot(NpcData snapshot)
    {
        LoadSprite(VocationType.Warrior, Gender.Male); // TODO: Default sprite for NPCs, need load prefabs based on NPC type
        UpdateName(snapshot.Name);
        UpdateAnimationState(snapshot.Direction, false, false, false);
        UpdatePosition(new Vector3I(snapshot.X, snapshot.Y, snapshot.Z));
        UpdateVitals(snapshot.Hp, snapshot.MaxHp, snapshot.Mp, snapshot.MaxMp);
        if (Sprite is not null) UpdateAnimationSpeed(snapshot.MovementSpeed, snapshot.AttackSpeed);
    }
}