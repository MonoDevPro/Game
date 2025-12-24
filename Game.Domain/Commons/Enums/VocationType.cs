namespace Game.Domain.Commons.Enums;

/// <summary>
/// Vocações base do jogo.
/// </summary>
public enum VocationType : byte
{
    None = 0,
    
    // Vocações Base (Tier 1) - Disponíveis na criação
    Warrior = 1,
    Archer = 2,
    Mage = 3,
    Cleric = 4,
    
    // Promoções Guerreiro (Tier 2)
    Knight = 10,      // Warrior → Tank/Defesa
    Berserker = 11,   // Warrior → DPS/Ofensivo
    
    // Promoções Arqueiro (Tier 2)
    Ranger = 20,      // Archer → Controle/Utilitário
    Assassin = 21,    // Archer → Burst/Crítico
    
    // Promoções Mago (Tier 2)
    Sorcerer = 30,    // Mage → DPS Mágico
    Warlock = 31,     // Mage → DoT/Debuff
    
    // Promoções Clérigo (Tier 2)
    Priest = 40,      // Cleric → Cura
    Paladin = 41,     // Cleric → Híbrido Tank/Suporte
}

/// <summary>
/// Tier da vocação (determina poder e requisitos).
/// </summary>
public enum VocationTier : byte
{
    None = 0,
    Base = 1,       // Vocações iniciais
    Promoted = 2,   // Primeira promoção (nível 50+)
    Elite = 3,      // Segunda promoção futura (nível 100+)
}

/// <summary>
/// Arquétipo de combate da vocação.
/// </summary>
public enum VocationArchetype : byte
{
    None = 0,
    Tank,       // Alta defesa, controle de aggro
    MeleeDps,   // Dano físico corpo-a-corpo
    RangedDps,  // Dano físico à distância
    MagicDps,   // Dano mágico
    Healer,     // Cura e suporte
    Hybrid,     // Combinação de funções
}