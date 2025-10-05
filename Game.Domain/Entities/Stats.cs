// Domain/Entities/Stats.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace Game.Domain.Entities;

/// <summary>
/// Estatísticas do personagem
/// Autor: MonoDevPro
/// Data: 2025-10-05 21:10:27
/// </summary>
public class Stats : BaseEntity
{
    // Progressão
    public int Level { get; set; } = 1;
    public long Experience { get; set; } = 0; // MUDADO para long (evita overflow)
    
    // Atributos Base (do personagem, sem equipamento)
    public int BaseStrength { get; set; } = 5;
    public int BaseDexterity { get; set; } = 5;
    public int BaseIntelligence { get; set; } = 5;
    public int BaseConstitution { get; set; } = 5;
    public int BaseSpirit { get; set; } = 5;
    
    // Vida e Mana Atuais
    public int CurrentHp { get; set; } = 50;
    public int CurrentMp { get; set; } = 30;
    
    // Atributos Totais (Base + Bônus de Equipamento) [NotMapped - calculados]
    [NotMapped]
    public int TotalStrength => BaseStrength + BonusStrength;
    
    [NotMapped]
    public int TotalDexterity => BaseDexterity + BonusDexterity;
    
    [NotMapped]
    public int TotalIntelligence => BaseIntelligence + BonusIntelligence;
    
    [NotMapped]
    public int TotalConstitution => BaseConstitution + BonusConstitution;
    
    [NotMapped]
    public int TotalSpirit => BaseSpirit + BonusSpirit;
    
    // Bônus de Equipamento (calculados externamente, não persistidos)
    [NotMapped]
    public int BonusStrength { get; set; }
    
    [NotMapped]
    public int BonusDexterity { get; set; }
    
    [NotMapped]
    public int BonusIntelligence { get; set; }
    
    [NotMapped]
    public int BonusConstitution { get; set; }
    
    [NotMapped]
    public int BonusSpirit { get; set; }
    
    // Stats Derivados (calculados com base nos totais)
    [NotMapped]
    public int MaxHp => 10 * TotalConstitution + Level * 5;
    
    [NotMapped]
    public int MaxMp => 5 * TotalIntelligence + Level * 3;
    
    [NotMapped]
    public int PhysicalAttack => TotalStrength * 2 + Level;
    
    [NotMapped]
    public int MagicAttack => TotalIntelligence * 3 + (TotalSpirit / 2);
    
    [NotMapped]
    public int PhysicalDefense => TotalConstitution + (TotalStrength / 2);
    
    [NotMapped]
    public int MagicDefense => TotalSpirit + (TotalIntelligence / 2);
    
    [NotMapped]
    public double AttackSpeed => 1.0 + (TotalDexterity / 100.0);
    
    [NotMapped]
    public double MovementSpeed => 1.0 + (TotalDexterity / 200.0);
    
    // Regeneração
    public int HpRegenPerTick() => Math.Max(1, TotalConstitution / 10);
    public int MpRegenPerTick() => Math.Max(1, TotalSpirit / 10);
    
    // Relacionamento
    public int CharacterId { get; init; }
    public virtual Character Character { get; init; } = null!;
}