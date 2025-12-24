namespace Game.Domain;

/// <summary>
/// Constantes globais do domínio do jogo.
/// Centraliza valores que são usados em múltiplos lugares.
/// </summary>
public static class GameConstants
{
    /// <summary>
    /// Configurações de personagem.
    /// </summary>
    public static class Character
    {
        public const int MaxLevel = 500;
        public const int MaxCharactersPerAccount = 5;
        public const int MinNameLength = 3;
        public const int MaxNameLength = 20;
        public const int DefaultStartPositionX = 100;
        public const int DefaultStartPositionY = 100;
        public const int DefaultStartPositionZ = 0;
    }
    
    /// <summary>
    /// Configurações de inventário.
    /// </summary>
    public static class Inventory
    {
        public const int DefaultCapacity = 30;
        public const int MaxCapacity = 100;
        public const int MaxStackSize = 999;
    }
    
    /// <summary>
    /// Configurações de combate.
    /// </summary>
    public static class Combat
    {
        public const float BaseCriticalDamageMultiplier = 1.5f;
        public const float MinDamageMultiplier = 0.1f;
        public const int MinDamage = 1;
        public const float BaseHitChance = 90f;
        public const float MinHitChance = 20f;
        public const float MaxHitChance = 99f;
        public const float BaseAttackSpeed = 1.0f;
        public const float BaseMovementSpeedTilesPerSecond = 4.0f;
    }
    
    /// <summary>
    /// Configurações de progressão.
    /// </summary>
    public static class Progression
    {
        public const int PromotionLevel = 50;
        public const int ElitePromotionLevel = 100;
        
        // Fórmula de XP: 50 * level^2 + 100 * level
        public const int XpBaseMultiplier = 50;
        public const int XpLinearMultiplier = 100;
    }
    
    /// <summary>
    /// Configurações de regeneração.
    /// </summary>
    public static class Regeneration
    {
        public const int MinRegenPerTick = 1;
        public const int HpRegenDivisor = 10;
        public const int MpRegenDivisor = 10;
        public const float RegenTickIntervalSeconds = 1.0f;
    }
    
    /// <summary>
    /// Configurações de mapa.
    /// </summary>
    public static class Map
    {
        public const int MaxWidth = 2048;
        public const int MaxHeight = 2048;
        public const int MaxLayers = 16;
        public const int DefaultChunkSize = 32;
    }
}
