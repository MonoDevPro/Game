using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.Domain.DomainServices;

namespace Game.Domain.Specifications.Character;

/// <summary>
/// Especificação para validar se um personagem pode promover para uma vocação específica.
/// </summary>
public class CanPromoteSpecification
{
    /// <summary>
    /// Verifica se o personagem satisfaz os requisitos de promoção.
    /// </summary>
    public bool IsSatisfiedBy(Entities.Character character, VocationType targetVocation)
    {
        var targetInfo = VocationRegistry.Get(targetVocation);
        
        return targetInfo.BaseVocation == character.Vocation
            && character.Progress.Level >= targetInfo.PromotionLevel
            && character.IsActive;
    }

    /// <summary>
    /// Retorna a razão pela qual a promoção falhou, se aplicável.
    /// </summary>
    public string GetFailureReason(Entities.Character character, VocationType targetVocation)
    {
        var targetInfo = VocationRegistry.Get(targetVocation);
        
        if (targetInfo.BaseVocation != character.Vocation)
            return $"Cannot promote from {character.Vocation} to {targetVocation}";
        
        if (character.Progress.Level < targetInfo.PromotionLevel)
            return $"Level {targetInfo.PromotionLevel} required for promotion";
        
        if (!character.IsActive)
            return "Character must be active to promote";
        
        return string.Empty;
    }
}
