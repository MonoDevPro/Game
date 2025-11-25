A análise dos arquivos `Game.Server/Npc/NpcSpawnService.cs`, `Game.ECS/Entities/Factories/NpcFactory.cs` e sistemas relacionados confirma sua suspeita: o código funciona, mas mistura responsabilidades (dados hardcoded vs lógica de serviço) e a criação de entidades está muito acoplada.

Aqui está a proposta de refatoração completa para melhorar a **coesão**, tornar o sistema **orientado a dados (Data-Driven)** e limpar o fluxo de **Spawn**.

### Diagnóstico
1.  **Acoplamento de Dados:** O `NpcSpawnService` contém a lista de inimigos "Hardcoded" dentro do código (`BuildDefaultDefinitions`). Isso exige recompilação para mudar atributos de um Orc, por exemplo.
2.  **Responsabilidade Excessiva:** O `NpcSpawnService` gerencia definições, IDs de rede, snapshots e lógica de spawn ao mesmo tempo.
3.  **Fábrica Monolítica:** O `NpcFactory.cs` recebe um struct gigante (`NPCData`) que serve como um "Objeto Deus" para transferir dados, o que pode gerar problemas de performance e manutenção.

---

### Plano de Refatoração

Vamos separar o sistema em 3 camadas claras:
1.  **Dados (JSON/Scriptable):** Definições dos NPCs carregadas externamente.
2.  **Catálogo (Repository):** Cache e acesso aos templates de NPCs.
3.  **Serviço (Spawner):** Apenas gerencia o ciclo de vida (criar/destruir).

#### 1. Nova Estrutura de Dados (Substituindo Hardcode)

Primeiro, criamos um modelo de template puro, desacoplado do ECS.

```csharp name=Game.Domain/Templates/NpcTemplate.cs
namespace Game.Domain.Templates;

// Representa o "Molde" de um NPC, carregado de JSON/Database
public class NpcTemplate
{
    public string Id { get; set; } // Ex: "orc_warrior"
    public string Name { get; set; }
    public int BaseHp { get; set; }
    public int BaseMp { get; set; }
    // ... outros atributos base
    
    public NpcStats Stats { get; set; }
    public NpcBehaviorConfig Behavior { get; set; }
}

public class NpcStats 
{
    public float MovementSpeed { get; set; }
    public int PhysicalAttack { get; set; }
    // ...
}
```

#### 2. Criar um Repositório de NPCs
Isso remove a responsabilidade de "conhecer os monstros" do `NpcSpawnService`.

```csharp name=Game.Server/Npc/NpcRepository.cs
public interface INpcRepository
{
    NpcTemplate GetTemplate(string templateId);
    IEnumerable<NpcSpawnPoint> GetSpawnPoints(int mapId);
}

public class NpcRepository : INpcRepository
{
    private readonly Dictionary<string, NpcTemplate> _templates = new();

    public void LoadTemplates(string jsonContent)
    {
        // Deserializar JSON e popular _templates
        // Isso permite recarregar balanceamento sem reiniciar o servidor (Hot Reload)
    }
    
    // ... implementação dos métodos
}
```

#### 3. Refatorar o `NpcSpawnService`
Ele agora foca apenas em *gerenciar a existência* dos NPCs.

```csharp name=Game.Server/Npc/NpcSpawnService.cs
public sealed class NpcSpawnService
{
    private readonly ServerGameSimulation _simulation;
    private readonly INpcRepository _repository;
    private readonly Dictionary<int, int> _activeNpcs = new(); // NetworkId -> EntityId (ou struct info)

    public NpcSpawnService(ServerGameSimulation simulation, INpcRepository repository)
    {
        _simulation = simulation;
        _repository = repository;
    }

    public void SpawnInitialNpcs()
    {
        // O serviço pede ao repositório: "Onde devo criar monstros?"
        var spawnPoints = _repository.GetSpawnPoints(mapId: 0); 

        foreach (var spawn in spawnPoints)
        {
            SpawnNpc(spawn.TemplateId, spawn.Position, spawn.MapId);
        }
    }

    public void SpawnNpc(string templateId, Position position, int mapId)
    {
        var template = _repository.GetTemplate(templateId);
        
        // Transforma Template em Entidade ECS
        // Note que não passamos mais aquele struct gigante NPCData manualmente aqui
        var entity = _simulation.CreateNpcFromTemplate(template, position, mapId);
        
        // Registrar ID para controle
    }
}
```

#### 4. Refatorar a Fábrica (`NpcFactory`)
Em vez de receber um struct `NPCData` gigante com 30 parâmetros, a fábrica deve usar o `NpcTemplate`.

**Antes (Problema):**
```csharp
// Assinatura gigante e difícil de manter
public static Entity CreateNPC(this World world, in NPCData data, in NpcBehaviorData behaviorData)
```

**Depois (Solução):**
```csharp name=Game.ECS/Entities/Factories/NpcFactory.cs
public static partial class EntityFactory
{
    public static Entity CreateNPC(this World world, NpcTemplate template, Position pos, int mapId, int networkId)
    {
        var entity = world.Create(GameArchetypes.NPCCharacter);

        // Componentes derivados diretamente do template limpo
        world.Set(entity, new Position { X = pos.X, Y = pos.Y });
        world.Set(entity, new NetworkId { Value = networkId });
        
        // Stats vêm direto do template, sem conversão intermediária
        world.Set(entity, new Health { Current = template.BaseHp, Max = template.BaseHp });
        world.Set(entity, new AttackPower { Physical = template.Stats.PhysicalAttack });
        
        // Comportamento
        world.Set(entity, new NpcBehavior 
        { 
            Type = template.Behavior.Type,
            AggroRange = template.Behavior.AggroRange 
        });

        return entity;
    }
}
```

### Benefícios Desta Refatoração

1.  **Performance:** Remove a criação intermediária do struct `NPCData` durante o spawn. O template é lido uma vez e aplicado diretamente aos componentes ECS (que são structs leves).
2.  **Coesão:** O `NpcMovementSystem` e outros sistemas não precisam mudar, mas a forma como os dados chegam até eles fica limpa.
3.  **Manutenibilidade:** Se quiser adicionar um novo monstro ("Dragon"), você cria um arquivo JSON/Script. Não precisa recompilar o C# nem mexer no `NpcSpawnService.cs`.
4.  **Hot Reload:** Com o Repositório, você pode criar um comando de admin `/reloadnpcs` que atualiza os atributos dos monstros em tempo real sem derrubar o servidor.

Esta abordagem transforma o sistema de hardcoded para uma arquitetura profissional de servidor de jogo escalável.