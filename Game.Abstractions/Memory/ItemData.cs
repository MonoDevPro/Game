using Game.Domain.Enums;

namespace Game.Abstractions.Memory;

/// <summary>
/// DTO com dados de item otimizado para ECS
/// Autor: MonoDevPro
/// Data: 2025-10-04 22:13:13
/// </summary>
public class ItemData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public ItemType Type { get; set; }
    public int StackSize { get; set; }
    public int Weight { get; set; }
}