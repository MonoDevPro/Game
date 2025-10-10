namespace Game.Server.Security;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string hashedPassword, string providedPassword);
}

public class PasswordHasher : IPasswordHasher
{
    // Implementação simples de hash de senha (substitua por uma implementação segura em produção)
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12, false);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword, false);
    }
}