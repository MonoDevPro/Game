# ✅ Checklist de Implementação - Game.ECS

Acompanhe o progresso da refatoração e melhorias.

---

## 🔥 SEMANA 1: Quick Wins (Alta Prioridade)

### Quick Win #1: MapService Integration ⏱️ 4h
**Arquivo:** `Game.ECS/Systems/MovementSystem.cs`

- [ ] Adicionar `IMapService` ao construtor do MovementSystem
- [ ] Modificar método `TryStep` para validar colisões
  - [ ] Validação de limites (InBounds)
  - [ ] Validação de células bloqueadas (IsBlocked)
  - [ ] Validação de colisão com outras entidades
  - [ ] Atualizar spatial hash
- [ ] Atualizar query `ProcessMovement` para passar Entity
- [ ] Atualizar `ServerGameSimulation` para injetar MapService
- [ ] Atualizar `ClientGameSimulation` para injetar MapService
- [ ] Teste manual: entidade para na frente de parede
- [ ] Teste manual: entidade não sai do mapa
- [ ] Commit: "feat: integrate MapService in MovementSystem"

**Critério de Sucesso:**
- ✅ 0 entidades atravessam paredes
- ✅ 0 entidades saem do mapa
- ✅ Spatial hash atualizado corretamente

---

### Quick Win #2: Filtro [None<Dead>] ⏱️ 2h
**Arquivos:** Todos os sistemas

- [ ] `MovementSystem.cs`
  - [ ] Adicionar `[None<Dead>]` em `ProcessMovement`
  - [ ] Adicionar `[None<Dead>]` em `ProcessEntityFacing`
- [ ] `HealthSystem.cs`
  - [ ] Adicionar `[None<Dead>]` em `ProcessHealthRegeneration`
  - [ ] Adicionar `[None<Dead>]` em `ProcessManaRegeneration`
- [ ] `AISystem.cs`
  - [ ] Adicionar `[None<Dead>]` em `ProcessAIMovement`
  - [ ] Adicionar `[None<Dead>]` em `ProcessAICombat`
- [ ] `InputSystem.cs`
  - [ ] Adicionar `[None<Dead>]` em `ProcessPlayerInput`
- [ ] `CombatSystem.cs`
  - [ ] Adicionar `[None<Dead>]` em queries relevantes
- [ ] Teste manual: entidade morta não processa ações
- [ ] Benchmark: medir redução de iterações
- [ ] Commit: "perf: add [None<Dead>] filter to all queries"

**Critério de Sucesso:**
- ✅ 5-10% menos iterações medidas
- ✅ Entidades mortas não executam lógica

---

### Quick Win #3: AI Decision Timer ⏱️ 3h
**Arquivos:** `Components.cs`, `GameArchetypes.cs`, `EntityFactory.cs`, `AISystem.cs`

- [ ] Criar componente `AIState` em `Components.cs`
  - [ ] Campo `DecisionCooldown`
  - [ ] Enum `AIBehavior`
  - [ ] Campos adicionais (target, patrol, etc)
- [ ] Adicionar `AIState` ao arquétipo `NPCCharacter`
- [ ] Inicializar `AIState` no `EntityFactory.CreateNPC`
- [ ] Refatorar `AISystem`
  - [ ] Query para incluir `AIState`
  - [ ] Decrementar cooldown
  - [ ] Executar lógica apenas quando cooldown <= 0
  - [ ] Implementar comportamentos (Idle, Wander, Patrol)
- [ ] Remover código antigo de Random por frame
- [ ] Benchmark: CPU antes/depois
- [ ] Commit: "perf: implement AI decision timer"

**Critério de Sucesso:**
- ✅ AI CPU < 5% (antes: ~30%)
- ✅ ~98% redução de checks aleatórios
- ✅ Comportamento AI permanece natural

---

### Quick Win #4: Combat Range Validation ⏱️ 3h
**Arquivos:** `Components.cs`, `SimulationConfig.cs`, `CombatSystem.cs`

- [ ] Adicionar helper `ManhattanDistance` em `Position`
- [ ] Adicionar constante `MaxAttackRange` em `SimulationConfig`
- [ ] Refatorar `TryDamage` → `TryAttack`
  - [ ] Validar posições dos componentes
  - [ ] Calcular distância
  - [ ] Retornar false se distância > max
  - [ ] Validar cooldown
  - [ ] Validar target não morto/invulnerável
- [ ] Separar `ApplyDamageInternal` da validação
- [ ] Teste manual: ataque de longa distância falha
- [ ] Teste manual: ataque corpo-a-corpo funciona
- [ ] Commit: "feat: add range validation to combat"

**Critério de Sucesso:**
- ✅ Ataques fora de range são rejeitados
- ✅ Ataques em range funcionam normalmente
- ✅ Performance mantida

---

### Quick Win #5: DirtyFlags ⏱️ 3h
**Arquivos:** `Components.cs`, `GameArchetypes.cs`, `MovementSystem.cs`, `CombatSystem.cs`, `Systems/SyncSystem.cs` (novo)

- [ ] Criar componente `DirtyFlags`
  - [ ] Campo `ushort Flags`
  - [ ] Métodos: MarkDirty, ClearDirty, IsDirty, ClearAll
  - [ ] Enum `ComponentType`
- [ ] Adicionar `DirtyFlags` aos arquétipos relevantes
- [ ] Marcar dirty em sistemas
  - [ ] MovementSystem marca Position
  - [ ] CombatSystem marca Health
  - [ ] HealthSystem marca Health/Mana
- [ ] Criar `SyncSystem`
  - [ ] Query `[All<NetworkId, DirtyFlags>]`
  - [ ] Sincronizar apenas componentes dirty
  - [ ] Limpar flags após sync
- [ ] Adicionar SyncSystem aos Examples
- [ ] Teste: verificar apenas dirty são enviados
- [ ] Commit: "feat: implement dirty flag synchronization"

**Critério de Sucesso:**
- ✅ Apenas componentes modificados sincronizados
- ✅ 70-80% redução de bandwidth medida
- ✅ Sem overhead perceptível

---

## 📊 SEMANAS 2-3: Fundação

### Reorganização de Pastas ⏱️ 16h

- [ ] Planejar estrutura nova (ver `REFACTORING_ARCHITECTURE.md`)
- [ ] Criar pastas base
  - [ ] `Core/` (Components, Systems, Events, Services)
  - [ ] `Gameplay/` (Components, Systems, Events)
  - [ ] `Networking/` (Components, Systems, Events)
  - [ ] `Spatial/` (Interfaces, Implementation)
  - [ ] `Telemetry/`
- [ ] Migrar componentes
  - [ ] Core/Components/ (Identity, Transform, Vitals, Tags)
  - [ ] Gameplay/Components/ (Combat, AI, StatusEffects)
  - [ ] Networking/Components/ (Network, Snapshots, Prediction)
- [ ] Migrar sistemas
  - [ ] Por domínio
  - [ ] Atualizar namespaces
- [ ] Migrar eventos
  - [ ] Separar por domínio
  - [ ] Manter compatibilidade com deprecated
- [ ] Atualizar Examples
- [ ] Atualizar testes
- [ ] Documentar migração
- [ ] Commit: "refactor: reorganize folder structure"

---

### Refatoração de Events ⏱️ 8h

- [ ] Criar `Core/Events/LifecycleEvents.cs`
- [ ] Criar `Core/Events/MovementEvents.cs`
- [ ] Criar `Gameplay/Events/CombatEvents.cs`
- [ ] Criar `Networking/Events/InputEvents.cs`
- [ ] Criar agregador `GameEvents` com backwards compatibility
- [ ] Adicionar `[Obsolete]` em eventos antigos
- [ ] Migrar sistemas para novos eventos
- [ ] Atualizar Examples
- [ ] Commit: "refactor: split GameEventSystem by domain"

---

### Testes Unitários ⏱️ 24h

#### Components Tests (4h)
- [ ] `Core/Components/PositionTests.cs`
  - [ ] ManhattanDistance
  - [ ] EuclideanDistance
  - [ ] Add
  - [ ] Equals
- [ ] `Core/Components/VelocityTests.cs`
- [ ] `Gameplay/Components/AIStateTests.cs`

#### Systems Tests (16h)
- [ ] `Core/Systems/MovementSystemTests.cs`
  - [ ] Should move when path is clear
  - [ ] Should not move when cell is blocked
  - [ ] Should not move out of bounds
  - [ ] Should update spatial hash
- [ ] `Gameplay/Systems/CombatSystemTests.cs`
  - [ ] Should attack when in range
  - [ ] Should not attack when out of range
  - [ ] Should apply damage correctly
  - [ ] Should mark entity dead when HP = 0
  - [ ] Should respect cooldown
- [ ] `Gameplay/Systems/HealthSystemTests.cs`
  - [ ] Should regenerate health
  - [ ] Should stop at max health
  - [ ] Should not regenerate when dead
- [ ] `Gameplay/Systems/AISystemTests.cs`
  - [ ] Should respect decision cooldown
  - [ ] Should execute behaviors

#### Integration Tests (4h)
- [ ] `Integration/FullSimulationTests.cs`
  - [ ] Should spawn and move player
  - [ ] Should handle combat interaction
  - [ ] Should synchronize state

---

### Telemetria & Profiling ⏱️ 16h

- [ ] Criar `Telemetry/ECSMetrics.cs`
  - [ ] Entity count metrics
  - [ ] System update time histogram
  - [ ] Event count metrics
  - [ ] Observable gauges
- [ ] Criar `Telemetry/PerformanceProfiler.cs`
  - [ ] RecordFrame
  - [ ] GetSnapshot (avg, min, max, percentiles)
  - [ ] PrintReport
- [ ] Integrar no `GameSimulation`
  - [ ] Medir cada sistema
  - [ ] Print report periodicamente
- [ ] Criar dashboard básico (console)
- [ ] Documentar métricas disponíveis
- [ ] Commit: "feat: add telemetry and profiling"

---

### Otimização HealthSystem ⏱️ 2h

- [ ] Criar tag `NeedsHealthRegeneration`
- [ ] Criar tag `NeedsManaRegeneration`
- [ ] Adicionar filtro `[All<NeedsHealthRegeneration>]` na query
- [ ] Adicionar flag quando receber dano
- [ ] Remover flag quando HP cheio
- [ ] Repetir para Mana
- [ ] Benchmark: antes/depois
- [ ] Commit: "perf: optimize health regen with flags"

**Critério de Sucesso:**
- ✅ 70-90% menos iterações
- ✅ Lógica permanece correta

---

## 🏗️ MÊS 2+: Features Avançadas

### Position Helpers ⏱️ 8h
- [ ] ManhattanDistance
- [ ] EuclideanDistance
- [ ] Add/Subtract
- [ ] Equals
- [ ] ToString
- [ ] Testes unitários
- [ ] Commit: "feat: add Position helper methods"

### Refatorar AbilityCooldown ⏱️ 8h
- [ ] Remover array
- [ ] Campos individuais (Ability1-4)
- [ ] Método TickAll
- [ ] Método CanUse
- [ ] Atualizar usos
- [ ] Testes
- [ ] Commit: "refactor: replace array cooldowns with fields"

### ReconciliationSystem ⏱️ 40h
- [ ] Design do sistema
- [ ] Client prediction components
- [ ] Input buffering
- [ ] Server snapshots
- [ ] Reconciliation logic
- [ ] Replay de inputs
- [ ] Testes extensivos
- [ ] Documentação
- [ ] Commit: "feat: implement client-server reconciliation"

### Multi-Map Support ⏱️ 16h
- [ ] Criar componente `MapId`
- [ ] Atualizar sistemas para usar MapId
- [ ] MapService gerencia múltiplos mapas
- [ ] Factory suporta MapId
- [ ] Testes de transição entre mapas
- [ ] Commit: "feat: add multi-map support"

### Sistema de Habilidades ⏱️ 80h
- [ ] Arquitetura de abilities
- [ ] Ability components
- [ ] Cooldown management
- [ ] Cost validation (mana, etc)
- [ ] Effect system
- [ ] AoE implementation
- [ ] Targeting system
- [ ] Testes
- [ ] Documentação completa
- [ ] Commit: "feat: implement ability system"

---

## 📈 Métricas de Sucesso

### Performance
- [ ] 60 FPS constante com 1000 entidades
- [ ] < 5ms por sistema
- [ ] < 100KB/s bandwidth por player
- [ ] AI CPU < 5%

### Code Quality
- [ ] 80%+ test coverage
- [ ] 0 dead code
- [ ] Estrutura de pastas clara
- [ ] Documentação atualizada

### Funcionalidade
- [ ] 0 bugs de colisão
- [ ] Sincronização eficiente
- [ ] AI natural e performante
- [ ] Combat balanceado

---

## 🎯 Status Geral

```
Quick Wins:        [ ] 0/5 completos
Fundação:          [ ] 0/5 completos
Features Avançadas: [ ] 0/5 completos

Progress Total: ░░░░░░░░░░ 0%
```

---

## 📝 Notas & Bloqueios

_Use este espaço para anotar bloqueios, decisões técnicas, ou descobertas durante implementação_

### Bloqueios
- 

### Decisões Técnicas
- 

### Descobertas
- 

---

**Última atualização:** 21/10/2025  
**Status:** 🚀 PRONTO PARA COMEÇAR
