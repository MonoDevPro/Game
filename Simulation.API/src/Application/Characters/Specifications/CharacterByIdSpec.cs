using GameWeb.Application.Common.Interfaces;
using GameWeb.Domain.Entities;

namespace GameWeb.Application.Characters.Specifications;

/// <summary>
/// Obtém um único personagem pelo seu ID.
/// Opcionalmente, pode verificar se o personagem pertence a um utilizador específico.
/// </summary>
public class CharacterByIdSpec : BaseSpecification<Character>
{
    public CharacterByIdSpec(int characterId, string? ownerId = null)
        : base(c => c.Id == characterId && (ownerId == null || c.OwnerId == ownerId))
    {
        // Aplica AsNoTracking para otimizar consultas de apenas leitura.
        AsNoTrackingEnabled();
    }
}
