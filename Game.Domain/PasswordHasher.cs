using System.Security.Cryptography;
using System.Text;

namespace Game.Domain;

public static class PasswordHasher
{
    public static string Hash(string value)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    public static bool Verify(string value, string hash)
    {
        var computed = Hash(value);
        return StringComparer.OrdinalIgnoreCase.Equals(computed, hash);
    }
}
