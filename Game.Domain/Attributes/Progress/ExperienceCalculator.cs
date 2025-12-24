namespace Game.Domain.Attributes.Progress;

/// <summary>
/// Calculador de experiência para diferentes fontes.
/// </summary>
public static class ExperienceCalculator
{
    /// <summary>
    /// XP base por matar um monstro do mesmo nível.
    /// </summary>
    public static long MonsterExperience(int monsterLevel, int playerLevel)
    {
        var baseXp = 10L + monsterLevel * 5L;
        var levelDiff = monsterLevel - playerLevel;
        
        // Bônus/penalidade por diferença de nível
        var multiplier = levelDiff switch
        {
            >= 5 => 1.5,   // Monstro muito mais forte
            >= 2 => 1.2,   // Monstro mais forte
            <= -5 => 0.1,  // Monstro muito mais fraco
            <= -2 => 0.5,  // Monstro mais fraco
            _ => 1.0       // Nível similar
        };
        
        return (long)(baseXp * multiplier);
    }
    
    /// <summary>
    /// XP por completar uma quest.
    /// </summary>
    public static long QuestExperience(int questLevel, int difficulty)
    {
        // difficulty: 1=easy, 2=normal, 3=hard, 4=epic
        return 100L * questLevel * difficulty;
    }
}