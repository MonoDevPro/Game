namespace Simulation.Core.Persistence.Models;

public class AccountModel
{
    public int Id;
    public string Username = string.Empty;
    public string PasswordHash = string.Empty; // BCrypt hash
    public DateTimeOffset CreatedAt;
    public DateTimeOffset? LastLoginAt;
}