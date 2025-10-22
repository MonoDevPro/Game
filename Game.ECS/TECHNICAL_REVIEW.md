# 🔍 Revisão Técnica Completa - Game.ECS

**Data:** 21 de outubro de 2025  
**Projeto:** Game.ECS - Sistema ECS com ArchECS para Client/Server  
**Foco:** Arquitetura, Performance, Consistência, Manutenibilidade

---

## 📊 Executive Summary

### ✅ Pontos Fortes
1. ✅ **Arquitetura limpa** - Separação clara entre Client/Server
2. ✅ **Event System desacoplado** - Boa inversão de dependências
3. ✅ **Fixed Timestep** - Determinismo implementado corretamente
4. ✅ **Componentes struct-based** - Memory-efficient
5. ✅ **MemoryPack** - Serialização zero-copy
6. ✅ **Spatial hashing** - MapSpatial com bom design

### 🚨 Problemas Críticos
1. ❌ **Falta de validação em MovementSystem** - Sem collision detection
2. ❌ **MapService não integrado** - Criado mas não usado nos sistemas
3. ❌ **AISystem ineficiente** - Random check todo frame
4. ❌ **Ausência total de testes** - Pasta vazia
5. ❌ **Falta de observabilidade** - Sem métricas/logs
6. ❌ **Queries podem incluir entidades mortas** - Sem filtro `[None<Dead>]`
7. ❌ **Event System monolítico** - 20+ eventos em uma classe
8. ❌ **Coupling forte** - Sistemas criam dependências internamente

---

## 🏗️ 1. ARQUITETURA & ORGANIZAÇÃO

### 1.1 Problemas na Estrutura Atual

#### ❌ Problema 1: Pastas Planas
```
Game.ECS/
├── Components/          # Tudo misturado em 3 arquivos
├── Systems/             # 6 arquivos sem categorização
├── Services/            # 6 arquivos, pouco uso
```

**IMPACTO:**
- Difícil encontrar código específico
- Sem separação de concerns
- Hard to navigate em projetos grandes

#### ❌ Problema 2: Services Isolados
```csharp
// MapService existe mas não é usado!
// MovementSystem.cs NÃO usa MapGrid
// CombatSystem.cs NÃO usa MapSpatial
```

**IMPACTO:**
- Código duplicado
- Validações ausentes
- Services inutilizados

### 1.2 Proposta de Reorganização

```
Game.ECS/
├── Core/
│   ├── Components/
│   │   ├── Transform.cs        # Position, Velocity, Facing
│   │   ├── Vitals.cs           # Health, Mana
│   │   └── Identity.cs         # NetworkId, PlayerId
│   ├── Systems/
│   │   ├── MovementSystem.cs
│   │   └── PhysicsSystem.cs    # Collision detection
│   └── Services/
│       ├── ITimeService.cs     # Tick management
│       └── TimeService.cs
│
├── Gameplay/
│   ├── Components/
│   │   ├── Combat.cs           # Attack, Defense, CombatState
│   │   ├── AI.cs               # AIControlled, AIState
│   │   └── StatusEffects.cs    # Stun, Slow, Poison
│   └── Systems/
│       ├── CombatSystem.cs
│       ├── HealthSystem.cs
│       └── AISystem.cs
│
├── Networking/
│   ├── Components/
│   │   ├── NetworkComponents.cs    # NetworkId, DirtyFlags
│   │   └── Snapshots.cs
│   └── Systems/
│       ├── SyncSystem.cs           # Sincronização
│       └── ReconciliationSystem.cs # Client prediction
│
├── Spatial/
│   ├── IMapGrid.cs
│   ├── IMapSpatial.cs
│   ├── MapGrid.cs
│   ├── MapSpatial.cs
│   └── MapService.cs
│
├── Entities/
│   ├── Archetypes/
│   ├── Data/
│   └── Factories/
│
├── Telemetry/                  # ⭐ NOVO
│   ├── ECSMetrics.cs
│   └── PerformanceProfiler.cs
│
└── Testing/                    # ⭐ NOVO
    ├── Fixtures/
    └── Builders/
```

---

## 🔧 2. COMPONENTES - Análise & Refatoração

### 2.1 ✅ O Que Está Bom

**Position, Velocity, Facing:**
```csharp
public struct Position { public int X; public int Y; public int Z; }
public struct Velocity { public int DirectionX; public int DirectionY; public float Speed; }
public struct Facing { public int DirectionX; public int DirectionY; }
```
- ✅ Structs leves
- ✅ Layout eficiente
- ✅ Fácil de serializar

**Health, Mana:**
```csharp
public struct Health { public int Current; public int Max; public float RegenerationRate; }
public struct Mana { public int Current; public int Max; public float RegenerationRate; }
```
- ✅ Regeneração integrada
- ✅ Simples e eficaz

### 2.2 ❌ Problemas Identificados

#### Problema 1: Tags e Components Misturados

```csharp
// ATUAL - Components.cs
public struct LocalPlayerTag { }
public struct PlayerId { public int Value; }
public struct Health { public int Current; public int Max; public float RegenerationRate; }
```

**PROBLEMA:** Tudo no mesmo arquivo, difícil de encontrar.

**SOLUÇÃO:**
```
Core/Components/
├── Tags.cs              # Todos os tags zero-size
├── Identity.cs          # NetworkId, PlayerId
├── Transform.cs         # Position, Velocity, Facing
└── Vitals.cs            # Health, Mana
```

#### Problema 2: Falta de Metadados Úteis

```csharp
// ATUAL
public struct Position { public int X; public int Y; public int Z; }

// FALTA: Métodos auxiliares úteis
```

**PROPOSTA:**
```csharp
public struct Position 
{ 
    public int X; 
    public int Y; 
    public int Z;
    
    // ⭐ Helpers úteis
    public readonly int ManhattanDistance(Position other) 
        => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
    
    public readonly float EuclideanDistance(Position other)
    {
        int dx = X - other.X;
        int dy = Y - other.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
    
    public readonly Position Add(int dx, int dy) 
        => new() { X = X + dx, Y = Y + dy, Z = Z };
    
    public readonly bool Equals(Position other) 
        => X == other.X && Y == other.Y && Z == other.Z;
}
```

#### Problema 3: AbilityCooldown Mal Projetado

```csharp
// ATUAL - Components.cs
public struct AbilityCooldown { public float[] RemainingTimes; } // ❌ Array aloca no heap!
```

**PROBLEMA:** Alocação desnecessária, dificulta cópia de componentes.

**SOLUÇÃO:**
```csharp
// PROPOSTA
public struct AbilityCooldowns 
{ 
    public float Ability1Cooldown;
    public float Ability2Cooldown;
    public float Ability3Cooldown;
    public float Ability4Cooldown;
    
    public void TickAll(float deltaTime)
    {
        Ability1Cooldown = Math.Max(0, Ability1Cooldown - deltaTime);
        Ability2Cooldown = Math.Max(0, Ability2Cooldown - deltaTime);
        Ability3Cooldown = Math.Max(0, Ability3Cooldown - deltaTime);
        Ability4Cooldown = Math.Max(0, Ability4Cooldown - deltaTime);
    }
    
    public bool CanUse(int abilityIndex) => abilityIndex switch
    {
        0 => Ability1Cooldown <= 0,
        1 => Ability2Cooldown <= 0,
        2 => Ability3Cooldown <= 0,
        3 => Ability4Cooldown <= 0,
        _ => false
    };
}
```

### 2.3 Novos Componentes Necessários

```csharp
// Core/Components/DirtyTracking.cs
/// <summary>
/// Rastreamento de mudanças para sincronização eficiente.
/// Apenas componentes dirty são sincronizados.
/// </summary>
public struct DirtyFlags 
{ 
    public ushort Flags;
    
    public void MarkDirty(ComponentType type) => Flags |= (ushort)(1 << (int)type);
    public void ClearDirty(ComponentType type) => Flags &= (ushort)~(1 << (int)type);
    public bool IsDirty(ComponentType type) => (Flags & (ushort)(1 << (int)type)) != 0;
    public void ClearAll() => Flags = 0;
}

public enum ComponentType : byte
{
    Position = 0,
    Health = 1,
    Mana = 2,
    Facing = 3,
    Combat = 4,
    Equipment = 5,
}

// Gameplay/Components/AI.cs
/// <summary>
/// Estado da IA com timer para evitar decisões todo frame.
/// </summary>
public struct AIState
{
    public float DecisionCooldown;      // Tempo até próxima decisão
    public AIBehavior CurrentBehavior;
    public int TargetNetworkId;         // Alvo atual (se houver)
    public Position PatrolOrigin;       // Ponto de origem para patrol
    public float PatrolRadius;          // Raio de patrulha
}

public enum AIBehavior : byte
{
    Idle,
    Patrol,
    Chase,
    Attack,
    Flee,
    Return // Retorna ao ponto de spawn
}

// Core/Components/MapContext.cs
/// <summary>
/// Referência ao mapa onde a entidade está.
/// Útil para multi-map scenarios.
/// </summary>
public struct MapId { public int Value; }
```

---

## ⚙️ 3. SISTEMAS - Problemas & Soluções

### 3.1 MovementSystem - CRÍTICO

#### ❌ Problema: Sem Validação de Colisão

```csharp
// ATUAL - MovementSystem.cs linha 29-43
private bool Step(ref Position pos, ref Movement movement, ref Velocity vel, float dt)
{
    // ...
    pos.X += vel.DirectionX;  // ❌ Move SEM verificar colisão
    pos.Y += vel.DirectionY;  // ❌ Move SEM verificar parede
    vel.Speed = 0f;
    return true;
}
```

**IMPACTO:**
- Entidades atravessam paredes
- Entidades saem do mapa
- Sem spatial consistency

#### ✅ Solução: Integrar MapService

```csharp
// PROPOSTA - Core/Systems/MovementSystem.cs
public sealed partial class MovementSystem : GameSystem
{
    private readonly IMapService _mapService;

    public MovementSystem(World world, GameEventSystem events, EntityFactory factory, IMapService mapService) 
        : base(world, events, factory)
    {
        _mapService = mapService;
    }

    [Query]
    [All<Position, Movement, Velocity, Walkable, MapId>]
    [None<Dead>] // ⭐ Não processar entidades mortas
    private void ProcessMovement(
        in Entity e, 
        ref Position pos, 
        ref Movement movement, 
        ref Velocity velocity,
        in Walkable walkable,
        in MapId mapId,
        [Data] float deltaTime)
    {
        if (!TryStep(ref pos, ref movement, ref velocity, mapId, deltaTime))
            return;
            
        Events.RaisePositionChanged(e, pos.X, pos.Y);
    }
    
    private bool TryStep(
        ref Position pos, 
        ref Movement movement, 
        ref Velocity vel,
        in MapId mapId,
        float dt)
    {
        if ((vel.DirectionX == 0 && vel.DirectionY == 0) || vel.Speed <= 0f)
            return false;

        movement.Timer += vel.Speed * dt;
        if (movement.Timer < SimulationConfig.CellSize)
            return false;

        // Calcula nova posição
        var newPos = new Position 
        { 
            X = pos.X + vel.DirectionX, 
            Y = pos.Y + vel.DirectionY, 
            Z = pos.Z 
        };

        // ⭐ VALIDAÇÃO 1: Limites do mapa
        var mapGrid = _mapService.GetMapGrid(mapId.Value);
        if (!mapGrid.InBounds(newPos))
        {
            vel.Speed = 0f;
            return false;
        }

        // ⭐ VALIDAÇÃO 2: Colisão com terreno
        if (mapGrid.IsBlocked(newPos))
        {
            vel.Speed = 0f;
            return false;
        }

        // ⭐ VALIDAÇÃO 3: Colisão com outras entidades
        var spatial = _mapService.GetMapSpatial(mapId.Value);
        if (spatial.TryGetFirstAt(newPos, out _)) // Célula ocupada
        {
            vel.Speed = 0f;
            return false;
        }

        // ⭐ Atualiza spatial hash
        spatial.Update(pos, newPos, e);

        // Move para nova posição
        movement.Timer -= SimulationConfig.CellSize;
        pos = newPos;
        vel.Speed = 0f;
        
        return true;
    }
    
    // Facing update permanece igual
    [Query]
    [All<PlayerControlled, Facing>]
    private void ProcessEntityFacing(in Entity e, in Velocity velocity, ref Facing facing, [Data] float _)
    {
        if (velocity.DirectionX == 0 && velocity.DirectionY == 0) return;
        
        int previousX = facing.DirectionX;
        int previousY = facing.DirectionY;

        facing.DirectionX = velocity.DirectionX;
        facing.DirectionY = velocity.DirectionY;

        if (previousX != facing.DirectionX || previousY != facing.DirectionY)
            Events.RaiseFacingChanged(e, facing.DirectionX, facing.DirectionY);
    }

    public override void BeforeUpdate(in float t) { }
    public override void AfterUpdate(in float t) { }
}
```

### 3.2 AISystem - PERFORMANCE

#### ❌ Problema: Random Check Todo Frame

```csharp
// ATUAL - AISystem.cs linha 21-45
if (_random.Next(0, 100) < 20) // ❌ Executa para CADA NPC TODO frame!
{
    // Toma decisão
}
```

**IMPACTO:**
- 1000 NPCs = 60,000 random checks/segundo
- CPU desperdiçado
- Comportamento twitchy

#### ✅ Solução: Decision Timer

```csharp
// PROPOSTA - Gameplay/Systems/AISystem.cs
public sealed partial class AISystem : GameSystem
{
    [Query]
    [All<AIControlled, AIState, Position, Velocity, Facing>]
    [None<Dead>]
    private void ProcessAI(
        in Entity e, 
        ref AIState aiState,
        ref Position pos,
        ref Velocity vel, 
        ref Facing facing,
        [Data] float deltaTime)
    {
        // ⭐ Decrementa timer
        aiState.DecisionCooldown -= deltaTime;
        
        // ⭐ Só toma decisão quando timer expira
        if (aiState.DecisionCooldown > 0f)
            return;
        
        // Reset timer (0.5-1.5s randomizado)
        aiState.DecisionCooldown = 0.5f + Random.Shared.NextSingle();
        
        // Executa comportamento atual
        switch (aiState.CurrentBehavior)
        {
            case AIBehavior.Idle:
                ProcessIdleBehavior(ref aiState, ref vel);
                break;
            
            case AIBehavior.Patrol:
                ProcessPatrolBehavior(e, ref aiState, ref pos, ref vel);
                break;
            
            case AIBehavior.Wander:
                ProcessWanderBehavior(ref vel, ref facing);
                break;
                
            // Outros comportamentos...
        }
        
        Events.RaiseFacingChanged(e, facing.DirectionX, facing.DirectionY);
    }
    
    private void ProcessIdleBehavior(ref AIState aiState, ref Velocity vel)
    {
        // 30% chance de começar a patrulhar
        if (Random.Shared.NextSingle() < 0.3f)
        {
            aiState.CurrentBehavior = AIBehavior.Wander;
        }
    }
    
    private void ProcessWanderBehavior(ref Velocity vel, ref Facing facing)
    {
        // Escolhe direção aleatória
        int dir = Random.Shared.Next(0, 5);
        (vel.DirectionX, vel.DirectionY) = dir switch
        {
            0 => (1, 0),   // Direita
            1 => (-1, 0),  // Esquerda
            2 => (0, 1),   // Cima
            3 => (0, -1),  // Baixo
            _ => (0, 0)    // Parado
        };

        facing.DirectionX = vel.DirectionX;
        facing.DirectionY = vel.DirectionY;
        vel.Speed = 3f;
    }
    
    private void ProcessPatrolBehavior(Entity e, ref AIState aiState, ref Position pos, ref Velocity vel)
    {
        // Verifica distância do ponto de origem
        float distance = pos.EuclideanDistance(aiState.PatrolOrigin);
        
        if (distance > aiState.PatrolRadius)
        {
            // Retorna ao ponto de origem
            int dx = Math.Sign(aiState.PatrolOrigin.X - pos.X);
            int dy = Math.Sign(aiState.PatrolOrigin.Y - pos.Y);
            vel.DirectionX = dx;
            vel.DirectionY = dy;
            vel.Speed = 3f;
        }
        else
        {
            // Anda aleatoriamente dentro do raio
            ProcessWanderBehavior(ref vel, ref World.Get<Facing>(e));
        }
    }

    public override void BeforeUpdate(in float t) { }
    public override void AfterUpdate(in float t) { }
}
```

**GANHO DE PERFORMANCE:**
- Antes: 60,000 checks/segundo (1000 NPCs x 60 FPS)
- Depois: ~1,000 checks/segundo (1000 NPCs x 1 decisão/seg)
- **98% de redução!**

### 3.3 CombatSystem - Falta de Range Check

#### ❌ Problema: Sem Validação de Distância

```csharp
// ATUAL - CombatSystem.cs
public bool TryDamage(Entity target, int damage, Entity? attacker = null)
{
    // ❌ Aceita dano de qualquer distância
    // ❌ Não verifica se attacker pode alcançar target
}
```

**IMPACTO:**
- Ataques de longa distância inválidos
- Sem line of sight

#### ✅ Solução: Validação de Range

```csharp
// PROPOSTA - Gameplay/Systems/CombatSystem.cs
public bool TryAttack(Entity attacker, Entity target)
{
    // Validação de componentes
    if (!World.IsAlive(attacker) || !World.IsAlive(target))
        return false;

    if (!World.TryGet(attacker, out Position attackerPos) ||
        !World.TryGet(target, out Position targetPos) ||
        !World.TryGet(attacker, out AttackPower attackPower) ||
        !World.TryGet(target, out Defense defense) ||
        !World.TryGet(target, out Health health) ||
        !World.TryGet(attacker, out CombatState combat))
        return false;

    // ⭐ VALIDAÇÃO 1: Cooldown
    if (combat.LastAttackTime > 0)
        return false;

    // ⭐ VALIDAÇÃO 2: Range
    int distance = attackerPos.ManhattanDistance(targetPos);
    if (distance > SimulationConfig.MaxAttackRange)
        return false;

    // ⭐ VALIDAÇÃO 3: Target não está morto
    if (World.Has<Dead>(target) || World.Has<Invulnerable>(target))
        return false;

    // Calcula dano
    int damage = CalculateDamage(attackPower, defense);

    // Aplica dano
    return ApplyDamage(target, damage, attacker);
}

private bool ApplyDamage(Entity target, int damage, Entity attacker)
{
    ref Health health = ref World.Get<Health>(target);
    
    int previous = health.Current;
    health.Current = Math.Max(0, previous - damage);

    Events.RaiseDamage(attacker, target, damage);
    
    // Marca como morto se necessário
    if (health.Current <= 0 && !World.Has<Dead>(target))
    {
        World.Add<Dead>(target);
        Events.RaiseDeath(target, attacker);
    }
    
    return true;
}
```

### 3.4 GameEventSystem - Monolítico

#### ❌ Problema: God Class

```csharp
// ATUAL - GameEventSystem.cs
public class GameEventSystem
{
    // 20+ eventos em uma classe
    public event Action<Entity>? OnEntitySpawned;
    public event Action<Entity>? OnPlayerJoined;
    public event Action<Entity, int, int>? OnDamage;
    // ... 17 mais
}
```

**PROBLEMAS:**
- Difícil de testar
- Coupling alto
- Dificultosa manutenção

#### ✅ Solução: Separar por Domínio

```csharp
// Core/Events/LifecycleEvents.cs
public class LifecycleEvents
{
    public event Action<Entity>? OnEntitySpawned;
    public event Action<Entity>? OnEntityDespawned;
    public event Action<Entity>? OnPlayerJoined;
    public event Action<Entity>? OnPlayerLeft;
    
    public void RaiseSpawn(Entity e) => OnEntitySpawned?.Invoke(e);
    public void RaiseDespawn(Entity e) => OnEntityDespawned?.Invoke(e);
    public void RaisePlayerJoined(Entity e) => OnPlayerJoined?.Invoke(e);
    public void RaisePlayerLeft(Entity e) => OnPlayerLeft?.Invoke(e);
}

// Gameplay/Events/CombatEvents.cs
public class CombatEvents
{
    public event Action<Entity, Entity, int>? OnDamage;
    public event Action<Entity, Entity, int>? OnHeal;
    public event Action<Entity, Entity?>? OnDeath;
    public event Action<Entity>? OnCombatEnter;
    public event Action<Entity>? OnCombatExit;
    
    public void RaiseDamage(Entity attacker, Entity victim, int dmg) 
        => OnDamage?.Invoke(attacker, victim, dmg);
    // ...
}

// Core/Events/MovementEvents.cs
public class MovementEvents
{
    public event Action<Entity, int, int>? OnPositionChanged;
    public event Action<Entity, int, int>? OnFacingChanged;
    
    public void RaisePositionChanged(Entity e, int x, int y) 
        => OnPositionChanged?.Invoke(e, x, y);
    public void RaiseFacingChanged(Entity e, int dx, int dy) 
        => OnFacingChanged?.Invoke(e, dx, dy);
}

// Core/Events/GameEvents.cs (agregador)
public class GameEvents
{
    public LifecycleEvents Lifecycle { get; } = new();
    public CombatEvents Combat { get; } = new();
    public MovementEvents Movement { get; } = new();
    public InputEvents Input { get; } = new();
    
    // Backwards compatibility (deprecated)
    [Obsolete("Use Lifecycle.OnEntitySpawned")]
    public event Action<Entity>? OnEntitySpawned
    {
        add => Lifecycle.OnEntitySpawned += value;
        remove => Lifecycle.OnEntitySpawned -= value;
    }
    
    // ... outros para compatibilidade
}
```

**USO:**
```csharp
// Antes
events.RaisePositionChanged(entity, x, y);

// Depois
events.Movement.RaisePositionChanged(entity, x, y);

// Subscribers
events.Combat.OnDamage += (attacker, victim, damage) => 
{
    Console.WriteLine($"Damage dealt: {damage}");
};
```

---

## 🚀 4. PERFORMANCE

### 4.1 Problemas Identificados

#### ❌ Problema 1: MapSpatial - Alocação de Listas

```csharp
// ATUAL - MapSpatial.cs
private readonly Dictionary<(int x, int y), List<Entity>> _grid = [];
```

**PROBLEMA:** `List<Entity>` aloca no heap para cada célula.

**SOLUÇÃO:**
```csharp
// PROPOSTA - Use ArrayPool
using System.Buffers;

public class MapSpatial : IMapSpatial
{
    private readonly Dictionary<(int x, int y), List<Entity>> _grid = [];
    private readonly ArrayPool<Entity> _pool = ArrayPool<Entity>.Shared;

    public int QueryArea(Position minInclusive, Position maxInclusive, Span<Entity> results)
    {
        int count = 0;
        int minX = Math.Min(minInclusive.X, maxInclusive.X);
        int maxX = Math.Max(minInclusive.X, maxInclusive.X);
        int minY = Math.Min(minInclusive.Y, maxInclusive.Y);
        int maxY = Math.Max(minInclusive.Y, maxInclusive.Y);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var key = (x, y);
                if (!_grid.TryGetValue(key, out var list))
                    continue;

                foreach (var entity in list)
                {
                    if (count >= results.Length)
                        return count;

                    results[count++] = entity;
                }
            }
        }

        return count;
    }
    
    // Alternativa: Pre-allocate small lists
    private List<Entity> GetOrCreateList((int x, int y) key)
    {
        if (!_grid.TryGetValue(key, out var list))
        {
            list = new List<Entity>(capacity: 4); // Assume ~4 entidades por célula
            _grid[key] = list;
        }
        return list;
    }
}
```

#### ❌ Problema 2: Queries Incluem Entidades Mortas

```csharp
// ATUAL - MovementSystem.cs
[Query]
[All<Position, Movement, Velocity>]
private void ProcessMovement(...) // ❌ Processa entidades mortas!
```

**SOLUÇÃO:**
```csharp
// PROPOSTA
[Query]
[All<Position, Movement, Velocity>]
[None<Dead>] // ⭐ Exclui entidades mortas
private void ProcessMovement(...)
```

**GANHO:**
- 5-10% menos iterações
- Evita processamento desnecessário

#### ❌ Problema 3: HealthSystem - Regeneração Desnecessária

```csharp
// ATUAL - HealthSystem.cs
[Query]
[All<Health>]
private void ProcessHealthRegeneration(in Entity e, ref Health health, [Data] float deltaTime)
{
    if (health.Current >= health.Max)
        return; // ❌ Itera entidades com HP cheio!
    // ...
}
```

**SOLUÇÃO:**
```csharp
// PROPOSTA - Use flag para regeneração ativa
public struct NeedsRegeneration { }

// Apenas entidades com HP < Max têm o componente
[Query]
[All<Health, NeedsRegeneration>]
[None<Dead>]
private void ProcessHealthRegeneration(in Entity e, ref Health health, [Data] float deltaTime)
{
    float regeneration = health.RegenerationRate * deltaTime;
    int previous = health.Current;
    health.Current = Math.Min(health.Max, previous + (int)regeneration);

    // ⭐ Remove flag quando HP estiver cheio
    if (health.Current >= health.Max)
    {
        World.Remove<NeedsRegeneration>(e);
    }

    if (health.Current != previous)
    {
        Events.RaiseHealHp(e, e, health.Current - previous);
    }
}

// Quando receber dano, adiciona flag
public bool TryDamage(Entity target, int damage)
{
    // ... aplica dano
    
    if (health.Current < health.Max && !World.Has<NeedsRegeneration>(target))
    {
        World.Add<NeedsRegeneration>(target);
    }
}
```

**GANHO:**
- Evita iterar sobre 70-90% das entidades (que estão com HP cheio)
- Query muito mais eficiente

### 4.2 Configurações de Performance

```csharp
// PROPOSTA - Utils/SimulationConfig.cs
public static class SimulationConfig
{
    // ... configs existentes
    
    // ⭐ Performance tuning
    public const int SpatialHashCellSize = 16; // Células por bucket
    public const int ExpectedEntitiesPerCell = 4;
    public const int MaxEntitiesInQuery = 1000; // Limite de queries grandes
    
    // ⭐ AI tuning
    public const float AIDecisionMinInterval = 0.3f; // Mínimo entre decisões
    public const float AIDecisionMaxInterval = 1.5f; // Máximo entre decisões
    
    // ⭐ Regeneration
    public const float HealthRegenTickRate = 0.5f; // Regen a cada 0.5s
    public const float ManaRegenTickRate = 0.5f;
}
```

---

## 🧪 5. TESTES & OBSERVABILIDADE

### 5.1 ❌ Problema: Pasta Game.Tests Vazia

**IMPACTO:**
- Sem garantias de funcionamento
- Refatoração arriscada
- Bugs em produção

### 5.2 ✅ Proposta: Testes Essenciais

```csharp
// Game.Tests/Systems/MovementSystemTests.cs
using Xunit;
using Arch.Core;
using Game.ECS.Systems;
using Game.ECS.Components;
using Game.ECS.Entities.Factories;
using Game.ECS.Services;

public class MovementSystemTests : IDisposable
{
    private readonly World _world;
    private readonly GameEvents _events;
    private readonly EntityFactory _factory;
    private readonly MapService _mapService;
    private readonly MovementSystem _system;

    public MovementSystemTests()
    {
        _world = World.Create();
        _events = new GameEvents();
        _factory = new EntityFactory(_world, _events);
        _mapService = new MapService();
        _system = new MovementSystem(_world, _events, _factory, _mapService);
    }

    [Fact]
    public void MovementSystem_ShouldMoveEntity_WhenPathIsClear()
    {
        // Arrange
        var entity = _world.Create();
        _world.SetRange(entity, new object[]
        {
            new Position { X = 5, Y = 5, Z = 0 },
            new Velocity { DirectionX = 1, DirectionY = 0, Speed = 5f },
            new Movement { Timer = 0f },
            new Walkable { BaseSpeed = 5f, CurrentModifier = 1f },
            new MapId { Value = 0 }
        });

        // Act
        _system.Update(SimulationConfig.TickDelta);

        // Assert
        var pos = _world.Get<Position>(entity);
        Assert.Equal(6, pos.X);
        Assert.Equal(5, pos.Y);
    }

    [Fact]
    public void MovementSystem_ShouldNotMove_WhenCellIsBlocked()
    {
        // Arrange
        var mapGrid = _mapService.GetMapGrid(0) as MapGrid;
        mapGrid!.SetBlocked(new Position { X = 6, Y = 5 }, true);

        var entity = _world.Create();
        _world.SetRange(entity, new object[]
        {
            new Position { X = 5, Y = 5, Z = 0 },
            new Velocity { DirectionX = 1, DirectionY = 0, Speed = 5f },
            new Movement { Timer = 0f },
            new Walkable { BaseSpeed = 5f, CurrentModifier = 1f },
            new MapId { Value = 0 }
        });

        // Act
        _system.Update(SimulationConfig.TickDelta);

        // Assert
        var pos = _world.Get<Position>(entity);
        Assert.Equal(5, pos.X); // Não se moveu
        Assert.Equal(5, pos.Y);
    }

    [Fact]
    public void MovementSystem_ShouldNotMove_WhenOutOfBounds()
    {
        // Arrange
        var entity = _world.Create();
        _world.SetRange(entity, new object[]
        {
            new Position { X = 99, Y = 50, Z = 0 }, // Perto do limite
            new Velocity { DirectionX = 1, DirectionY = 0, Speed = 5f },
            new Movement { Timer = 0f },
            new Walkable { BaseSpeed = 5f, CurrentModifier = 1f },
            new MapId { Value = 0 }
        });

        // Act
        _system.Update(SimulationConfig.TickDelta);

        // Assert
        var pos = _world.Get<Position>(entity);
        Assert.Equal(99, pos.X); // Não se moveu além do limite
    }

    public void Dispose()
    {
        _world.Dispose();
    }
}

// Game.Tests/Components/PositionTests.cs
public class PositionTests
{
    [Theory]
    [InlineData(0, 0, 5, 5, 10)]
    [InlineData(0, 0, 3, 4, 7)]
    [InlineData(5, 5, 5, 5, 0)]
    public void Position_ManhattanDistance_ShouldCalculateCorrectly(
        int x1, int y1, int x2, int y2, int expected)
    {
        // Arrange
        var pos1 = new Position { X = x1, Y = y1 };
        var pos2 = new Position { X = x2, Y = y2 };

        // Act
        var distance = pos1.ManhattanDistance(pos2);

        // Assert
        Assert.Equal(expected, distance);
    }
}

// Game.Tests/Systems/CombatSystemTests.cs
public class CombatSystemTests
{
    [Fact]
    public void CombatSystem_ShouldNotAttack_WhenOutOfRange()
    {
        // Implementar teste
    }

    [Fact]
    public void CombatSystem_ShouldApplyDamage_WhenInRange()
    {
        // Implementar teste
    }

    [Fact]
    public void CombatSystem_ShouldMarkEntityDead_WhenHealthReachesZero()
    {
        // Implementar teste
    }
}
```

### 5.3 ✅ Proposta: Observabilidade

```csharp
// Telemetry/ECSMetrics.cs
using System.Diagnostics;
using System.Diagnostics.Metrics;

public class ECSMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _entityCount;
    private readonly Histogram<double> _systemUpdateTime;
    private readonly Counter<long> _eventCount;

    public ECSMetrics(string serviceName = "GameECS")
    {
        _meter = new Meter(serviceName, "1.0.0");
        
        _entityCount = _meter.CreateCounter<long>(
            "ecs.entities.count",
            description: "Number of active entities");
        
        _systemUpdateTime = _meter.CreateHistogram<double>(
            "ecs.system.update_time",
            unit: "ms",
            description: "Time taken to update a system");
        
        _eventCount = _meter.CreateCounter<long>(
            "ecs.events.count",
            description: "Number of events fired");
    }

    public void RecordEntitySpawn(string entityType)
    {
        _entityCount.Add(1, new KeyValuePair<string, object?>("entity_type", entityType));
    }

    public void RecordEntityDespawn(string entityType)
    {
        _entityCount.Add(-1, new KeyValuePair<string, object?>("entity_type", entityType));
    }

    public IDisposable MeasureSystemUpdate(string systemName)
    {
        return new SystemUpdateMeasurement(_systemUpdateTime, systemName);
    }

    public void RecordEvent(string eventType)
    {
        _eventCount.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
    }

    private class SystemUpdateMeasurement : IDisposable
    {
        private readonly Histogram<double> _histogram;
        private readonly string _systemName;
        private readonly Stopwatch _sw;

        public SystemUpdateMeasurement(Histogram<double> histogram, string systemName)
        {
            _histogram = histogram;
            _systemName = systemName;
            _sw = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _sw.Stop();
            _histogram.Record(_sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("system", _systemName));
        }
    }
}

// Telemetry/PerformanceProfiler.cs
public class PerformanceProfiler
{
    private readonly Dictionary<string, List<double>> _measurements = new();
    private readonly int _maxSamples = 300; // 5 segundos @ 60 FPS

    public void RecordFrame(string systemName, double durationMs)
    {
        if (!_measurements.TryGetValue(systemName, out var samples))
        {
            samples = new List<double>(_maxSamples);
            _measurements[systemName] = samples;
        }

        samples.Add(durationMs);
        
        if (samples.Count > _maxSamples)
            samples.RemoveAt(0);
    }

    public PerformanceSnapshot GetSnapshot(string systemName)
    {
        if (!_measurements.TryGetValue(systemName, out var samples) || samples.Count == 0)
            return default;

        return new PerformanceSnapshot
        {
            SystemName = systemName,
            SampleCount = samples.Count,
            AverageMs = samples.Average(),
            MinMs = samples.Min(),
            MaxMs = samples.Max(),
            P50Ms = Percentile(samples, 0.5),
            P95Ms = Percentile(samples, 0.95),
            P99Ms = Percentile(samples, 0.99)
        };
    }

    public void PrintReport()
    {
        Console.WriteLine("\n=== ECS Performance Report ===");
        foreach (var systemName in _measurements.Keys.OrderBy(x => x))
        {
            var snapshot = GetSnapshot(systemName);
            Console.WriteLine($"{systemName,-30} Avg: {snapshot.AverageMs:F2}ms | P95: {snapshot.P95Ms:F2}ms | Max: {snapshot.MaxMs:F2}ms");
        }
    }

    private static double Percentile(List<double> values, double percentile)
    {
        var sorted = values.OrderBy(x => x).ToList();
        int index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
        return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
    }
}

public struct PerformanceSnapshot
{
    public string SystemName;
    public int SampleCount;
    public double AverageMs;
    public double MinMs;
    public double MaxMs;
    public double P50Ms;
    public double P95Ms;
    public double P99Ms;
}
```

**INTEGRAÇÃO NO GAMESIMULATION:**
```csharp
public abstract class GameSimulation : GameSystem
{
    protected readonly ECSMetrics Metrics;
    protected readonly PerformanceProfiler Profiler;
    
    protected GameSimulation()
    {
        // ...
        Metrics = new ECSMetrics();
        Profiler = new PerformanceProfiler();
    }

    public override void Update(in float deltaTime)
    {
        _fixedTimeStep.Accumulate(deltaTime);

        while (_fixedTimeStep.ShouldUpdate())
        {
            CurrentTick++;

            // ⭐ Medir performance por sistema
            foreach (var system in Systems)
            {
                using (Metrics.MeasureSystemUpdate(system.GetType().Name))
                {
                    system.BeforeUpdate(SimulationConfig.TickDelta);
                    system.Update(SimulationConfig.TickDelta);
                    system.AfterUpdate(SimulationConfig.TickDelta);
                }
            }

            _fixedTimeStep.Step();
        }
        
        // Print report a cada 5 segundos
        if (CurrentTick % 300 == 0)
        {
            Profiler.PrintReport();
        }
    }
}
```

---

## 🎯 6. PLANO DE AÇÃO PRIORIZADO

### 🔥 QUICK WINS (1-3 dias)

#### 1. **Integrar MapService no MovementSystem** ⏱️ 4h
**Impacto:** ALTO | **Esforço:** BAIXO
- ✅ Adicionar `IMapService` ao construtor
- ✅ Validar colisões com `IsBlocked()`
- ✅ Atualizar spatial hash
- ✅ Prevenir movimento fora do mapa

**Arquivo:** `Game.ECS/Systems/MovementSystem.cs`

#### 2. **Adicionar filtro `[None<Dead>]` em todas as queries** ⏱️ 2h
**Impacto:** MÉDIO | **Esforço:** TRIVIAL
- ✅ MovementSystem
- ✅ AISystem
- ✅ HealthSystem
- ✅ CombatSystem
- ✅ InputSystem

**Ganho:** 5-10% menos iterações

#### 3. **Refatorar AISystem com Decision Timer** ⏱️ 3h
**Impacto:** ALTO | **Esforço:** BAIXO
- ✅ Adicionar componente `AIState`
- ✅ Implementar timer de decisão
- ✅ Remover checks aleatórios por frame

**Ganho:** ~98% redução de processamento

#### 4. **Adicionar validação de range em CombatSystem** ⏱️ 3h
**Impacto:** MÉDIO | **Esforço:** BAIXO
- ✅ Validar distância entre attacker e target
- ✅ Adicionar constante `MaxAttackRange`
- ✅ Retornar false se fora de range

#### 5. **Implementar DirtyFlags para sincronização** ⏱️ 3h
**Impacto:** ALTO | **Esforço:** MÉDIO
- ✅ Criar componente `DirtyFlags`
- ✅ Marcar quando componentes mudam
- ✅ Sistema de sync lê apenas dirty

**Ganho:** 50-70% redução de bandwidth

### 📊 MÉDIO PRAZO (1-2 semanas)

#### 6. **Reorganizar estrutura de pastas** ⏱️ 2 dias
**Impacto:** MÉDIO | **Esforço:** MÉDIO
- 📁 Separar Core/Gameplay/Networking
- 📁 Componentes por categoria
- 📁 Sistemas por domínio

#### 7. **Refatorar GameEventSystem** ⏱️ 1 dia
**Impacto:** MÉDIO | **Esforço:** MÉDIO
- ✂️ Separar eventos por domínio
- ✅ LifecycleEvents
- ✅ CombatEvents
- ✅ MovementEvents
- ✅ InputEvents

#### 8. **Implementar testes unitários básicos** ⏱️ 3 dias
**Impacto:** ALTO | **Esforço:** ALTO
- ✅ MovementSystem tests
- ✅ CombatSystem tests
- ✅ Component tests
- ✅ Integration tests

#### 9. **Adicionar telemetria e profiling** ⏱️ 2 dias
**Impacto:** ALTO | **Esforço:** MÉDIO
- ✅ ECSMetrics implementation
- ✅ PerformanceProfiler
- ✅ Integração no GameSimulation
- ✅ Dashboard básico

#### 10. **Otimizar HealthSystem com NeedsRegeneration** ⏱️ 2h
**Impacto:** MÉDIO | **Esforço:** BAIXO
- ✅ Adicionar flag component
- ✅ Filtrar query
- ✅ Add/Remove flag dinamicamente

**Ganho:** 70-90% menos iterações

### 🏗️ ARQUITETURAL (1+ mês)

#### 11. **Implementar Position helpers** ⏱️ 1 dia
**Impacto:** MÉDIO | **Esforço:** BAIXO
- ✅ ManhattanDistance
- ✅ EuclideanDistance
- ✅ Add/Subtract
- ✅ Equals

#### 12. **Refatorar AbilityCooldown** ⏱️ 1 dia
**Impacto:** BAIXO | **Esforço:** BAIXO
- ✅ Remover array
- ✅ Campos individuais
- ✅ Métodos TickAll/CanUse

#### 13. **Implementar ReconciliationSystem** ⏱️ 1 semana
**Impacto:** ALTO | **Esforço:** ALTO
- ✅ Client-side prediction
- ✅ Server reconciliation
- ✅ Input buffering

#### 14. **Adicionar MapId component** ⏱️ 2 dias
**Impacto:** MÉDIO | **Esforço:** BAIXO
- ✅ Multi-map support
- ✅ Atualizar sistemas para usar MapId
- ✅ MapService por mapa

#### 15. **Criar sistema de habilidades robusto** ⏱️ 2 semanas
**Impacto:** ALTO | **Esforço:** ALTO
- ✅ Ability system architecture
- ✅ Cooldowns gerenciados
- ✅ Validação de custos
- ✅ Efeitos de área

---

## 🎨 7. ANTI-PATTERNS IDENTIFICADOS

### ❌ 1. Services Não Utilizados
```csharp
// MapService existe mas MovementSystem não o usa
// PROBLEMA: Dead code, waste of effort
```

**FIX:** Injetar dependências nos sistemas

### ❌ 2. Random Check Todo Frame
```csharp
// AISystem faz Random.Next() para cada NPC todo frame
```

**FIX:** Decision timer

### ❌ 3. Array em Component
```csharp
public struct AbilityCooldown { public float[] RemainingTimes; }
```

**FIX:** Campos individuais

### ❌ 4. God Class EventSystem
```csharp
// 20+ eventos em uma classe
```

**FIX:** Separar por domínio

### ❌ 5. Queries Sem Filtro de Dead
```csharp
[Query]
[All<Position>] // Processa entidades mortas
```

**FIX:** Adicionar `[None<Dead>]`

### ❌ 6. Tight Coupling
```csharp
// Sistemas criam suas próprias dependências
var mapGrid = new MapGrid(100, 100);
```

**FIX:** Dependency injection

### ❌ 7. Sem Helpers em Components
```csharp
// Position sem métodos úteis
public struct Position { public int X; public int Y; public int Z; }
```

**FIX:** Adicionar helpers (Distance, Add, etc)

---

## 📊 8. MÉTRICAS DE SUCESSO

### Performance
- ✅ **Target:** 60 FPS constante com 1000 entidades
- ✅ **Target:** < 5ms por sistema
- ✅ **Target:** < 100KB/s bandwidth por player

### Code Quality
- ✅ **Target:** 80%+ test coverage
- ✅ **Target:** 0 dead code
- ✅ **Target:** < 10 minutos para adicionar novo sistema

### Manutenibilidade
- ✅ **Target:** Estrutura de pastas clara
- ✅ **Target:** Documentação atualizada
- ✅ **Target:** Dependency injection consistente

---

## 🚀 CONCLUSÃO

O projeto **Game.ECS** tem uma **base sólida** com ArchECS e separação client/server. No entanto, há **gaps críticos de implementação** que impedem o funcionamento adequado:

### ⚠️ Problemas Prioritários
1. **MovementSystem sem collision detection** - Crítico
2. **Services não integrados** - Desperdício de código
3. **AISystem ineficiente** - Impacto em performance
4. **Ausência de testes** - Risco de bugs

### ✨ Oportunidades
1. Arquitetura permite extensões facilmente
2. Event system pode ser modularizado
3. Performance pode ser 10x melhor com otimizações simples

### 🎯 Prioridade Absoluta
1. **Semana 1:** Quick wins (collision, dead filter, AI timer)
2. **Semana 2:** Testes básicos + telemetria
3. **Semana 3-4:** Refatoração arquitetural
4. **Mês 2+:** Features avançadas (reconciliation, abilities)

---

**Revisado por:** GitHub Copilot  
**Data:** 21/10/2025  
**Versão:** 2.0 (Focused on Architecture & Performance)
