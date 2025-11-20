## 1. Estrutura atual do sistema de NPCs

Hoje você já tem um pipeline bem definido “server → ECS → snapshots → client”:

### Núcleo ECS / Servidor

- **Arquétipo de NPC**
  - [`Game.ECS/Entities/Archetypes/NpcArchetype.cs`](https://github.com/MonoDevPro/Game/blob/551e93b8321e1b1174aa43c2b33b5d05c7d7ed62/Game.ECS/Entities/Archetypes/NpcArchetype.cs)
  - `GameArchetypes.NPCCharacter` inclui:
    - `NetworkId`, `MapId`, `Position`, `Facing`, `Velocity`
    - `Movement`, `Walkable`, `Attackable`
    - `Health`
    - `AttackPower`, `Defense`, `CombatState`
    - `Input`, `AIControlled`
    - `DirtyFlags`
  - Comentário: “NPC com IA, suporta movimento e combate, sem input de jogador”.

- **Dados / Snapshots**
  - [`Game.ECS/Entities/Data/NpcData.cs`](https://github.com/MonoDevPro/Game/blob/551e93b8321e1b1174aa43c2b33b5d05c7d7ed62/Game.ECS/Entities/Data/NpcData.cs)
    - `NPCData`: dados estáticos/estado base (spawn).
    - `NpcStateData`: snapshot de estado (pos, facing, speed, HP).

- **Criação e snapshots**
  - [`Game.ECS/Entities/Factories/NpcFactory.cs`](https://github.com/MonoDevPro/Game/blob/551e93b8321e1b1174aa43c2b33b5d05c7d7ed62/Game.ECS/Entities/Factories/NpcFactory.cs)
    - `EntityFactory.CreateNPC(World world, in NPCData data)`:
      - Cria entidade com `GameArchetypes.NPCCharacter`.
      - Seta `Position`, `Facing`, `Velocity`, `Health`, `Walkable`, `Attackable`, `AttackPower`, `Defense`, `CombatState`, `Input`, `AIControlled`, `DirtyFlags`.
  - [`Game.ECS/Entities/Factories/NpcBuilder.cs`](https://github.com/MonoDevPro/Game/blob/551e93b8321e1b1174aa43c2b33b5d05c7d7ed62/Game.ECS/Entities/Factories/NpcBuilder.cs)
    - `BuildNPCSnapshot(World, Entity)` (snapshot de spawn).
    - `BuildNpcStateSnapshot(World, Entity)` → monta `NpcStateData` com:
      - `NetworkId`, `Position`, `Facing`, `Speed` (de `Walkable`), `Health`.

- **Índice**
  - [`Game.ECS/Entities/Repositories/NpcIndex.cs`](https://github.com/MonoDevPro/Game/blob/551e93b8321e1b1174aa43c2b33b5d05c7d7ed62/Game.ECS/Entities/Repositories/NpcIndex.cs)
    - `NpcIndex : EntityIndex<int>` baseado em `NetworkId`.

- **Serviço de spawn no servidor**
  - [`Game.Server/Npc/NpcSpawnService.cs`](https://github.com/MonoDevPro/Game/blob/551e93b8321e1b1174aa43c2b33b5d05c7d7ed62/Game.Server/Npc/NpcSpawnService.cs)
    - Mantém `_definitions` (`NpcSpawnDefinition`) com atributos de status/combate.
    - Controla `_activeNetworkIds`, gera `NetworkId` (`GenerateNetworkId()`).
    - Métodos:
      - `SpawnInitialNpcs()`
      - `BuildSnapshots()` → `IEnumerable<NPCData>`
      - `TryDespawnNpc(int networkId)`
      - `BuildNpcData(NpcSpawnDefinition definition)`.

### Camada core / networking

- [`Game.Core/Extensions/NpcDataExtensions.cs`](https://github.com/MonoDevPro/Game/blob/551e93b8321e1b1174aa43c2b33b5d05c7d7ed62/Game.Core/Extensions/NpcDataExtensions.cs)
  - Converte entre:
    - `NPCData` ↔ `NpcSpawnSnapshot` (packets de spawn).
    - `NpcStateData` ↔ `NpcStateSnapshot` (packets de estado).
  - Faz o “glue” entre ECS e camada de rede (`Game.Network.Packets.Game`).

### Cliente (Godot)

- [`Simulation.Client/godot-client/Scripts/Simulation/NpcVisual.cs`](https://github.com/MonoDevPro/Game/blob/551e93b8321e1b1174aa43c2b33b5d05c7d7ed62/Simulation.Client/godot-client/Scripts/Simulation/NpcVisual.cs)
  - `NpcVisual : DefaultVisual`
  - `Create()` instancia o prefab `NpcVisual.tscn`.
  - `UpdateFromSnapshot(NPCData data)`:
    - `LoadSprite(VocationType.Archer, Gender.Male);`
    - `UpdateName("NPC " + data.NetworkId);`
    - `UpdateAnimationState(new Vector2I(0, 1), false, false);`
    - `UpdatePosition(new Vector3I(...))`
    - `UpdateVitals(data.Hp, data.MaxHp, ...)`
  - `UpdateFromState(NpcStateData state)` (assinado; lógica a completar/expandir).

- [`Simulation.Client/godot-client/Scripts/UI/Menu/MenuScript.cs`](https://github.com/MonoDevPro/Game/blob/551e93b8321e1b1174aa43c2b33b5d05c7d7ed62/Simulation.Client/godot-client/Scripts/UI/Menu/MenuScript.cs)
  - `HandleNpcSpawnWhileConnecting(...)`:
    - Se bufferiza `NpcSpawnPacket` enquanto conecta:
      - `GameStateManager.Instance.StoreNpcSnapshots(packet.Npcs);`

---

## 2. O que está completo, parcial e faltando

### Já bem encaminhado / essencialmente completo

- **Infraestrutura ECS para NPCs:**
  - Arquétipo (`NPCCharacter`), dados (`NPCData`, `NpcStateData`), fábrica (`CreateNPC`), builder de snapshots.
- **Serviço de spawn no servidor (`NpcSpawnService`)**:
  - Definições default (`BuildDefaultDefinitions`).
  - Criação, snapshot e despawn de NPCs.
- **Conversão para/da camada de rede (`NpcDataExtensions`).**
- **Visual básico de NPC no cliente (`NpcVisual`)**:
  - Atualização de posição, HP, nome, animação padrão.
- **Buffer de snapshots de NPC no fluxo de conexão (`MenuScript.HandleNpcSpawnWhileConnecting`).**
- **Estrutura para indexação (`NpcIndex`).**

### Parcialmente feito (precisa ser concluído)

- **Comportamento / IA de NPC**
  - Você tem um componente `AIControlled` no arquétipo, mas:
    - Não encontrei, neste recorte, sistemas claros de:
      - `NpcAISystem`, `NpcPatrolSystem`, `NpcAggroSystem`, etc.
    - Provavelmente a IA ainda está vaga ou misturada em um sistema genérico de movimento/combat.
- **Uso completo de `NpcStateData` e `UpdateFromState` no cliente**
  - A assinatura está lá, mas a lógica de animação e interpolações ainda parece simples/minimal.
- **Configuração externa / data-driven de NPCs**
  - `NpcSpawnService` usa `BuildDefaultDefinitions()` hard-coded.
  - Falta camada para carregar configurações de NPC (stats, tipo, comportamento) de arquivo/DB/script.

### O que está faltando para um sistema de NPCs “completo”

Do ponto de vista de jogo, para considerar o sistema de NPCs “fechado”/coeso:

1. **Estados de IA claros (state machine)**
   - Estados básicos: `Idle`, `Patrolling`, `Chasing`, `Attacking`, `Fleeing`, `Dead`.
   - Dados de configuração de comportamento: raio de visão, raio de perseguição, cooldown de ataque, etc.

2. **Pathfinding / movimentação guiada**
   - Integração de NPC com:
     - Caminhos predefinidos (waypoints / rotas).
     - Eventual sistema de pathfinding do jogo (grid/navmesh) se já existir.
   - Hoje o NPC tem `Walkable`, `Movement`, `Velocity`, mas falta “quem” decide o destino e rota.

3. **Sistema de diálogos / interação**
   - Pelo menos:
     - Trigger para abrir diálogo ao aproximar/acionar.
     - Dados de diálogos (textos, múltiplas falas, opções simples).
   - Ligação com UI de diálogo no cliente.

4. **Rotinas diárias / scripts simples**
   - Possibilidade de:
     - Rotas pré-programadas (ex.: andando em círculo, indo/trocando de posição em horários).
     - Estados “decorativos” (ex.: ficar parado, sentar, executar animação de trabalho).

5. **Integração com outros sistemas**
   - **Quests**:
     - NPC como “giver/receiver” de quest.
   - **Inventário/loot**:
     - NPC hostil com tabela de drops.
   - **Combate**:
     - Já existe `AttackPower`, `Defense`, `CombatState`, mas falta:
       - Lógica de decisão de atacar / escolher alvo / checar range.
   - **UI / feedback ao jogador**:
     - Exibir ícones de quest, minimapa, nomes coloridos por tipo (amigável, neutro, hostil).

6. **Ferramentas de debug e testes**
   - Comandos de debug para spawn/despawn NPC, alterar estado de IA, teleportar, etc.
   - Testes unitários e testes de simulação offline.

---

## 3. Plano de finalização do sistema de NPCs

Organizando por camadas:

### 3.1. Domínio / dados de NPC

**Objetivo:** Tornar NPCs data-driven e diferenciáveis por “tipo”.

**Sugestão de módulos/classe:**

1. **`Game.Domain/Npcs/NpcType.cs`**
   - Enum ou struct:
     ```csharp
     public enum NpcType
     {
         PassiveVendor,
         PassiveCitizen,
         AggressiveMelee,
         AggressiveRanged,
         QuestGiver
     }
     ```

2. **`Game.Domain/Npcs/NpcBehaviorConfig.cs`**
   - Dados de comportamento:
     ```csharp
     public sealed record NpcBehaviorConfig
     (
         NpcType Type,
         float AggroRange,
         float ChaseRange,
         float AttackRange,
         float PatrolRadius,
         float IdleDurationMin,
         float IdleDurationMax,
         bool CanFlee,
         float FleeHealthThreshold
     );
     ```

3. **`Game.Domain/Npcs/NpcConfigRepository`**
   - Carrega configs a partir de JSON/YAML ou script (pode ser stub inicialmente):
     - Chaveada por `NpcType` ou `NetworkId`/`NpcId`.

4. **Estender `NPCData` (se fizer sentido)**
   - Adicionar um `NpcTypeId` ou `BehaviorId`:
     ```csharp
     public readonly record struct NPCData(
         int NetworkId,
         int PositionX, int PositionY, int PositionZ,
         int Hp, int MaxHp, float HpRegen,
         int PhysicalAttack, int MagicAttack,
         int PhysicalDefense, int MagicDefense,
         int MapId = 0,
         int NpcTypeId = 0);
     ```

### 3.2. IA e estados de NPC

**Responsabilidades:**

- **Sistemas ECS** controlam:
  - Estado da IA (`AIState`), movimento (definem `Velocity` e `Facing`), decisão de ataque e alvo.
- **Componentes ECS** guardam:
  - Dados de estado da IA, alvo atual, timers (idle, patrol, cooldowns).

**Módulos/classes sugeridos:**

1. **Componentes (em `Game.ECS/Components`):**
   - `AIState`:
     ```csharp
     public enum NpcAIState
     {
         Idle,
         Patrolling,
         Chasing,
         Attacking,
         Fleeing,
         Dead
     }

     public struct AIState
     {
         public NpcAIState Current;
         public float StateTime;   // quanto tempo no estado atual
     }
     ```
   - `AITarget`:
     ```csharp
     public struct AITarget
     {
         public int TargetNetworkId;  // 0/−1 = sem alvo
         public float LastKnownX;
         public float LastKnownY;
     }
     ```
   - `AIPatrol` (opcional, ou simplies no próprio `AIState`):
     ```csharp
     public struct AIPatrol
     {
         public int CenterX;
         public int CenterY;
         public float Radius;
     }
     ```

   - Adicionar estes componentes ao arquétipo `NPCCharacter` se fizer sentido:
     - `Component<AIState>.ComponentType`, `Component<AITarget>.ComponentType`, `Component<AIPatrol>.ComponentType`.

2. **Sistemas (em `Game.ECS/Systems/Npc`):**

   - `NpcPerceptionSystem`
     - Responsável por detectar jogadores dentro de um raio (usando `Position` e `MapId`).
     - Atualiza `AITarget` e muda `AIState` de `Idle/Patrolling` → `Chasing` quando um alvo é encontrado.

   - `NpcAISystem`
     - State machine de alto nível:
       - Idle → Patrolling (timer).
       - Chasing → Attacking (quando dentro de `AttackRange`).
       - Attacking → Chasing/Idle.
       - Fleeing se HP baixo etc.
     - Atualiza `AIState.StateTime`.
     - Seta flags que outros sistemas usam (por ex. “quer atacar”).

   - `NpcMovementSystem`
     - Lê `AIState`/`AITarget` e decide o `Velocity` e `Facing` do NPC.
     - Usa o mesmo pipeline de movimento que o jogador (respeitando `Walkable`, colisão, etc.).
     - Integra com pathfinding quando existir.

   - `NpcCombatSystem` (caso ainda não exista separação clara)
     - Quando `AIState == Attacking`:
       - Verifica distância para `AITarget`.
       - Calcula dano usando `AttackPower` vs `Defense`.
       - Atualiza `Health` do alvo e `CombatState`.
       - Marca flags de hit para replicação, VFX, etc.

3. **Integração com `NpcSpawnService`**
   - Quando criar `NPCData`, também informar tipo de NPC / comportamento.
   - `CreateNPC` inicializa `AIState` com `NpcAIState.Idle` e `AIPatrol` com algum centro/raio padrão.

### 3.3. Diálogo e interação

**Responsabilidades:**

- Servidor:
  - Armazena quais NPCs têm diálogos/quests.
  - Valida e envia o conteúdo básico do diálogo.
- Cliente:
  - UI de diálogo, respostas do jogador.
- Rede:
  - Packets simples: “PlayerX iniciou diálogo com NpcY”, “Próxima fala/etapa”, etc.

**Sugestão de início mínimo:**

1. `Game.Domain/Dialogues/NpcDialogueDefinition`
2. `Game.Domain/Dialogues/NpcDialogueRepository`
3. Packet `NpcDialogueStartPacket` / `NpcDialogueLinePacket`.
4. No cliente, uma `DialogueUI` simples para mostrar texto e “Continuar/Fechar”.

Mesmo que você não implemente tudo agora, defina as interfaces e pontos de extensão.

### 3.4. Integração com outros sistemas (hooks)

- **Quests**
  - Adicionar Tag/ID de quest em `NpcDialogueDefinition` ou `NPCData`.
- **Inventário/loot**
  - Tabela de drops ligada ao tipo de NPC (no domínio).
- **UI**
  - `NpcVisual`:
    - Cor do nome por tipo (amigável/hostil).
    - Ícones de quest.
    - Animações condizentes com `AIState` (idle, walk, attack, death).

### 3.5. Apresentação/Visual (cliente Godot)

Expandir `NpcVisual`:

```csharp name=Simulation.Client/godot-client/Scripts/Simulation/NpcVisual.cs url=https://github.com/MonoDevPro/Game/blob/551e93b8321e1b1174aa43c2b33b5d05c7d7ed62/Simulation.Client/godot-client/Scripts/Simulation/NpcVisual.cs
public sealed partial class NpcVisual : DefaultVisual
{
    public static NpcVisual Create()
    {
        return GD.Load<PackedScene>("res://Scenes/Prefabs/NpcVisual.tscn").Instantiate<NpcVisual>();
    }

    public void UpdateFromSnapshot(NPCData data)
    {
        LoadSprite(VocationType.Archer, Gender.Male); // depois: variar por tipo de NPC
        UpdateName("NPC " + data.NetworkId);
        UpdatePosition(new Vector3I(data.PositionX, data.PositionY, data.PositionZ));
        UpdateVitals(data.Hp, data.MaxHp, 0, 0);
        UpdateAnimationState(new Vector2I(0, 1), false, false);
    }

    public void UpdateFromState(NpcStateData state)
    {
        UpdatePosition(new Vector3I(state.PositionX, state.PositionY, state.PositionZ));

        var moving = state.Speed > 0.01f;
        var facing = new Vector2I(state.FacingX, state.FacingY);
        UpdateAnimationState(facing, moving, isAttacking: false); // depois: usar info de combate
        if (Sprite is not null) UpdateAnimationSpeed(1f, 1f);
    }
}
```

---

## 4. Próximos passos concretos (ToDo com prioridades)

### Alta prioridade (fechar o loop básico de NPC com IA)

1. **Definir componentes de IA e estados**
   - [ ] Criar `AIState`, `AITarget`, `AIPatrol` em `Game.ECS/Components`.
   - [ ] Adicionar esses componentes em `GameArchetypes.NPCCharacter`.
   - [ ] Inicializar esses componentes em `EntityFactory.CreateNPC`.

2. **Criar sistemas de IA básicos**
   - [ ] `NpcPerceptionSystem`:
     - Detectar jogadores num raio fixo (hard-coded inicialmente) e setar `AITarget`.
   - [ ] `NpcAISystem`:
     - Implementar máquina de estados simples:
       - `Idle` → `Chasing` ao ver jogador.
       - `Chasing` → `Attacking` se perto.
       - `Attacking` → `Chasing` se jogador foge.
   - [ ] `NpcMovementSystem`:
     - Traduz estado + alvo em `Velocity` e `Facing` usando a infra de movimento existente.
   - [ ] Integrar esses sistemas no pipeline da `ServerGameSimulation`.

3. **Atualizar snapshots de estado**
   - [ ] Garantir que `BuildNpcStateSnapshot` capte tudo que o cliente precisa (posição, direção, HP, possivelmente estado de animação futuro).
   - [ ] Garantir envio periódico de `NpcStateData` para o cliente (reuso do pipeline já existente de snapshots de player, se houver).

4. **Cliente: consumir `NpcStateData`**
   - [ ] Implementar `NpcVisual.UpdateFromState` de forma consistente (como exemplo acima).
   - [ ] Garantir que `GameStateManager` atualize visuais de NPCs com base em `NpcStateSnapshot`.

### Média prioridade (data-driven + integração simples com outros sistemas)

5. **Configuração de NPC data-driven**
   - [ ] Criar `NpcBehaviorConfig` e `NpcConfigRepository` (mesmo que somente com dicionário estático no código inicialmente).
   - [ ] Adicionar `NpcTypeId` em `NPCData` (ou outro campo equivalente).
   - [ ] Ajustar `NpcSpawnService.BuildNpcData` para setar `NpcTypeId` e carregar configs.
   - [ ] Usar configs em `NpcPerceptionSystem` / `NpcAISystem` (raios de visão/ataque, etc.).

6. **Interação básica (diálogo placeholder)**
   - [ ] Definir um componente tag, ex.: `NpcInteractable`.
   - [ ] Definir um evento/packet simples: “player pediu diálogo com NPC X”.
   - [ ] No cliente, abrir uma UI placeholder com texto fixo carregado de `NpcDialogueRepository`.

### Baixa prioridade (polimento, rotinas, debug, testes)

7. **Rotinas diárias / patrol**
   - [ ] Implementar `AIPatrol` usando waypoints fixos ou posição + raio.
   - [ ] No estado `Patrolling`, NPC caminha entre pontos.

8. **Integração com quests e loot (caso o sistema já exista)**
   - [ ] Adicionar metadados de quest/loot aos tipos de NPC.
   - [ ] No `NpcCombatSystem`, disparar lógica de loot/quest ao morrer.

9. **Ferramentas de debug**
   - [ ] Comando de console/server: spawnar NPC de tipo X em posição Y.
   - [ ] Visualização de raio de visão/patrol em build de debug.

10. **Testes**
    - **Unitários (Game.ECS / Game.Server)**
      - [ ] Testar transições de `NpcAISystem`:
        - Sem jogador → continua Idle.
        - Jogador entra no range → Chasing.
        - Jogador no range de ataque → Attacking.
      - [ ] Testar `NpcMovementSystem` mantendo NPC dentro do mapa/limites.
      - [ ] Testar `NpcSpawnService` (IDs únicos, número correto de NPCs, despawn).
    - **Teste de jogo**
      - [ ] Cenário: servidor com 2–3 NPCs agressivos → verificar perseguição e ataque.
      - [ ] Cenário: NPC passivo que patrulha uma área.