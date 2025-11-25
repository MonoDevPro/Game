# Análise Completa: Sistema de Pathfinding A* para NPCs

## 📋 Resumo do Problema Atual

Atualmente, o sistema de movimento de NPCs (`NpcMovementSystem`) usa uma abordagem **simplista de direção direta** - ele simplesmente calcula a direção do NPC para o alvo e tenta se mover em linha reta:

```csharp
// Código atual - movimento direto sem pathfinding
(desiredX, desiredY) = PositionLogic.GetDirectionTowards(in position, target.LastKnownPosition);
```

### Problemas com a abordagem atual:
1. **NPCs ficam presos em obstáculos** - não conseguem contornar paredes
2. **Movimento ineficiente** - não encontra o caminho mais curto
3. **Comportamento não realista** - NPCs colidem repetidamente com bloqueios
4. **Sem consideração de entidades dinâmicas** - não evita outros NPCs/players no caminho

---

## 🔍 Infraestrutura Existente

### 1. **MapGrid** (`Game.ECS/Services/MapGrid.cs`)
Já possui métodos essenciais para pathfinding:
```csharp
public interface IMapGrid
{
    bool InBounds(SpatialPosition spatialPosition);
    bool IsBlocked(SpatialPosition spatialPosition);      // ✅ Útil para A*
    bool AnyBlockedInArea(SpatialPosition min, SpatialPosition max);
    int CountBlockedInArea(SpatialPosition min, SpatialPosition max);
}
```

### 2. **MapSpatial** (`Game.ECS/Services/IMapSpatial.cs`)
Permite verificar entidades dinâmicas:
```csharp
public interface IMapSpatial
{
    bool TryGetFirstAt(SpatialPosition position, out Entity entity); // ✅ Para evitar colisões
    int QueryArea(SpatialPosition min, SpatialPosition max, Span<Entity> results);
}
```

### 3. **Componentes de IA** (`Game.ECS/Components/NpcAI.cs`)
- `NpcPatrol` - já tem `HomePosition` e `Destination`
- `NpcAIState` - estados `Chasing`, `Returning`, `Patrolling`
- `NpcBehavior` - `LeashRange`, `PatrolRadius`

---

## 📝 Solicitação para Especialista

### **Título: Implementar Sistema de Pathfinding A* para NPCs com Foco em Performance**

---

### **Contexto e Objetivo**

Implementar um sistema de pathfinding tile-based usando algoritmo A* otimizado para alta performance em servidor de jogo com múltiplos NPCs simultâneos. O sistema deve:

- Permitir que NPCs contornem obstáculos de forma inteligente
- Minimizar alocações de memória (zero-alloc onde possível)
- Suportar recálculo incremental quando caminhos são bloqueados
- Integrar-se naturalmente com a arquitetura ECS existente (Arch)

---

### **Requisitos de Performance**

| Métrica | Target |
|---------|--------|
| NPCs simultâneos | 100+ por mapa |
| Tempo máximo de cálculo por path | < 1ms |
| Frequência de recálculo | A cada 0.5-1.0s (não todo frame) |
| Memória por NPC | < 1KB para cache de path |
| Alocações por frame | Zero (usar pools) |

---

### **Arquivos a Criar/Modificar**

#### **Game.ECS (Lógica Compartilhada)**

1. **Novo: `Game.ECS/Logic/Pathfinding/AStarPathfinder.cs`**
   - Implementação do algoritmo A* otimizado
   - Usar `ArrayPool<T>` para evitar alocações
   - Heurística: Manhattan Distance (tile-based)
   - Suporte a 4 direções (cardinal) e 8 direções (diagonal)

2. **Novo: `Game.ECS/Logic/Pathfinding/PathNode.cs`**
   ```csharp
   public struct PathNode
   {
       public Position Position;
       public int GCost;        // Custo do início até este nó
       public int HCost;        // Heurística até o destino
       public int FCost => GCost + HCost;
       public Position Parent;  // Para reconstruir o caminho
   }
   ```

3. **Novo: `Game.ECS/Components/NpcPath.cs`**
   ```csharp
   public struct NpcPath
   {
       public Position[] Waypoints;      // Caminho calculado (pool'd)
       public int CurrentIndex;          // Índice do waypoint atual
       public int WaypointCount;         // Quantos waypoints válidos
       public float RecalculateTimer;    // Timer para recálculo
       public bool NeedsRecalculation;   // Flag de recálculo
       public Position LastTargetPosition; // Para detectar mudanças
   }
   ```

4. **Modificar: `Game.ECS/Services/IMapGrid.cs`**
   - Adicionar método para obter vizinhos válidos:
   ```csharp
   int GetWalkableNeighbors(SpatialPosition center, Span<SpatialPosition> neighbors);
   ```

#### **Game.Server (Sistemas de Servidor)**

5. **Novo: `Game.Server/ECS/Systems/NpcPathfindingSystem.cs`**
   - Responsável por calcular/atualizar caminhos
   - Executar ANTES do `NpcMovementSystem`
   - Limitar recálculos por frame (ex: máx 5 NPCs por tick)
   - Usar job/batch processing para múltiplos NPCs

6. **Modificar: `Game.Server/ECS/Systems/NpcMovementSystem.cs`**
   - Usar waypoints do `NpcPath` em vez de direção direta
   - Seguir waypoints sequencialmente
   - Solicitar recálculo quando bloqueado

7. **Novo: `Game.Server/Services/PathfindingService.cs`**
   - Gerenciar pools de memória
   - Cache de paths recentes (opcional)
   - Rate limiting de recálculos

---

### **Especificação Técnica do A***

#### **Algoritmo Base**
```
1. Inicializar open list com nó inicial
2. Loop enquanto open list não vazia:
   a. Pegar nó com menor F-cost
   b. Se é o destino, reconstruir caminho
   c. Para cada vizinho walkable:
      - Calcular G-cost tentativo
      - Se menor que anterior, atualizar
      - Adicionar à open list se não presente
3. Caminho não encontrado
```

#### **Otimizações Obrigatórias**

1. **Min-Heap para Open List**
   - Usar `PriorityQueue<PathNode, int>` do .NET 6+
   - Ou implementar binary heap custom para zero-alloc

2. **HashSet para Closed List**
   - Usar `HashSet<Position>` com capacity pré-alocada
   - Considerar `Dictionary<Position, PathNode>` para lookup O(1)

3. **Object Pooling**
   ```csharp
   // Pool de arrays para waypoints
   private static readonly ArrayPool<Position> WaypointPool = ArrayPool<Position>.Shared;
   
   // Pool de nós para A*
   private readonly ObjectPool<Dictionary<Position, PathNode>> _nodePool;
   ```

4. **Early Exit Conditions**
   - Se destino está a <= 1 tile, não calcular path
   - Se destino é bloqueado, abortar imediatamente
   - Limite máximo de nós expandidos (ex: 500)

5. **Path Smoothing (Opcional)**
   - Remover waypoints intermediários em linha reta
   - Reduz quantidade de waypoints a seguir

---

### **Integração com Sistema Existente**

#### **Fluxo por Tick**
```
1. NpcPerceptionSystem     → Detecta alvo
2. NpcAISystem            → Muda estado para Chasing/Returning
3. NpcPathfindingSystem   → Calcula/atualiza caminho (NOVO)
4. NpcMovementSystem      → Segue waypoints (MODIFICADO)
5. MovementSystem         → Aplica movimento físico
6. SpatialSyncSystem      → Sincroniza posições
```

#### **Condições de Recálculo**
- Alvo moveu mais de N tiles desde último cálculo
- Timer de recálculo expirou
- NPC ficou bloqueado por obstáculo dinâmico
- Caminho atual ficou inválido (obstáculo adicionado)

---

### **Componente NpcPath - Ciclo de Vida**

```csharp
// Adicionar ao archetype do NPC em GameArchetypes.cs
Component<NpcPath>.ComponentType,

// Factory: inicializar com pool
new NpcPath 
{
    Waypoints = WaypointPool.Rent(MaxWaypoints),
    CurrentIndex = 0,
    WaypointCount = 0,
    RecalculateTimer = 0f,
    NeedsRecalculation = true,
    LastTargetPosition = default
}
```

---

### **Critérios de Aceitação**

- [ ] NPCs contornam obstáculos estáticos (paredes)
- [ ] NPCs evitam outros NPCs/players (opcional: considera bloqueio dinâmico)
- [ ] Pathfinding executa em < 1ms por NPC
- [ ] Zero alocações em steady-state (após warmup)
- [ ] Limite de recálculos por frame (throttling)
- [ ] NPCs seguem waypoints suavemente
- [ ] Recálculo automático quando alvo se move significativamente
- [ ] Fallback para movimento direto se path não encontrado
- [ ] Path máximo de ~50 tiles (limite para LeashRange)
- [ ] Funciona corretamente com mapas multi-layer (floor)

---

### **Benchmarks Sugeridos**

Criar testes de benchmark com:
```csharp
[Benchmark]
public void Pathfind_ShortPath_10Tiles() { /* ... */ }

[Benchmark]
public void Pathfind_LongPath_50Tiles() { /* ... */ }

[Benchmark]
public void Pathfind_Blocked_NoPath() { /* ... */ }

[Benchmark]
public void Pathfind_100NPCs_SingleFrame() { /* ... */ }
```

---

### **Referências de Implementação**

- Roy-T AStar (MIT) - Referência para implementação eficiente
- .NET `PriorityQueue<T>` - Para min-heap nativo
- `ArrayPool<T>.Shared` - Para pooling de arrays
- Jump Point Search (JPS) - Otimização futura para grids uniformes

---

### **Prioridade de Implementação**

1. ⭐ **Fase 1**: A* básico funcionando (sem otimizações extremas)
2. ⭐ **Fase 2**: Object pooling e zero-alloc
3. ⭐ **Fase 3**: Rate limiting e batch processing
4. ⭐ **Fase 4**: Path smoothing e cache
5. ⭐ **Fase 5**: Consideração de entidades dinâmicas