using System.Runtime.InteropServices;

namespace Game.Domain.Commons.ValueObjects.Character;

/// <summary>
/// Componente ECS de progressão de nível.
/// Struct imutável otimizada para cache-friendly iteration.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Progress(int Level, long Experience, long ExperienceToNext)
{
    public const int MaxLevel = 500;
    private const double GrowthRate = 1.15;
    private const long BaseExperience = 100;

    public static Progress Initial => new(Level: 1, Experience: 0, ExperienceToNext: CalculateExpForLevel(2));
    
    public static Progress Create(int level, long experience)
    {
        return new Progress(
            Level: Math.Clamp(level, 1, MaxLevel), 
            Experience: Math.Max(0, experience),
            ExperienceToNext: CalculateExpForLevel(level + 1));
    }

    /// <summary>
    /// XP total necessário para alcançar um nível específico.
    /// Fórmula exponencial: 100 * (1.15^(level-1))
    /// </summary>
    public static long CalculateExpForLevel(int level)
    {
        if (level <= 1) return 0;
        return (long)(BaseExperience * Math.Pow(GrowthRate, level - 1));
    }

    /// <summary>
    /// XP acumulado necessário desde o nível 1.
    /// </summary>
    public static long TotalExpForLevel(int level)
    {
        if (level <= 1) return 0;
        // Soma da série geométrica: a * (r^n - 1) / (r - 1)
        return (long)(BaseExperience * (Math.Pow(GrowthRate, level - 1) - 1) / (GrowthRate - 1));
    }

    /// <summary>
    /// Progresso percentual para o próximo nível (0.0 a 1.0).
    /// </summary>
    public double LevelProgress
    {
        get
        {
            if (Level >= MaxLevel) return 1.0;
            var currentLevelExp = TotalExpForLevel(Level);
            var nextLevelExp = TotalExpForLevel(Level + 1);
            var range = nextLevelExp - currentLevelExp;
            return range <= 0 ? 1.0 : (double)(Experience - currentLevelExp) / range;
        }
    }

    public bool CanLevelUp => Level < MaxLevel && Experience >= ExperienceToNext;
    
    public bool IsMaxLevel => Level >= MaxLevel;

    /// <summary>
    /// Adiciona XP e processa level ups automaticamente.
    /// Retorna novo Progress (imutável).
    /// </summary>
    public Progress AddExperience(long amount)
    {
        if (amount <= 0 || IsMaxLevel) return this;

        var newExp = Experience + amount;
        var newLevel = Level;
        var newExpToNext = ExperienceToNext;

        // Processa múltiplos level ups
        while (newLevel < MaxLevel && newExp >= newExpToNext)
        {
            newExp -= newExpToNext;
            newLevel++;
            newExpToNext = CalculateExpForLevel(newLevel + 1);
        }

        return new Progress
        {
            Level = newLevel,
            Experience = newExp,
            ExperienceToNext = newExpToNext
        };
    }

    /// <summary>
    /// Tenta subir apenas um nível.  Útil para sistemas que precisam
    /// processar efeitos de level up individualmente.
    /// </summary>
    public bool TryLevelUp(out Progress result)
    {
        if (! CanLevelUp)
        {
            result = this;
            return false;
        }

        result = new Progress
        {
            Level = Level + 1,
            Experience = Experience - ExperienceToNext,
            ExperienceToNext = CalculateExpForLevel(Level + 2)
        };
        return true;
    }

    /// <summary>
    /// Calcula níveis ganhos com determinada quantidade de XP.
    /// </summary>
    public int LevelsGainedWith(long amount) => AddExperience(amount).Level - Level;

    public override string ToString() => 
        $"Lv. {Level} ({Experience: N0}/{ExperienceToNext:N0} XP)";
}