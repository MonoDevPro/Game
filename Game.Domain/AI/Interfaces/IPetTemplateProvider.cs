using Game.Domain.AI.Data;

namespace Game.Domain.AI.Interfaces;

/// <summary>
/// Provider de templates de Pet.
/// </summary>
public interface IPetTemplateProvider
{
    PetTemplate? Get(string id);
    IReadOnlyList<PetTemplate> GetAll();
}