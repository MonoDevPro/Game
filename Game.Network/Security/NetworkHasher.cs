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