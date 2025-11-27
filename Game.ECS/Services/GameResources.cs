using Game.Domain.Templates;

namespace Game.ECS.Services;

public class GameResources
{
    // Banco de Strings (Nomes de NPCs)
    public ResourceLifecycle<string> Strings { get; private set; } = new(capacity: 10);

    // Banco de Templates (Configurações pesadas de Classes/JSON)
    public ResourceLifecycle<NpcTemplate> Templates { get; private set; } = new(capacity: 10);
}