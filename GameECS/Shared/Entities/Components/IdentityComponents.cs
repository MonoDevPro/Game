using System.Runtime.InteropServices;
using GameECS.Shared.Entities.Data;

namespace GameECS.Shared.Entities.Components;

/// <summary>
/// Identificador de rede para sincronização cliente-servidor.
/// </summary>
public struct NetworkId(int value)
{
    public int Value = value;
    public static implicit operator int(NetworkId id) => id.Value;
    public static implicit operator NetworkId(int value) => new(value);
}

/// <summary>
/// Identidade única da entidade.
/// </summary>
public struct Identity
{
    public int UniqueId;
    public EntityType Type;
}

/// <summary>
/// Nome da entidade (fixed size para performance).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct Name
{
    private const int MaxLength = 32;
    private fixed char _name[MaxLength];
    private byte _length;

    public static Name Create(string? name)
    {
        var result = new Name();
        if (string.IsNullOrEmpty(name)) return result;

        int len = Math.Min(name.Length, MaxLength - 1);
        for (int i = 0; i < len; i++)
            result._name[i] = name[i];
        result._length = (byte)len;
        return result;
    }

    public readonly override string ToString()
    {
        fixed (char* ptr = _name)
            return new string(ptr, 0, _length);
    }
}

/// <summary>
/// Nível da entidade.
/// </summary>
public struct Level(int lvl)
{
    public const int MaxLevel = 500;

    public int Lvl = Math.Clamp(lvl, 1, MaxLevel);
    public long Experience = 0;
    public long ExperienceToNext = CalculateExpToNext(lvl);

    public Level() : this(1) { }

    public void AddExperience(long amount)
    {
        if (Lvl >= MaxLevel) return;
        Experience += amount;
    }

    public readonly bool CanLevelUp => Lvl < MaxLevel && Experience >= ExperienceToNext;

    public bool TryLevelUp()
    {
        if (!CanLevelUp) return false;
        Experience -= ExperienceToNext;
        Lvl++;
        ExperienceToNext = CalculateExpToNext(Lvl);
        return true;
    }

    private static long CalculateExpToNext(int level)
        => (long)(100 * Math.Pow(1.15, level - 1));
}

/// <summary>
/// Ownership de player (para persistência).
/// </summary>
public struct PlayerOwnership
{
    public int AccountId;
    public int CharacterId;
}

/// <summary>
/// Ownership de pet.
/// </summary>
public struct PetOwnership
{
    public int OwnerEntityId;
    public bool IsActive;
}

/// <summary>
/// Membro de party.
/// </summary>
public struct PartyMember
{
    public int PartyId;
    public bool IsLeader;

    public readonly bool IsInParty => PartyId > 0;
}
