namespace Game.DTOs.Persistence;

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