using Game.Domain.AI.Data;

namespace Game.Domain.AI.Interfaces;

/// <summary>
/// Provider de templates de NPC.
/// </summary>
public interface INpcTemplateProvider
{
    NpcTemplate? Get(string id);
    IReadOnlyList<NpcTemplate> GetAll();
}