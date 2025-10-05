namespace Game.Domain.Entities;

/// <summary>
/// Conta de login do jogador
/// Autor: MonoDevPro
/// Data: 2025-10-05 21:10:27
/// </summary>
public class Account : BaseEntity
{
    public string Username { get; init; } = null!;
    public string Email { get; init; } = null!; // ADICIONADO
    public string PasswordHash { get; set; } = null!;
    public byte[] PasswordSalt { get; set; } = null!; // ADICIONADO para segurança
    
    public DateTime? LastLoginAt { get; set; }
    public bool IsEmailVerified { get; set; } // ADICIONADO
    public bool IsBanned { get; set; } // ADICIONADO
    public DateTime? BannedUntil { get; set; } // ADICIONADO
    
    // Relacionamentos
    public virtual ICollection<Character> Characters { get; init; } = new List<Character>();
}