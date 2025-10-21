# Game ECS - Entity Component System

Um sistema Entity-Component-System (ECS) de alta performance para jogos, baseado em Arch ECS. Suporta uso tanto como **server** (simulação completa) quanto como **client** (previsão local + sincronização).

## Características

✅ **Arquitetura ECS** - Separação clara entre dados (componentes) e lógica (sistemas)
✅ **Timestep Fixo** - Simulação determinística com 60 ticks/segundo
✅ **Sincronização de Rede** - Sistema de dirty flags para sincronização eficiente
✅ **Client-Server** - Pode rodar como servidor (autoridade) ou cliente (previsão local)
✅ **Spatial Hashing** - Queries rápidas de proximidade
✅ **IA Integrada** - Sistemas de IA para NPCs
✅ **Combat System** - Lógica completa de combate, dano e morte
✅ **Event System** - Callbacks para eventos importantes do jogo

## Estrutura de Pastas

```
Game.ECS/
├── Components/
│   ├── Components.cs      # Definição de todos os componentes
│   ├── Flags.cs          # Flags de input e sincronização
│   └── Snapshots.cs      # Structs para rede (MemoryPack)
├── Systems/
│   ├── GameSystem.cs     # Base abstrata para sistemas
│   ├── MovementSystem.cs # Movimento de entidades
│   ├── HealthSystem.cs   # Regeneração de vida/mana
│   ├── CombatSystem.cs   # Combate e dano
│   ├── AISystem.cs       # IA para NPCs
│   ├── InputSystem.cs    # Processamento de input
│   ├── SyncSystem.cs     # Coleta de snapshots para rede
│   └── GameEventSystem.cs # Sistema de eventos
├── Entities/
│   ├── EntityFactory.cs   # Factory para criar entidades
│   ├── IEntityFactory.cs  # Interface de factory
│   ├── Archetypes/
│   │   └── GameArchetypes.cs # Blueprints de entidades
│   └── Data/
│       └── GameData.cs    # Structs de dados (PlayerCharacter, NPCCharacter, etc)
├── Services/
│   ├── IMapGrid.cs       # Interface para checar limites/bloqueios
│   ├── MapGrid.cs        # Implementação de grid de mapa
│   ├── IMapSpatial.cs    # Interface para spatial queries
│   ├── MapSpatial.cs     # Spatial hashing
│   ├── IMapService.cs    # Interface para gerenciar múltiplos mapas
│   └── MapService.cs     # Implementação de MapService
├── Utils/
│   ├── SimulationConfig.cs # Constantes de configuração
│   ├── MovementMath.cs     # Cálculos de movimento determinístico
│   └── NetworkDirtyExtensions.cs # Extensões para dirty flags
├── Examples/
│   ├── ServerGameSimulation.cs # Exemplo: usando como servidor
│   ├── ClientGameSimulation.cs # Exemplo: usando como cliente
│   └── ECSUsageExample.cs      # Exemplos de uso prático
└── GameSimulation.cs    # Base abstrata da simulação
```

## Componentes Principais

### Tags (Marcadores)
- `LocalPlayerTag` - Jogador controlado pela sessão atual
- `RemotePlayerTag` - Outro jogador
- `PlayerControlled` - Entidade controlada por jogador
- `AIControlled` - Entidade controlada por IA
- `Dead` - Entidade morta
- `Invulnerable` - Entidade invulnerável
- `Silenced` - Não pode usar habilidades

### Identity
- `PlayerId` - ID único do jogador
- `NetworkId` - ID para sincronização de rede

### Transform & Movement
- `Position` - Coordenadas (X, Y, Z)
- `Velocity` - Direção e velocidade
- `Facing` - Direção que a entidade está virada
- `Movement` - Acumulador de movimento
- `Walkable` - Velocidade de movimento
- `PreviousPosition` - Posição anterior (para reconciliação)

### Vitals
- `Health` - Vida (atual, máximo, regeneração)
- `Mana` - Mana (atual, máximo, regeneração)

### Combat
- `Attackable` - Capacidade de atacar
- `AttackPower` - Dano físico e mágico
- `Defense` - Defesa física e mágica
- `CombatState` - Em combate? Alvo? Cooldown?

### Status Effects
- `Stun` - Atordoado
- `Slow` - Desacelerado
- `Poison` - Envenenado
- `Burning` - Queimado

### Network
- `NetworkDirty` - Flags indicando quais dados precisam sincronizar
- `PlayerInput` - Input do jogador (X, Y, flags)

## Archetypes (Blueprints)

Um arquétipo é uma composição pré-definida de componentes:

- **PlayerCharacter** - Jogador completo (movimento, combate, input, vitals)
- **NPCCharacter** - NPC com IA (movimento, combate, vitals, sem input)
- **Projectile** - Projétil (apenas movimento e dano)
- **DroppedItem** - Item no chão (apenas posição)
- **InteractiveObject** - Objeto interativo (apenas posição)

## Uso

### Servidor

```csharp
var server = new ServerGameSimulation();

// Registra novo jogador
server.RegisterNewPlayer(playerId: 1, networkId: 1);

// Spawna NPC
server.SpawnNPC("Goblin", npcId: 100, x: 60, y: 50);

// Loop principal
while (true)
{
    server.Update(deltaTime);
    // Sincroniza com clientes via network
}
```

### Cliente

```csharp
var client = new ClientGameSimulation();

// Spawna jogador local
client.SpawnLocalPlayer(playerId: 1, networkId: 1);

// Input do jogador
client.HandlePlayerInput(inputX: 1, inputY: 0, flags: InputFlags.Sprint);

// Loop principal
while (true)
{
    client.Update(deltaTime);
    // Renderiza estado
}
```

## Sincronização de Rede

O sistema usa **dirty flags** para sincronizar apenas o que mudou:

```csharp
public enum SyncFlags : ulong
{
    Input    = 1 << 0,  // Input do jogador
    Movement = 1 << 1,  // Posição/velocidade
    Facing   = 1 << 2,  // Direção
    Vitals   = 1 << 3,  // HP/MP
    Combat   = 1 << 4,  // Estado de combate
    Status   = 1 << 5,  // Status effects
    All      = ...
}
```

O `SyncSystem` coleta snapshots de entidades dirty e invoca callbacks para o network layer:

```csharp
syncSystem.OnPlayerStateSnapshot += (snapshot) => 
{
    // Serializa e envia via UDP/TCP
};
```

## Combate

### Cálculo de Dano

```csharp
int damage = CombatSystem.CalculateDamage(attack, defense, isMagical: false);
// Dano = max(1, attackPower - defensePower) * variance (±20%)
```

### Aplicar Dano

```csharp
combatSystem.TryDamage(target, damage);
// Atualiza health, marca como dirty, dispara evento
```

### Cura

```csharp
combatSystem.TryHeal(target, amount, isHeal: true);
```

## Movimento Determinístico

Movimento por célula usando acumulador:

```csharp
public static bool Step(ref Position pos, ref Movement movement, in Velocity vel, float dt)
{
    movement.Timer += vel.Speed * dt;
    if (movement.Timer >= SimulationConfig.CellSize)
    {
        movement.Timer -= SimulationConfig.CellSize;
        pos.X += vel.DirectionX;
        pos.Y += vel.DirectionY;
        return true;
    }
    return false;
}
```

## IA para NPCs

O `AISystem` controla NPCs:

```csharp
aiSystem.TryAIAttack(npc, target);
aiSystem.StopAICombat(npc);
```

Comportamento padrão:
- Movimento aleatório
- Ataque se em combate
- Recalcula direção a cada 20% de chance por frame

## Sistema de Eventos

Eventos centralizados para hooks importantes:

```csharp
eventSystem.OnPlayerJoined += (networkId) => Console.WriteLine($"Player {networkId} joined");
eventSystem.OnDeath += (deadEntity, killer) => Console.WriteLine($"Entity died");
eventSystem.OnCombatEnter += (entity) => Console.WriteLine($"Combat started");
eventSystem.OnPositionChanged += (entity, x, y) => Console.WriteLine($"Moved to {x}, {y}");
```

## Spatial Queries

```csharp
var spatial = mapService.GetMapSpatial(mapId);

// Query um ponto
spatial.QueryAt(position, resultBuffer);

// Query área
spatial.QueryArea(min, max, resultBuffer);

// ForEach
spatial.ForEachAt(position, (entity) => {
    // Processa entity
    return true; // continue
});
```

## Configuração

Em `SimulationConfig.cs`:

```csharp
public const int TicksPerSecond = 60;        // 60 ticks/segundo
public const float TickDelta = 1f / 60f;     // 16.66ms por tick
public const float CellSize = 1f;            // Tamanho de célula
public const float ReconciliationThreshold = 1f; // Limiar de reconciliação
```

## Performance

- **Chunking**: Entidades são agrupadas em chunks para cache efficiency
- **Query Optimization**: Systems iterem apenas sobre entidades relevantes
- **Dirty Flags**: Sincronização otimizada (apenas dados que mudaram)
- **Spatial Hashing**: O(1) lookup de entidades por posição

## Extensão

### Adicionar novo componente

```csharp
// Components.cs
public struct Invisibility { public float RemainingTime; }

// Atualizar archetypes conforme necessário
```

### Adicionar novo sistema

```csharp
public sealed partial class InvisibilitySystem(World world) : GameSystem(world)
{
    [Query]
    [All<Invisibility>]
    private void ProcessInvisibility(in Entity e, ref Invisibility inv, [Data] float dt)
    {
        inv.RemainingTime -= dt;
        if (inv.RemainingTime <= 0)
        {
            World.Remove<Invisibility>(e);
        }
    }
}

// Adicionar ao ConfigureSystems()
group.Add(new InvisibilitySystem(world));
```

## Client vs Server

### Server (Autoridade)

```
Input → Movement → Combat → AI → Sync → Network
```

- Executa TODOS os sistemas
- Fonte de verdade para estado do jogo
- Valida input de clientes
- Envia estado para sincronizar clientes

### Client (Previsão Local)

```
Input → Movement (Local Prediction) → Sync (Server Reconciliation)
```

- Executa apenas movimento e input
- Faz previsão local para responsividade
- Reconcilia quando servidor discorda
- Recebe estado de outras entidades (NPCs, outros jogadores)

## Reconciliação

Cliente prediz movimento localmente mas aceita posição do servidor:

```csharp
// Cliente: predição local
client.HandlePlayerInput(1, 0, 0);  // Move para direita
// Posição local: 51, 50

// Servidor: posição autorizada
// → (51, 50)

// Se servidor discordar:
// → (50, 50) - Cliente reconcilia para posição do servidor
```

## Benchmarks Aproximados

- Spawn 1000 entidades: ~10ms
- Update 1000 entidades com movimento: ~1ms
- Combate 100 entidades: ~2ms
- Spatial query 1000 entities: <1ms

(Varia com CPU, apenas referência)

## Roadmap

- [ ] Habilidades e cooldowns
- [ ] Efeitos visuais (partículas)
- [ ] Sistema de loot
- [ ] Inventário
- [ ] Skills e talentos
- [ ] PvP arena
- [ ] Guild system
- [ ] Persistência (banco de dados)

## Licença

Parte do projeto GameSimulation.

## Referências

- [Arch ECS Documentation](https://github.com/genaray/Arch)
- [Entity Component System Pattern](https://en.wikipedia.org/wiki/Entity_component_system)
- [Networking Best Practices](https://www.gabrielgambetta.com/client-side-prediction-server-reconciliation.html)
