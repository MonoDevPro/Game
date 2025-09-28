using GameWeb.Application.Common.Interfaces;
using GameWeb.Domain.Entities;

namespace GameWeb.Application.Management.Specifications;

/// <summary>
/// Seleciona todos os personagens que estão inativos, para serem removidos permanentemente.
/// </summary>
public class CharactersForPurgeSpec : BaseSpecification<Character>
{
    public CharactersForPurgeSpec(bool? isActive = null) : base(c => isActive == null || c.IsActive == isActive)
    {
        if (isActive is false)
            IgnoreQueryFiltersEnabled();
    }
}
