using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using GameECS.Modules.Entities.Shared.Data;

namespace GameECS.Modules.Entities.Shared.Components;

/// <summary>
/// Identidade única da entidade.
/// </summary>
public struct EntityIdentity
{
    public int UniqueId;
    public EntityType Type;
    public int TemplateId;
}

/// <summary>
/// Nome da entidade (fixed size para performance).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct EntityName
{
    private const int MaxLength = 32;
    private fixed char _name[MaxLength];
    private byte _length;

    public static EntityName Create(string? name)
    {
        var result = new EntityName();
        if (string.IsNullOrEmpty(name)) return result;

        int len = Math.Min(name.Length, MaxLength - 1);
        for (int i = 0; i < len; i++)
            result._name[i] = name[i];
        result._length = (byte)len;
        return result;
    }

    public override readonly string ToString()
    {
        fixed (char* ptr = _name)
            return new string(ptr, 0, _length);
    }
}

/// <summary>
/// Nível da entidade.
/// </summary>
public struct EntityLevel
{
    public const int MaxLevel = 500;

    public int Level;
    public long Experience;
    public long ExperienceToNext;

    public EntityLevel() : this(1) { }

    public EntityLevel(int level)
    {
        Level = Math.Clamp(level, 1, MaxLevel);
        Experience = 0;
        ExperienceToNext = CalculateExpToNext(level);
    }

    public void AddExperience(long amount)
    {
        if (Level >= MaxLevel) return;
        Experience += amount;
    }

    public readonly bool CanLevelUp => Level < MaxLevel && Experience >= ExperienceToNext;

    public bool TryLevelUp()
    {
        if (!CanLevelUp) return false;
        Experience -= ExperienceToNext;
        Level++;
        ExperienceToNext = CalculateExpToNext(Level);
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
