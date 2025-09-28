using GameWeb.Application.Common.Interfaces;
using GameWeb.Domain.Entities;

namespace GameWeb.Application.Characters.Specifications;

/// <summary>
/// Encontra um personagem pelo seu nome exato.
/// Esta especificação é usada principalmente para verificações de unicidade.
/// </summary>
public class CharacterByNameSpec : BaseSpecification<Character>
{
    public CharacterByNameSpec(string characterName)
        // O critério compara o valor do Value Object 'Name' com a string fornecida.
        : base(c => c.Name == characterName)
    {
        // Para uma verificação de existência ('AnyAsync'), não precisamos de tracking.
        AsNoTrackingEnabled();
        // Também ignoramos filtros globais para garantir que verificamos todos os personagens,
        // incluindo os inativos, para evitar a criação de um novo com o mesmo nome.
        IgnoreQueryFiltersEnabled();
    }
}
