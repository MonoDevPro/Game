# 🏗️ Proposta de Refatoração Arquitetural

Documentação da nova estrutura de pastas e exemplos de código refatorado.

---

## 📁 Nova Estrutura de Pastas

```
Game.ECS/
│
├── Core/                                    # Funcionalidades essenciais
│   ├── Components/
│   │   ├── Identity.cs                     # NetworkId, PlayerId
│   │   ├── Transform.cs                    # Position, Velocity, Facing, Movement
│   │   ├── Vitals.cs                       # Health, Mana
│   │   └── Tags.cs                         # LocalPlayer, RemotePlayer, Dead, etc
│   │
│   ├── Systems/
│   │   ├── MovementSystem.cs
│   │   └── TransformSystem.cs              # Facing updates
│   │
│   ├── Events/
│   │   ├── LifecycleEvents.cs              # Spawn, Despawn
│   │   └── MovementEvents.cs               # Position, Facing changes
│   │
│   └── Services/
│       └── TimeService.cs                  # Tick management
│
├── Gameplay/                               # Lógica de jogo
│   ├── Components/
│   │   ├── Combat.cs                       # Attack, Defense, CombatState
│   │   ├── AI.cs                          # AIControlled, AIState
│   │   ├── StatusEffects.cs               # Stun, Slow, Poison, Burning
│   │   └── Abilities.cs                   # Cooldowns, Skills
│   │
│   ├── Systems/
│   │   ├── CombatSystem.cs
│   │   ├── HealthSystem.cs
│   │   ├── AISystem.cs
│   │   └── StatusEffectSystem.cs
│   │
│   └── Events/
│       └── CombatEvents.cs                # Damage, Heal, Death
│
├── Networking/                            # Sincronização client/server
│   ├── Components/
│   │   ├── NetworkComponents.cs           # NetworkId, DirtyFlags
│   │   ├── Snapshots.cs                   # State snapshots
│   │   └── Prediction.cs                  # Client prediction state
│   │
│   ├── Systems/
│   │   ├── SyncSystem.cs                  # Dirty flag sync
│   │   └── ReconciliationSystem.cs        # Client prediction
│   │
│   └── Events/
│       └── NetworkEvents.cs               # Sync events
│
├── Spatial/                               # Gerenciamento de mapas
│   ├── Components/
│   │   └── MapId.cs                       # Identificação de mapa
│   │
│   ├── Interfaces/
│   │   ├── IMapGrid.cs
│   │   ├── IMapSpatial.cs
│   │   └── IMapService.cs
│   │
│   └── Implementation/
│       ├── MapGrid.cs
│       ├── MapSpatial.cs
│       └── MapService.cs
│
├── Entities/                              # Criação de entidades
│   ├── Archetypes/
│   │   └── GameArchetypes.cs
│   │
│   ├── Data/
│   │   └── GameData.cs
│   │
│   └── Factories/
│       ├── IEntityFactory.cs
│       └── EntityFactory.cs
│
├── Telemetry/                             # Observabilidade
│   ├── ECSMetrics.cs
│   ├── PerformanceProfiler.cs
│   └── DebugDrawer.cs
│
├── Utils/
│   ├── SimulationConfig.cs
│   └── MathHelpers.cs
│
├── Examples/                              # Exemplos de uso
│   ├── ServerGameSimulation.cs
│   └── ClientGameSimulation.cs
│
└── GameSimulation.cs                      # Classe base

Game.Tests/                                # Testes unitários
├── Core/
│   ├── Components/
│   │   └── PositionTests.cs
│   └── Systems/
│       └── MovementSystemTests.cs
├── Gameplay/
│   └── Systems/
│       ├── CombatSystemTests.cs
│       └── HealthSystemTests.cs
├── Spatial/
│   ├── MapGridTests.cs
│   └── MapSpatialTests.cs
├── Fixtures/
│   └── TestWorldBuilder.cs
└── Integration/
    └── FullSimulationTests.cs
```

---

## 📝 Exemplos de Código Refatorado

### Core/Components/Transform.cs

```csharp
namespace Game.ECS.Core.Components;

/// <summary>
/// Posição no mundo (grid-based).
/// </summary>
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
    
    public readonly Position Add(Velocity velocity)
        => Add(velocity.DirectionX, velocity.DirectionY);
    
    public readonly bool Equals(Position other) 
        => X == other.X && Y == other.Y && Z == other.Z;
    
    public override readonly string ToString() 
        => $"({X}, {Y}, {Z})";
}

/// <summary>
/// Velocidade e direção de movimento.
/// </summary>
public struct Velocity 
{ 
    public int DirectionX; 
    public int DirectionY; 
    public float Speed;
    
    public readonly bool IsMoving 
        => (DirectionX != 0 || DirectionY != 0) && Speed > 0f;
    
    public void Stop()
    {
        DirectionX = 0;
        DirectionY = 0;
        Speed = 0f;
    }
}

/// <summary>
/// Direção que a entidade está olhando.
/// </summary>
public struct Facing 
{ 
    public int DirectionX; 
    public int DirectionY;
    
    public void SetFromVelocity(in Velocity velocity)
    {
        DirectionX = velocity.DirectionX;
        DirectionY = velocity.DirectionY;
    }
}

/// <summary>
/// Acumulador de movimento entre células.
/// </summary>
public struct Movement 
{ 
    public float Timer;
    
    public bool ShouldStep(float cellSize) => Timer >= cellSize;
    
    public void ConsumeStep(float cellSize) => Timer -= cellSize;
}

/// <summary>
/// Capacidade de andar.
/// </summary>
public struct Walkable 
{ 
    public float BaseSpeed; 
    public float CurrentModifier;
    
    public readonly float EffectiveSpeed => BaseSpeed * CurrentModifier;
}
```

### Core/Components/Tags.cs

```csharp
namespace Game.ECS.Core.Components;

// ============================================
// Player Tags
// ============================================

/// <summary>
/// Marca jogador local (controlado por este cliente).
/// </summary>
public struct LocalPlayerTag { }

/// <summary>
/// Marca jogador remoto (controlado por outro cliente).
/// </summary>
public struct RemotePlayerTag { }

/// <summary>
/// Entidade controlada por jogador (local ou remoto).
/// </summary>
public struct PlayerControlled { }

// ============================================
// AI Tags
// ============================================

/// <summary>
/// Entidade controlada por IA.
/// </summary>
public struct AIControlled { }

// ============================================
// State Tags
// ============================================

/// <summary>
/// Entidade está morta.
/// Query: [None<Dead>] para filtrar entidades mortas.
/// </summary>
public struct Dead { }

/// <summary>
/// Entidade é invulnerável a dano.
/// </summary>
public struct Invulnerable { }

/// <summary>
/// Entidade não pode usar habilidades.
/// </summary>
public struct Silenced { }

/// <summary>
/// Entidade não pode se mover.
/// </summary>
public struct Rooted { }

// ============================================
// Performance Tags
// ============================================

/// <summary>
/// Entidade precisa de regeneração de HP.
/// Remove quando HP estiver cheio para otimizar queries.
/// </summary>
public struct NeedsHealthRegeneration { }

/// <summary>
/// Entidade precisa de regeneração de MP.
/// </summary>
public struct NeedsManaRegeneration { }
```

### Gameplay/Components/AI.cs

```csharp
namespace Game.ECS.Gameplay.Components;

/// <summary>
/// Estado da IA com timer de decisão.
/// </summary>
public struct AIState
{
    /// <summary>
    /// Tempo até próxima decisão (em segundos).
    /// </summary>
    public float DecisionCooldown;
    
    /// <summary>
    /// Comportamento atual da IA.
    /// </summary>
    public AIBehavior CurrentBehavior;
    
    /// <summary>
    /// NetworkId do alvo atual (se houver).
    /// </summary>
    public int TargetNetworkId;
    
    /// <summary>
    /// Ponto de origem para patrol.
    /// </summary>
    public Position PatrolOrigin;
    
    /// <summary>
    /// Raio de patrulha.
    /// </summary>
    public float PatrolRadius;
    
    // ⭐ Helpers
    public readonly bool HasTarget => TargetNetworkId != 0;
    
    public readonly bool ShouldDecide(float deltaTime, out float newCooldown)
    {
        newCooldown = DecisionCooldown - deltaTime;
        return newCooldown <= 0f;
    }
    
    public void ResetCooldown(float min, float max)
    {
        DecisionCooldown = min + Random.Shared.NextSingle() * (max - min);
    }
}

/// <summary>
/// Tipos de comportamento de IA.
/// </summary>
public enum AIBehavior : byte
{
    /// <summary>
    /// Parado, sem fazer nada.
    /// </summary>
    Idle = 0,
    
    /// <summary>
    /// Andando aleatoriamente.
    /// </summary>
    Wander = 1,
    
    /// <summary>
    /// Patrulhando em área definida.
    /// </summary>
    Patrol = 2,
    
    /// <summary>
    /// Perseguindo alvo.
    /// </summary>
    Chase = 3,
    
    /// <summary>
    /// Atacando alvo.
    /// </summary>
    Attack = 4,
    
    /// <summary>
    /// Fugindo de perigo.
    /// </summary>
    Flee = 5,
    
    /// <summary>
    /// Retornando ao ponto de spawn.
    /// </summary>
    Return = 6
}
```

### Networking/Components/NetworkComponents.cs

```csharp
namespace Game.ECS.Networking.Components;

/// <summary>
/// Rastreamento de componentes modificados para sincronização eficiente.
/// </summary>
public struct DirtyFlags 
{ 
    public ushort Flags;
    
    public void MarkDirty(DirtyComponent component) 
        => Flags |= (ushort)(1 << (int)component);
    
    public void ClearDirty(DirtyComponent component) 
        => Flags &= (ushort)~(1 << (int)component);
    
    public readonly bool IsDirty(DirtyComponent component) 
        => (Flags & (ushort)(1 << (int)component)) != 0;
    
    public void ClearAll() => Flags = 0;
    
    public readonly bool HasAnyDirty => Flags != 0;
}

/// <summary>
/// Tipos de componentes sincronizáveis.
/// </summary>
public enum DirtyComponent : byte
{
    Position = 0,
    Health = 1,
    Mana = 2,
    Facing = 3,
    Combat = 4,
    Equipment = 5,
    Inventory = 6,
    Stats = 7,
}

/// <summary>
/// Último tick sincronizado com servidor.
/// </summary>
public struct LastSyncTick 
{ 
    public uint Value;
    
    public readonly bool IsOutdated(uint currentTick, uint maxLag)
        => currentTick - Value > maxLag;
}

/// <summary>
/// ID do proprietário/controlador da entidade.
/// </summary>
public struct OwnerId 
{ 
    public int Value;
    
    public readonly bool IsOwnedBy(int playerId) => Value == playerId;
}
```

### Core/Events/GameEvents.cs (Refatorado)

```csharp
namespace Game.ECS.Core.Events;

/// <summary>
/// Agregador de eventos do jogo, organizado por domínio.
/// </summary>
public class GameEvents
{
    public LifecycleEvents Lifecycle { get; } = new();
    public MovementEvents Movement { get; } = new();
    public CombatEvents Combat { get; } = new();
    public InputEvents Input { get; } = new();
    
    // ⭐ Backwards compatibility (deprecated)
    [Obsolete("Use Lifecycle.OnEntitySpawned instead")]
    public event Action<Entity>? OnEntitySpawned
    {
        add => Lifecycle.OnEntitySpawned += value;
        remove => Lifecycle.OnEntitySpawned -= value;
    }
    
    [Obsolete("Use Movement.OnPositionChanged instead")]
    public event Action<Entity, int, int>? OnPositionChanged
    {
        add => Movement.OnPositionChanged += value;
        remove => Movement.OnPositionChanged -= value;
    }
    
    // ... outros eventos deprecated para compatibilidade
}

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
    
    public void RaiseHeal(Entity healer, Entity target, int amount) 
        => OnHeal?.Invoke(healer, target, amount);
    
    public void RaiseDeath(Entity victim, Entity? killer) 
        => OnDeath?.Invoke(victim, killer);
    
    public void RaiseCombatEnter(Entity e) 
        => OnCombatEnter?.Invoke(e);
    
    public void RaiseCombatExit(Entity e) 
        => OnCombatExit?.Invoke(e);
}
```

### Telemetry/ECSMetrics.cs

```csharp
namespace Game.ECS.Telemetry;

using System.Diagnostics;
using System.Diagnostics.Metrics;

/// <summary>
/// Métricas de observabilidade para o ECS.
/// </summary>
public class ECSMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _entityCount;
    private readonly Histogram<double> _systemUpdateTime;
    private readonly Counter<long> _eventCount;
    private readonly ObservableGauge<int> _worldEntityCount;

    public ECSMetrics(World world, string serviceName = "GameECS")
    {
        _meter = new Meter(serviceName, "1.0.0");
        
        _entityCount = _meter.CreateCounter<long>(
            "ecs.entities.count",
            description: "Number of active entities by type");
        
        _systemUpdateTime = _meter.CreateHistogram<double>(
            "ecs.system.update_time",
            unit: "ms",
            description: "Time taken to update a system");
        
        _eventCount = _meter.CreateCounter<long>(
            "ecs.events.count",
            description: "Number of events fired by type");
        
        _worldEntityCount = _meter.CreateObservableGauge<int>(
            "ecs.world.entity_count",
            observeValue: () => world.CountEntities(),
            description: "Total entities in world");
    }

    public void RecordEntitySpawn(string entityType)
    {
        _entityCount.Add(1, 
            new KeyValuePair<string, object?>("entity_type", entityType),
            new KeyValuePair<string, object?>("operation", "spawn"));
    }

    public void RecordEntityDespawn(string entityType)
    {
        _entityCount.Add(1, 
            new KeyValuePair<string, object?>("entity_type", entityType),
            new KeyValuePair<string, object?>("operation", "despawn"));
    }

    public SystemUpdateScope MeasureSystemUpdate(string systemName)
    {
        return new SystemUpdateScope(_systemUpdateTime, systemName);
    }

    public void RecordEvent(string eventType)
    {
        _eventCount.Add(1, 
            new KeyValuePair<string, object?>("event_type", eventType));
    }

    public readonly struct SystemUpdateScope : IDisposable
    {
        private readonly Histogram<double> _histogram;
        private readonly string _systemName;
        private readonly Stopwatch _sw;

        public SystemUpdateScope(Histogram<double> histogram, string systemName)
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
```

---

## 🔄 Migração Gradual

### Fase 1: Manter Estrutura Atual, Adicionar Novos Namespaces

```csharp
// Pode manter arquivos antigos e adicionar aliases
using Transform = Game.ECS.Core.Components.Position;
using OldPosition = Game.ECS.Components.Position;
```

### Fase 2: Deprecate Antigos

```csharp
namespace Game.ECS.Components;

[Obsolete("Use Game.ECS.Core.Components.Position instead")]
public struct Position { ... }
```

### Fase 3: Remover Antigos

Após garantir que todos os usos migraram, remover arquivos antigos.

---

## 📊 Benefícios da Refatoração

### Organização
- ✅ Fácil navegação
- ✅ Separação clara de concerns
- ✅ Escalabilidade melhorada

### Performance
- ✅ Queries otimizadas com tags
- ✅ Menos componentes por query
- ✅ Melhor cache locality

### Manutenibilidade
- ✅ Testes organizados por domínio
- ✅ Events agrupados logicamente
- ✅ Componentes com helpers úteis

### Extensibilidade
- ✅ Fácil adicionar novo domínio
- ✅ Plugins podem adicionar pastas próprias
- ✅ Namespaces claros

---

**Última atualização:** 21/10/2025
