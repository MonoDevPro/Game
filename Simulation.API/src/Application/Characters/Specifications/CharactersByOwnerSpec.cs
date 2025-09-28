using GameWeb.Application.Common.Interfaces;
using GameWeb.Domain.Entities;

namespace GameWeb.Application.Characters.Specifications;

/// <summary>
/// Obtém todos os personagens que pertencem a um utilizador específico,
/// ordenados pelo nome.
/// </summary>
public class CharactersByOwnerSpec : BaseSpecification<Character>
{
    public CharactersByOwnerSpec(string ownerId)
        : base(c => c.OwnerId == ownerId)
    {
        AddOrderBy(c => c.Name);
        AsNoTrackingEnabled();
    }
}
