namespace Simulation.Domain.Templates;

public enum Gender : int { None, Male, Female }
public enum Vocation : int { None, Mage, Archer }

public class PlayerData
{
    // --- Dados Frios / de Identificação ---
    public int CharId;
    public string Name = string.Empty;
    public Gender Gender;
    public Vocation Vocation;
    public int MapId;
    
    // --- Stats Base (geralmente não mudam a cada tick) ---
    public int HealthMax;
    public int AttackDamage;
    public int AttackRange;
    public float AttackCastTime;
    public float AttackCooldown;
    public float MoveSpeed;
    
    // --- Dados Quentes / de Estado (mudam a cada tick) ---
    public int PosX;
    public int PosY;
    public int DirX;
    public int DirY;
    public int HealthCurrent;
}