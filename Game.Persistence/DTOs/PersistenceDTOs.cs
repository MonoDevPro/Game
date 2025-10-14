using Game.Domain.Enums;

namespace Game.Persistence.DTOs;

/// <summary>
/// DTO para persistir dados de desconexão do personagem.
/// Autor: MonoDevPro
/// Data: 2025-10-13 20:30:00
/// </summary>
public sealed record DisconnectPersistenceDto
{
    public required int CharacterId { get; init; }
    public required int PositionX { get; init; }
    public required int PositionY { get; init; }
    public required DirectionEnum Direction { get; init; }
    public int CurrentHp { get; init; }
    public int CurrentMp { get; init; }
    
    public override string ToString()
    {
        return $"DisconnectPersistenceDto(CharacterId={CharacterId}, Position=({PositionX}, {PositionY}), Direction={Direction}, CurrentHp={CurrentHp}, CurrentMp={CurrentMp})";
    }
}

/// <summary>
/// DTO para persistir posição do personagem.
/// </summary>
public sealed record PositionPersistenceDto
{
    public required int CharacterId { get; init; }
    public required int PositionX { get; init; }
    public required int PositionY { get; init; }
    public required DirectionEnum Direction { get; init; }
}

/// <summary>
/// DTO para persistir vitals (HP/MP) do personagem.
/// </summary>
public sealed record VitalsPersistenceDto
{
    public required int CharacterId { get; init; }
    public required int CurrentHp { get; init; }
    public required int CurrentMp { get; init; }
}

/// <summary>
/// DTO para persistir stats completos do personagem.
/// </summary>
public sealed record StatsPersistenceDto
{
    public required int CharacterId { get; init; }
    public required int Level { get; init; }
    public required long Experience { get; init; }
    public required int BaseStrength { get; init; }
    public required int BaseDexterity { get; init; }
    public required int BaseIntelligence { get; init; }
    public required int BaseConstitution { get; init; }
    public required int BaseSpirit { get; init; }
    public required int CurrentHp { get; init; }
    public required int CurrentMp { get; init; }
}

/// <summary>
/// DTO para persistir slot de inventário.
/// </summary>
public sealed record InventorySlotPersistenceDto
{
    public required int SlotIndex { get; init; }
    public required int? ItemId { get; init; }
    public required int Quantity { get; init; }
    public required bool IsActive { get; init; }
}

/// <summary>
/// DTO para persistir inventário completo do personagem.
/// </summary>
public sealed record InventoryPersistenceDto
{
    public required int CharacterId { get; init; }
    public required IReadOnlyList<InventorySlotPersistenceDto> Slots { get; init; }
}