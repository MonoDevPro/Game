namespace Simulation.Core.Persistence.Models;

public enum Gender : int { None, Male, Female }
public enum Vocation : int { None, Mage, Archer }

public class PlayerModel
{
    // --- Dados Frios / de Identificação ---
    public int Id;
    public string Name = string.Empty;
    public string PasswordHash = string.Empty; // BCrypt hash
    public Gender Gender;
    public Vocation Vocation;
    
    // --- Stats Base (geralmente não mudam a cada tick) ---
    public int HealthMax;
    public int AttackDamage;
    public int AttackRange;
    public float AttackCastTime;
    public float AttackCooldown;
    public float MoveSpeed;
    
    // --- Dados Quentes / de Estado (mudam a cada tick) ---
    public int MapId;
    public int PosX;
    public int PosY;
    public int DirX;
    public int DirY;
    public int HealthCurrent;
}