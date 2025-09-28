using System.Linq.Expressions;
using GameWeb.Application.Common.Interfaces;
using GameWeb.Domain.Entities;

namespace GameWeb.Application.Management.Specifications;

/// <summary>
/// Obtém todos os personagens para um ecrã de administração, com suporte
/// para filtragem por estado (ativo/inativo) e paginação.
/// </summary>
public class AllCharactersAdminSpec : BaseSpecification<Character>
{
    public AllCharactersAdminSpec(bool? isActive) : base(BuildCriteria(isActive))
    {
        AddOrderBy(c => c.Name);
        IgnoreQueryFiltersEnabled(); // Essencial para ver personagens inativos
        AsNoTrackingEnabled();
    }
    
    /// <summary>
    /// Um método auxiliar estático para construir a expressão de critério.
    /// Isto mantém o construtor limpo e a lógica de construção do filtro encapsulada.
    /// </summary>
    private static Expression<Func<Character, bool>>? BuildCriteria(bool? isActive)
    {
        if (isActive.HasValue)
        {
            return c => c.IsActive == isActive.Value;
        }

        // Se isActive for nulo, não há filtro, então retornamos nulo.
        return null;
    }
}
