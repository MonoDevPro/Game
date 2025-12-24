using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Game.Domain.Commons.Enums;

namespace Game.Domain.Attributes.Equipments.ValueObjects;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct Equipments : IEquatable<Equipments>
{
    // precisa ser const compile-time
    public const int SlotCount = (int)EquipmentSlotType.Count;

    // itemId == 0 => vazio
    // fixed buffer garante que os ints ficam inline no struct (blittable)
    private fixed int _itemIds[SlotCount];

    // cria estado vazio (todos zeros)
    public static Equipments Empty()
    {
        return default; // fixed buffer inicializado com zeros
    }

    // leitura segura (inline)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Get(EquipmentSlotType slot)
    {
        return _itemIds[(int)slot];
    }

    // "Imutável" do ponto de vista do usuário: retorna um novo Equipments com a alteração
    public Equipments WithItem(EquipmentSlotType slot, int itemId)
    {
        Equipments copy = this;
        int* p = (int*)Unsafe.AsPointer(ref copy._itemIds[0]);
        p[(int)slot] = itemId;
        return copy;
    }

    // helper para remover (equivalente a WithItem(slot, 0))
    public Equipments WithoutItem(EquipmentSlotType slot) => WithItem(slot, 0);

    // iteração simples
    public Span<int> AsSpan()
    {
        var tmp = new int[SlotCount];
        for (int i = 0; i < SlotCount; i++) tmp[i] = _itemIds[i];
        return new Span<int>(tmp);
    }

    // comparação
    public bool Equals(Equipments other)
    {
        int* a = (int*)Unsafe.AsPointer(ref _itemIds[0]);
        int* b = (int*)Unsafe.AsPointer(ref other._itemIds[0]);
        for (int i = 0; i < SlotCount; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    public override bool Equals(object? obj) => obj is Equipments e && Equals(e);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            int* p = (int*)Unsafe.AsPointer(ref _itemIds[0]);
            for (int i = 0; i < SlotCount; i++)
                hash = hash * 31 + p[i];
            return hash;
        }
    }

    public override string ToString()
    {
        fixed (int* p = _itemIds)
        {
            return $"Equipments[{string.Join(",", new ReadOnlySpan<int>(p, SlotCount).ToArray())}]";
        }
    }
    
    public ReadOnlySpan<int> Serialize()
    {
        var tmp = new int[SlotCount];
        int* p = (int*)Unsafe.AsPointer(ref _itemIds[0]);
        for (int i = 0; i < SlotCount; i++)
            tmp[i] = p[i];
        return new ReadOnlySpan<int>(tmp);
    }
    
    public static Equipments Deserialize(ReadOnlySpan<int> itemIds)
    {
        if (itemIds.Length != SlotCount)
            throw new ArgumentException($"Expected {SlotCount} item IDs, got {itemIds.Length}");
        
        Equipments equipments = default;
        int* p = (int*)Unsafe.AsPointer(ref equipments._itemIds[0]);
        for (int i = 0; i < SlotCount; i++)
            p[i] = itemIds[i];
        return equipments;
    }
    
    
}