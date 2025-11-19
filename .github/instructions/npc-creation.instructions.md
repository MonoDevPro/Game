## 1. Objetivos do sistema de NPCs

1.1. **Servidor (autoridade):**
- Gerar, manter e destruir NPCs como entidades ECS com IA.
- Rodar lógica de movimento/comportamento (AI) e combate.
- Sincronizar estado relevante dos NPCs com os clientes:
  - Snapshot inicial (spawn).
  - Atualizações periódicas/incrementais de posição, direção, HP, etc.
- Integrar NPCs com:
  - `MapService` / `MapSpatial` para queries espaciais.
  - Sistemas de combate já existentes (Attackable, AttackPower, Defense, CombatState).

1.2. **Cliente (visual):**
- Receber snapshots/estados de NPCs vindos do servidor.
- Criar/remover entidades locais e nós visuais (`NpcVisual`) para cada NPC.
- Atualizar posição, animações e UI (nome/HP) a partir dos snapshots.
- Integrar com `ClientVisualSyncSystem` e `ClientGameSimulation`.

1.3. **Requisitos gerais:**
- Reutilizar o que já existe:
  - `Game.ECS.Entities.Archetypes.NpcArchetype` (`GameArchetypes.NPCCharacter`).
  - `NpcFactory` (`NPCData` / `NpcStateData` e métodos de snapshot).
  - `NpcVisual` no client.
- Seguir padrão atual de players:
  - Servidor gera `PlayerData` / `PlayerStatePacket` etc.
  - Cliente registra handlers no `GameClient.RegisterPacketHandlers()`.

---

## 2. Modelo ECS de NPC no servidor

### 2.1. Archetype e componentes

Referência existente:

- `Game.ECS/Entities/Archetypes/NpcArchetype.cs`:

```csharp
public static readonly ComponentType[] NPCCharacter =
[
    Component<NetworkId>.ComponentType,
    Component<MapId>.ComponentType,
    Component<Position>.ComponentType,
    Component<Facing>.ComponentType,
    Component<Velocity>.ComponentType,
    Component<Movement>.ComponentType,
    Component<Health>.ComponentType,
    Component<Walkable>.ComponentType,
    Component<Attackable>.ComponentType,
    Component<AttackPower>.ComponentType,
    Component<Defense>.ComponentType,
    Component<CombatState>.ComponentType,
    Component<AIControlled>.ComponentType,
    Component<DirtyFlags>.ComponentType
];
```

Tarefas:

1. Garantir que todos os componentes acima estejam bem definidos em `Game.ECS.Components`.
2. Verificar se `AIControlled` e `DirtyFlags` já são utilizados em outros sistemas; se não:
   - Documentar o contrato de `DirtyFlags` (quais bits indicam mudança de posição, HP, etc.).
   - Definir o uso de `AIControlled` (tag para sistemas de AI filtrarem apenas NPCs).

### 2.2. `NpcFactory` no servidor

Arquivo: `Game.ECS/Entities/Factories/NpcFactory.cs`

- Estruturas existentes:

```csharp
public readonly record struct NPCData(
    int NetworkId,
    int PositionX, int PositionY, int PositionZ,
    int Hp, int MaxHp, float HpRegen,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense,
    int MapId = 0);

public readonly record struct NpcStateData(
    int NetworkId,
    int PositionX,
    int PositionY,
    int PositionZ,
    int FacingX,
    int FacingY,
    float Speed,
    int CurrentHp,
    int MaxHp);
```

E métodos (a implementar/completar):

```csharp
public static class NpcFactory
{
    public static Entity CreateNPC(this World world, in NPCData data) { /*...*/ }

    public static NPCData BuildNPCSnapshot(this World world, Entity entity) { /*...*/ }

    public static NpcStateData BuildNpcStateSnapshot(this World world, Entity entity) { /*...*/ }
}
```

Tarefas para o especialista:

1. Implementar `CreateNPC` usando `GameArchetypes.NPCCharacter`:
   - Setar `NetworkId`, `MapId`, `Position`, `Facing`, `Velocity`, `Movement`.
   - Setar `Health` com `Hp`, `MaxHp`, `HpRegen`.
   - Setar componentes de combate (`AttackPower`, `Defense`, `CombatState`).
   - Marcar `AIControlled`.
   - Inicializar `DirtyFlags` com todos os bits relevantes como “dirty” no spawn.

2. Implementar `BuildNPCSnapshot`:
   - Extrair estado completo da entidade (valores que definem o NPC em si, para spawn).
   - Usar `NetworkId`, `Position`, `Health`, stats de combate.
   - Esse método será usado quando um cliente entra no jogo e precisa do snapshot inicial de todos os NPCs visíveis.

3. Implementar `BuildNpcStateSnapshot`:
   - Focado em estado dinâmico: posição, facing, velocidade atual, HP.
   - Usado em atualizações periódicas (tick de sync) para reduzir payload.

---

## 3. Sistema de NPCs no servidor (AI + spawn + sync)

### 3.1. Integração com `ServerGameSimulation`

Arquivo: `Game.Server/ECS/ServerGameSimulation.cs`

```csharp
public sealed class ServerGameSimulation : GameSimulation
{
    protected override void ConfigureSystems(World world, Group<float> systems)
    {
        // ... sistemas já existentes ...

        // 7. SpatialSyncSystem
        systems.Add(new SpatialSyncSystem(World, MapService, _loggerFactory.CreateLogger<SpatialSyncSystem>()));
        
        // 8. ServerSyncSystem
        systems.Add(new ServerSyncSystem(world, _networkManager, _loggerFactory.CreateLogger<ServerSyncSystem>()));
    }
}
```

Tarefas:

1. Adicionar sistemas de NPC antes de `ServerSyncSystem` e depois dos sistemas de movimento/combat:
   - `NpcAISystem` (controle de comportamento).
   - `NpcSpawnSystem` opcional (para spawn/despawn automático com base em configuração ou triggers).

   Exemplo de ordem:

   ```csharp
   // ... Input → Movement → Combat ...
   systems.Add(new NpcAISystem(world, MapService, _loggerFactory.CreateLogger<NpcAISystem>()));
   // opcional: systems.Add(new NpcSpawnSystem(world, MapService, _loggerFactory.CreateLogger<NpcSpawnSystem>()));
   systems.Add(new SpatialSyncSystem(World, MapService, _loggerFactory.CreateLogger<SpatialSyncSystem>()));
   systems.Add(new ServerSyncSystem(world, _networkManager, _loggerFactory.CreateLogger<ServerSyncSystem>()));
   ```

2. Garantir que `MapService` já esteja povoada com `Map` (já é feito em `Program.cs`).

### 3.2. `NpcAISystem`

Responsabilidades:

- Filtrar entidades com `AIControlled`, `Position`, `Movement`, `Health`, e opcionalmente `CombatState`.
- Usar `MapSpatial` / `MapGrid` do `MapService` para:
  - Buscar alvos (players) próximos.
  - Calcular caminho simples ou aproximado (por enquanto, pode ser “walk towards target” sem pathfinding complexo, se desejado).
- Atualizar `Movement` / `Velocity` e `Facing` baseado no comportamento.
- Atualizar `CombatState` quando dentro do range de ataque.
- Ajustar `DirtyFlags` quando houver mudanças relevantes.

Tarefas:

1. Criar `NpcAISystem` em `Game.Server/ECS/Systems` (ou pasta adequada).
2. Implementar padrão base similar aos outros sistemas ECS (ver sistemas de movimento/combat existentes).
3. Configurar queries:
   - Query 1: NPCs com `AIControlled` e `Health` > 0.
   - Query 2: `MapSpatial` para buscar players próximos.

4. Minimizar complexidade no primeiro momento:
   - Comportamento básico: “seguir o player mais próximo” até certa distância, parar e atacar.

### 3.3. Spawn e gerenciamento de NPCs

Há duas abordagens recomendadas:

#### 3.3.1. Serviço de spawn inicial

- Criar `NpcSpawnService` similar ao `PlayerSpawnService`:

  - Local: `Game.Server/Npc/NpcSpawnService.cs` (ou pasta adequada).
  - Responsabilidades:
    - Carregar config de NPCs por mapa (pode ser hardcoded inicialmente).
    - Em `Start()` do jogo ou quando o mapa é carregado, criar NPCs via `ServerGameSimulation.World.CreateNPC(...)`.
    - Registrar estes NPCs em alguma estrutura interna se necessário (para re-spawn, etc.).

- Integração com `GameLoopService` ou `GameServer`:
  - Disparar spawn inicial após a simulação estar pronta.

#### 3.3.2. Integração com o sistema existente de sync

- O `ServerSyncSystem` deve ser estendido ou configurado para:
  - Incluir NPCs nas mensagens enviadas para o cliente.
  - Utilizar `NpcFactory.BuildNPCSnapshot` para snapshots completos.
  - Utilizar `NpcFactory.BuildNpcStateSnapshot` para updates de estado.
  - Diferenciar entre “mensagens de player” e “mensagens de NPC” via tipos de pacotes separados.

---

## 4. Protocolo de rede para NPCs

### 4.1. Novos pacotes

Seguir padrão em `Game.Network.Packets.Game` (onde já existem `PlayerDataPacket`, `PlayerStatePacket`, etc.).

Propor, por exemplo:

1. `NpcSpawnPacket`
   - Enviado do servidor → clientes.
   - Payload: lista de `NPCData` ou 1 por pacote, dependendo de design.
   - Usado quando:
     - Player entra no jogo (snapshot inicial do mundo).
     - Novo NPC é criado no mundo.

2. `NpcDespawnPacket`
   - Contém `NetworkId` do NPC a ser despawnado.
   - Enviado quando o NPC é destruído (morte ou saída da área).

3. `NpcStatePacket`
   - Atualização periódica dos NPCs (delta ou full state simples).
   - Pode carregar uma lista de `NpcStateData`.

Tarefas:

1. Definir esses pacotes em `Game.Network.Packets.Game` com `[Serializable]` ou padrão utilizado.
2. Atualizar `PacketProcessor` para registrar e serializar/deserializar esses tipos.
3. No `ServerSyncSystem`, enviar:
   - `NpcSpawnPacket` quando um NPC entra no “interesse” de um jogador (ou via modelo simples: broadcast global inicialmente).
   - `NpcDespawnPacket` quando sai.
   - `NpcStatePacket` com frequência fixa (ex. 10–20 Hz) ou combinando com o tick dos players.

4. Utilizar `NetworkChannel.Game` e um `NetworkDeliveryMethod` compatível (similar aos pacotes de estado de player).

---

## 5. Lado cliente – integração com `GameClient` e `ClientGameSimulation`

### 5.1. Node visual `NpcVisual`

Arquivo: `Simulation.Client/godot-client/Scripts/Simulation/NpcVisual.cs`

```csharp
public sealed partial class NpcVisual : DefaultVisual
{
    public static NpcVisual Create()
    {
        return GD.Load<PackedScene>("res://Scenes/Prefabs/NpcVisual.tscn").Instantiate<NpcVisual>();
    }
    
    public void UpdateFromSnapshot(NPCData data)
    {
        LoadSprite(VocationType.Archer, Gender.Male);
        UpdateName("NPC " + data.NetworkId);
        UpdateAnimationState(new Vector2I(0, 1), false, false);
        UpdatePosition(new Vector3I(data.PositionX, data.PositionY, data.PositionZ));
        UpdateVitals(data.Hp, data.MaxHp, 0, 0);
        if (Sprite is not null) UpdateAnimationSpeed(1f, 1f);
    }
}
```

Tarefas:

1. Confirmar que `NpcVisual.tscn` existe e está correto (root derivando de `NpcVisual`).
2. Extender `UpdateFromSnapshot` se necessário para:
   - Atualizar facing/anim de movimento.
   - Atualizar outras informações (por ex. `MapId` se visualmente relevante).
3. Criar método para updates de estado parcial, por ex.:

   ```csharp
   public void UpdateFromState(NpcStateData state)
   {
       UpdatePosition(new Vector3I(state.PositionX, state.PositionY, state.PositionZ));
       UpdateVitals(state.CurrentHp, state.MaxHp, 0, 0);
       // Atualizar facing com base em FacingX/Y
       // Ajustar animação de idle/movimento
   }
   ```

### 5.2. `ClientGameSimulation` – registrar entidades NPC

Arquivo: `Simulation.Client/godot-client/Scripts/ECS/ClientGameSimulation.cs`

- Já possui:
  - `RegisterSpatial(Entity entity)` que registra entity no `MapService` local.
  - `ClientVisualSyncSystem` para sincronizar visuais com entidades ECS.

Tarefas:

1. No client, ao receber um `NpcSpawnPacket`:
   - Criar uma entidade ECS para o NPC no `World` do `ClientGameSimulation` usando algo como `world.CreateNpcProxy(NPCData data)`:
     - Criar um archetype “light” de NPC no client (talvez reutilizar `GameArchetypes.NPCCharacter`, mas sem `AIControlled`).
     - Configurar `NetworkId`, `MapId`, `Position`, `Facing`, `Health`, e componentes necessários para renderização.
   - Chamar `RegisterSpatial(entity)` para o NPC (para queries espaciais client-side se usado).
   - `ClientVisualSyncSystem` deve criar um `NpcVisual` correspondente com base no `NetworkId`/entity.

2. Ao receber `NpcDespawnPacket`:
   - Encontrar entity via `NetworkId`.
   - Remover entidade do `World`.
   - `ClientVisualSyncSystem` deve destruir o `Node` correspondente.

3. Ao receber `NpcStatePacket`:
   - Para cada `NpcStateData`:
     - Encontrar entidade via `NetworkId`.
     - Atualizar componentes `Position`, `Facing`, `Health`.
     - O `ClientVisualSyncSystem` refletirá isso em `NpcVisual` (ou chamará diretamente `UpdateFromState`).

### 5.3. Registro de handlers de pacote no `GameClient`

Arquivo: `Simulation.Client/godot-client/Scripts/Simulation/GameClient.cs`

Trecho chave:

```csharp
private void RegisterPacketHandlers()
{
    if (_network is null)
    {
        GD.PushError("[GameClient] NetworkManager is null!");
        return;
    }

    _network.OnPeerDisconnected += OnPeerDisconnected;
    
    _network.RegisterPacketHandler<PlayerDataPacket>(HandlePlayerSpawn);
    _network.RegisterPacketHandler<LeftPacket>(HandlePlayerDespawn);
    _network.RegisterPacketHandler<PlayerStatePacket>(HandlePlayerState);
    _network.RegisterPacketHandler<PlayerVitalsPacket>(HandlePlayerVitals);
    _network.RegisterPacketHandler<CombatStatePacket>(HandleCombatState);
    _network.RegisterPacketHandler<ChatMessagePacket>(HandleChatMessage);
}
```

Tarefas:

1. Criar handlers:
   - `HandleNpcSpawn(NpcSpawnPacket packet)`
   - `HandleNpcDespawn(NpcDespawnPacket packet)`
   - `HandleNpcState(NpcStatePacket packet)`

2. Registrar:

```csharp
_network.RegisterPacketHandler<NpcSpawnPacket>(HandleNpcSpawn);
_network.RegisterPacketHandler<NpcDespawnPacket>(HandleNpcDespawn);
_network.RegisterPacketHandler<NpcStatePacket>(HandleNpcState);
```

3. Em cada handler, encaminhar os dados para a instância de `ClientGameSimulation`:
   - `ClientSimulation.CreateNpcFromSnapshot(...)`
   - `ClientSimulation.DespawnNpc(...)`
   - `ClientSimulation.ApplyNpcState(...)`

---

## 6. Consistência e segurança

6.1. **Autoridade do servidor**
- Nenhum input de NPC vem do client: client é apenas consumidor de snapshots.
- NPCs não devem ser influenciados por dados não autorizados (sem RPC do client para “controlar” NPC).

6.2. **NetworkSecurity**
- Integrar envio de pacotes de NPC no mesmo fluxo da política de `NetworkSecurity` (limites de mensagens por segundo já definidos em `Game.Server/Program.cs`).

6.3. **Performance**
- Otimizar `NpcStatePacket` para enviar somente o necessário:
  - Possível compressão de posição (ex.: int16 se o mapa permitir).
  - Agrupar várias entidades em um só pacote por tick.

---

## 7. Entregáveis esperados do especialista

1. Implementação completa de:
   - `NpcFactory.CreateNPC`, `BuildNPCSnapshot`, `BuildNpcStateSnapshot`.
   - `NpcAISystem` (primeira versão simples).
   - Opcional: `NpcSpawnService` com spawn inicial configurável.

2. Extensões no servidor:
   - Modificações em `ServerGameSimulation.ConfigureSystems`.
   - Extensões em `ServerSyncSystem` (ou sistema novo) para envio de pacotes de NPC.

3. Protocolo de rede:
   - Novos pacotes `NpcSpawnPacket`, `NpcDespawnPacket`, `NpcStatePacket` em `Game.Network.Packets.Game`.
   - Registro desses pacotes no `PacketProcessor`.

4. Cliente:
   - Implementação dos handlers `HandleNpcSpawn/Despawn/State` em `GameClient`.
   - Métodos na `ClientGameSimulation` para criar/destruir/atualizar NPCs.
   - Ajustes em `ClientVisualSyncSystem` para suportar `NpcVisual`.
   - Ajustes em `NpcVisual` (se necessário) para suportar updates parciais (`NpcStateData`).

5. Documentação técnica mínima:
   - Comentários XML nas novas classes/sistemas explicando:
     - Papel do `NpcAISystem`.
     - Estrutura de cada pacote de NPC.
     - Fluxo de dados: `ServerGameSimulation` → `ServerSyncSystem` → rede → `GameClient` → `ClientGameSimulation` → `NpcVisual`.