using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Game.Domain.Commons.Enums;

namespace Game.Domain.Commons.ValueObjects.Equipment;

/// <summary>
/// Componente ECS para equipamentos.
/// Buffer fixo para zero heap allocation e cache-friendly access.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct Equipment : IEquatable<Equipment>
{
    public const int SlotCount = (int)EquipmentSlotType.Count;
    
    // Buffer fixo - todos os slots em memória contígua
    // Usa int para ItemId (0 = vazio, >0 = ID do item)
    private fixed int _slots[SlotCount];
    
    public static Equipment Empty => default;
    
    public static Equipment CreateFromEntity(Span<int?> itemIds)
    {
        Equipment equipment = default;
        for (int i = 0; i < SlotCount && i < itemIds.Length; i++)
        {
            equipment._slots[i] = itemIds[i] ?? 0;
        }
        return equipment;
    }

    /// <summary>
    /// Acessa o ItemId em um slot específico.
    /// </summary>
    public int this[EquipmentSlotType slot]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _slots[(int)slot];
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _slots[(int)slot] = value;
    }

    /// <summary>
    /// Verifica se o slot está ocupado.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool HasItem(EquipmentSlotType slot) 
        => _slots[(int)slot] > 0;

    /// <summary>
    /// Equipa um item no slot, retorna o item anterior (0 se vazio).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Equip(EquipmentSlotType slot, int itemId)
    {
        var index = (int)slot;
        var previous = _slots[index];
        _slots[index] = itemId;
        return previous;
    }

    /// <summary>
    /// Remove item do slot, retorna o item removido. 
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Unequip(EquipmentSlotType slot)
    {
        var index = (int)slot;
        var item = _slots[index];
        _slots[index] = 0;
        return item;
    }

    /// <summary>
    /// Limpa todos os slots.
    /// </summary>
    public void Clear()
    {
        for (var i = 0; i < SlotCount; i++)
            _slots[i] = 0;
    }

    /// <summary>
    /// Conta quantos slots estão ocupados. 
    /// </summary>
    public readonly int EquippedCount
    {
        get
        {
            var count = 0;
            for (var i = 1; i < SlotCount; i++) // Skip None
                if (_slots[i] > 0) count++;
            return count;
        }
    }
    
    // comparação
    public bool Equals(Equipment other)
    {
        int* a = (int*)Unsafe.AsPointer(ref _slots[0]);
        int* b = (int*)Unsafe.AsPointer(ref other._slots[0]);
        for (int i = 0; i < SlotCount; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    public override bool Equals(object? obj) => obj is Equipment e && Equals(e);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            int* p = (int*)Unsafe.AsPointer(ref _slots[0]);
            for (int i = 0; i < SlotCount; i++)
                hash = hash * 31 + p[i];
            return hash;
        }
    }

    public override string ToString()
    {
        fixed (int* p = _slots)
        {
            return $"Equipments[{string.Join(",", new ReadOnlySpan<int>(p, SlotCount).ToArray())}]";
        }
    }

    public static ReadOnlySpan<int> Serialize(ref Equipment equipment)
    {
        var tmp = new int[SlotCount];
        int* p = (int*)Unsafe.AsPointer(ref equipment._slots[0]);
        for (int i = 0; i < SlotCount; i++)
            tmp[i] = p[i];
        return new ReadOnlySpan<int>(tmp);
    }
    
    public static Equipment Deserialize(ReadOnlySpan<int> itemIds)
    {
        if (itemIds.Length != SlotCount)
            throw new ArgumentException($"Expected {SlotCount} item IDs, got {itemIds.Length}");
        
        Equipment equipments = default;
        int* p = (int*)Unsafe.AsPointer(ref equipments._slots[0]);
        for (int i = 0; i < SlotCount; i++)
            p[i] = itemIds[i];
        return equipments;
    }
}