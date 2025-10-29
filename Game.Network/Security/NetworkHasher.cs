using System.Text;

namespace Game.Network.Security;

public static class NetworkHasher<T>
{
    public static readonly ulong Id;
    static NetworkHasher()
    {
        ulong num1 = 14695981039346656037;
        foreach (ulong num2 in typeof (T).ToString())
            num1 = (num1 ^ num2) * 1099511628211UL /*0x0100000001B3*/;
        Id = num1;
    }
}


public static class KeyHash
{
    public static ulong Fnv1A64(string s)
    {
        if (s == null) throw new ArgumentNullException(nameof(s));

        // Normalizar e padronizar (importante para usernames etc.)
        string normalized = s.Trim().ToLowerInvariant();
        byte[] bytes = Encoding.UTF8.GetBytes(normalized);

        const ulong fnvOffset = 14695981039346656037UL;
        const ulong fnvPrime  = 1099511628211UL;
        ulong hash = fnvOffset;

        foreach (var b in bytes)
        {
            hash ^= b;
            hash *= fnvPrime;
        }
        return hash;
    }
}