namespace Game.Domain.Attributes.Progress.ValueObjects;

/// <summary>
/// Representa a progressão de nível do personagem.
/// </summary>
public readonly record struct Progress(int Level, long Experience)
{
    public static Progress Initial => new(1, 0);
    
    public const int MaxLevel = 500;
    
    /// <summary>
    /// XP necessário para alcançar um determinado nível.
    /// Fórmula: 50 * level^2 + 100 * level
    /// </summary>
    public static long ExperienceForLevel(int level)
    {
        if (level <= 1) return 0;
        return 50L * level * level + 100L * level;
    }
    
    /// <summary>
    /// XP necessário para o próximo nível.
    /// </summary>
    public long ExperienceToNextLevel => Level >= MaxLevel ? 0 : ExperienceForLevel(Level + 1) - Experience;
    
    /// <summary>
    /// Percentual de progresso para o próximo nível (0.0 a 1.0).
    /// </summary>
    public double LevelProgress
    {
        get
        {
            if (Level >= MaxLevel) return 1.0;
            var currentLevelXp = ExperienceForLevel(Level);
            var nextLevelXp = ExperienceForLevel(Level + 1);
            var range = nextLevelXp - currentLevelXp;
            if (range <= 0) return 1.0;
            return (double)(Experience - currentLevelXp) / range;
        }
    }
    
    /// <summary>
    /// Verifica se pode subir de nível.
    /// </summary>
    public bool CanLevelUp => Level < MaxLevel && Experience >= ExperienceForLevel(Level + 1);
    
    /// <summary>
    /// Adiciona experiência e retorna novo Progress (possivelmente com level up).
    /// </summary>
    public Progress AddExperience(long amount)
    {
        if (amount <= 0 || Level >= MaxLevel) return this;
        
        var newExp = Experience + amount;
        var newLevel = Level;
        
        // Processa múltiplos level ups
        while (newLevel < MaxLevel && newExp >= ExperienceForLevel(newLevel + 1))
        {
            newLevel++;
        }
        
        return new Progress(newLevel, newExp);
    }
    
    /// <summary>
    /// Calcula quantos níveis seriam ganhos com a quantidade de XP.
    /// </summary>
    public int LevelsGainedWith(long experienceAmount)
    {
        var simulated = AddExperience(experienceAmount);
        return simulated.Level - Level;
    }
}