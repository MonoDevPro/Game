namespace GameWeb.Domain.Entities;

/// <summary>
/// Entidade de domínio que representa um jogador (estado persistido).
/// Herdando de BaseAuditableEntity para obter Id, Created/Modified etc.
/// </summary>
public class Player : BaseAuditableEntity
{
    // Identity / descriptive
    public string UserId { get; set; } = null!; // Link to authentication system
    public string Name { get; init; } = null!;
    public byte Gender { get; init; }
    public byte Vocation { get; init; }

    // Core stats
    public int HealthMax { get; set; }
    public int HealthCurrent { get; set; }
    public int AttackDamage { get; set; }
    public int AttackRange { get; set; }
    public float AttackCastTime { get; set; }
    public float AttackCooldown { get; set; }
    public float MoveSpeed { get; set; }

    // World state (discrete grid)
    public int PosX { get; set; }
    public int PosY { get; set; }

    // Direction vector as integers (e.g. -1/0/1). Keep small for network efficiency.
    public int DirX { get; set; }
    public int DirY { get; set; }

    // ---- Convenience / domain methods ----

    /// <summary>
    /// Aplica dano ao jogador e retorna se morreu.
    /// </summary>
    public bool ApplyDamage(int damage)
    {
        if (damage <= 0) return HealthCurrent <= 0;
        HealthCurrent = Math.Max(0, HealthCurrent - damage);
        return HealthCurrent == 0;
    }

    /// <summary>
    /// Cura o jogador sem ultrapassar HealthMax.
    /// </summary>
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        HealthCurrent = Math.Min(HealthMax, HealthCurrent + amount);
    }

    /// <summary>
    /// Define a posição do jogador.
    /// </summary>
    public void SetPosition(int x, int y)
    {
        PosX = x;
        PosY = y;
    }

    /// <summary>
    /// Define direção do jogador (normalizada para -1/0/1 caso queira).
    /// </summary>
    public void SetDirection(int dx, int dy)
    {
        DirX = dx;
        DirY = dy;
    }

    /// <summary>
    /// Inicializa os valores padrões de vida/estado se necessário.
    /// Útil ao criar um novo jogador.
    /// </summary>
    public void InitializeDefaults(int healthMax = 100, int attackDamage = 10, float moveSpeed = 1.0f)
    {
        HealthMax = healthMax;
        HealthCurrent = healthMax;
        AttackDamage = attackDamage;
        AttackRange = 1;
        AttackCastTime = 0.25f;
        AttackCooldown = 1.0f;
        MoveSpeed = moveSpeed;
    }
}
