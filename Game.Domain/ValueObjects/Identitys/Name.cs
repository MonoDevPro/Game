using System.Runtime.InteropServices;

namespace Game.Domain.ValueObjects.Identitys;

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