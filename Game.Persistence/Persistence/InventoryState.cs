namespace Game.DTOs.Persistence;

public readonly record struct InventorySlotState(
    int SlotIndex,
    int ItemId,
    int Quantity,
    bool IsActive
);

/// <summary>
/// DTO para persistir inventário completo do personagem.
/// </summary>
public sealed record InventoryState
{
    public required int CharacterId { get; init; }
    public required IReadOnlyList<InventorySlotState> Slots { get; init; }
}