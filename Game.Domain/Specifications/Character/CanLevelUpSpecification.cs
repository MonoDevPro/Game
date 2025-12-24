using Game.Domain.Entities;
using Game.Domain.ValueObjects.Character;

namespace Game.Domain.Specifications.Character;

/// <summary>
/// Especificação para validar se um personagem pode subir de nível.
/// </summary>
public class CanLevelUpSpecification
{
    /// <summary>
    /// Verifica se o personagem pode subir de nível.
    /// </summary>
    public bool IsSatisfiedBy(Entities.Character character)
    {
        return character.Progress.CanLevelUp && character.IsActive;
    }

    /// <summary>
    /// Retorna a razão pela qual o level up falhou, se aplicável.
    /// </summary>
    public string GetFailureReason(Entities.Character character)
    {
        if (!character.IsActive)
            return "Character must be active to level up";
        
        if (character.Progress.Level >= Progress.MaxLevel)
            return $"Character has reached maximum level ({Progress.MaxLevel})";
        
        if (!character.Progress.CanLevelUp)
            return $"Not enough experience. Need {character.Progress.ExperienceToNextLevel} more XP";
        
        return string.Empty;
    }
}
