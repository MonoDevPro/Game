namespace Game.Domain.ValueObjects.Equipment;

/// <summary>
/// Resultado de uma operação de equipamento.
/// </summary>
public readonly record struct EquipmentResult(
    bool IsSuccess,
    int? UnequippedItemId,
    string? Message = null)
{
    public static EquipmentResult Success(int? unequippedItemId = null) => new(true, unequippedItemId);
    public static EquipmentResult Fail(string message) => new(false, null, message);
}