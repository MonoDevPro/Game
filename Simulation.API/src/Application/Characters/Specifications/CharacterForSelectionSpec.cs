using GameWeb.Application.Common.Interfaces;
using GameWeb.Domain.Entities;

namespace GameWeb.Application.Characters.Specifications;

/// <summary>
/// Valida se um personagem é válido para ser selecionado por um utilizador.
/// Verifica se o personagem existe, pertence ao utilizador e está ativo.
/// </summary>
public class CharacterForSelectionSpec : BaseSpecification<Character>
{
    public CharacterForSelectionSpec(int characterId, string ownerId)
        : base(c => c.Id == characterId && c.OwnerId == ownerId && c.IsActive)
    {
        // Apenas para validação, não precisa de tracking.
        AsNoTrackingEnabled();
    }
}
