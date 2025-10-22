# 🚀 Implementação Prática - Quick Wins

Guia passo-a-passo para implementar as melhorias de maior impacto.

---

## ✅ Quick Win #1: Integrar MapService no MovementSystem

### Status: 🔨 Pronto para implementar
**Tempo:** ~4 horas | **Impacto:** ALTO

### Mudanças Necessárias

#### 1. Modificar MovementSystem.cs

```csharp
// Antes:
public sealed partial class MovementSystem(World world, GameEventSystem events, EntityFactory factory) 
    : GameSystem(world, events, factory)
{
    // Sem acesso ao MapService
}

// Depois:
public sealed partial class MovementSystem : GameSystem
{
    private readonly IMapService _mapService;

    public MovementSystem(
        World world, 
        GameEventSystem events, 
        EntityFactory factory,
        IMapService mapService) 
        : base(world, events, factory)
    {
        _mapService = mapService;
    }
    
    // ... resto do código
}
```

#### 2. Atualizar método Step

```csharp
// Adicionar validações antes de mover
private bool TryStep(
    Entity entity,
    ref Position pos, 
    ref Movement movement, 
    ref Velocity vel,
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

    // ⭐ NOVA VALIDAÇÃO 1: Limites do mapa
    var mapGrid = _mapService.GetMapGrid(0); // TODO: usar MapId da entidade
    if (!mapGrid.InBounds(newPos))
    {
        vel.Speed = 0f;
        return false;
    }

    // ⭐ NOVA VALIDAÇÃO 2: Colisão com terreno
    if (mapGrid.IsBlocked(newPos))
    {
        vel.Speed = 0f;
        return false;
    }

    // ⭐ NOVA VALIDAÇÃO 3: Colisão com outras entidades
    var spatial = _mapService.GetMapSpatial(0);
    if (spatial.TryGetFirstAt(newPos, out _))
    {
        vel.Speed = 0f;
        return false;
    }

    // ⭐ Atualiza spatial hash
    spatial.Update(pos, newPos, entity);

    // Move para nova posição
    movement.Timer -= SimulationConfig.CellSize;
    pos = newPos;
    vel.Speed = 0f;
    
    return true;
}
```

#### 3. Atualizar query para passar Entity

```csharp
[Query]
[All<Position, Movement, Velocity, Walkable>]
[None<Dead>] // ⭐ Também adicionar este filtro
private void ProcessMovement(
    in Entity e,  // ⭐ Adicionar Entity
    ref Position pos, 
    ref Movement movement, 
    ref Velocity velocity,
    in Walkable walkable,
    [Data] float deltaTime)
{
    if (TryStep(e, ref pos, ref movement, ref velocity, deltaTime))
        Events.RaisePositionChanged(e, pos.X, pos.Y);
}
```

#### 4. Atualizar Examples para injetar MapService

```csharp
// ServerGameSimulation.cs
public sealed class ServerGameSimulation : GameSimulation
{
    protected override void ConfigureSystems(...)
    {
        // Criar map service
        var mapService = new MapService();
        
        // Injetar nos sistemas
        var movementSystem = new MovementSystem(world, gameEvents, factory, mapService);
        systems.Add(movementSystem);
        
        // ... outros sistemas
    }
}
```

### Teste Manual

```csharp
// Criar mapa com parede
var mapGrid = new MapGrid(100, 100);
mapGrid.SetBlocked(new Position { X = 50, Y = 50 }, true);

// Tentar mover para célula bloqueada
var player = factory.CreatePlayer(...);
World.Set(player, new Velocity { DirectionX = 1, DirectionY = 0, Speed = 5f });

// Resultado esperado: Player para na frente da parede
```

---

## ✅ Quick Win #2: Filtro [None<Dead>] em Queries

### Status: 🔨 Pronto para implementar
**Tempo:** ~2 horas | **Impacto:** MÉDIO

### Arquivos a Modificar

#### 1. MovementSystem.cs

```csharp
[Query]
[All<Position, Movement, Velocity, Walkable>]
[None<Dead>] // ⭐ ADICIONAR
private void ProcessMovement(...)

[Query]
[All<PlayerControlled, Facing>]
[None<Dead>] // ⭐ ADICIONAR
private void ProcessEntityFacing(...)
```

#### 2. HealthSystem.cs

```csharp
[Query]
[All<Health>]
[None<Dead>] // ⭐ ADICIONAR
private void ProcessHealthRegeneration(...)

[Query]
[All<Mana>]
[None<Dead>] // ⭐ ADICIONAR
private void ProcessManaRegeneration(...)
```

#### 3. AISystem.cs

```csharp
[Query]
[All<AIControlled, Position, Velocity, Facing>]
[None<Dead>] // ⭐ ADICIONAR
private void ProcessAIMovement(...)

[Query]
[All<AIControlled, CombatState, Position, Health>]
[None<Dead>] // ⭐ ADICIONAR
private void ProcessAICombat(...)
```

#### 4. InputSystem.cs

```csharp
[Query]
[All<PlayerControlled, Velocity>]
[None<Dead>] // ⭐ ADICIONAR
private void ProcessPlayerInput(...)
```

### Ganho Esperado

- **Performance:** 5-10% menos iterações
- **Correção lógica:** Entidades mortas não processam ações

---

## ✅ Quick Win #3: AI Decision Timer

### Status: 🔨 Pronto para implementar
**Tempo:** ~3 horas | **Impacto:** ALTO

### Passo 1: Criar Componente AIState

```csharp
// Em Components.cs ou criar novo arquivo Components/AI.cs
public struct AIState
{
    public float DecisionCooldown;
    public AIBehavior CurrentBehavior;
    public int TargetNetworkId;
}

public enum AIBehavior : byte
{
    Idle,
    Wander,
    Patrol,
    Chase,
    Attack,
    Flee
}
```

### Passo 2: Adicionar ao Arquétipo NPC

```csharp
// GameArchetypes.cs
public static readonly ComponentType[] NPCCharacter = new[]
{
    // ... existentes
    Component<AIState>.ComponentType, // ⭐ ADICIONAR
};
```

### Passo 3: Inicializar no Factory

```csharp
// EntityFactory.cs - CreateNPC
public Entity CreateNPC(in NPCCharacter data)
{
    var entity = world.Create(GameArchetypes.NPCCharacter);
    var components = new object[]
    {
        // ... componentes existentes
        new AIState 
        { 
            DecisionCooldown = 0f,
            CurrentBehavior = AIBehavior.Wander 
        }, // ⭐ ADICIONAR
    };
    world.SetRange(entity, components);
    events.RaiseEntitySpawned(entity);
    return entity;
}
```

### Passo 4: Refatorar AISystem

```csharp
// AISystem.cs
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
    // ⭐ Decrementa cooldown
    aiState.DecisionCooldown -= deltaTime;
    
    // ⭐ Só decide quando timer expira
    if (aiState.DecisionCooldown > 0f)
        return;
    
    // Reset timer com randomização
    aiState.DecisionCooldown = 0.5f + (float)Random.Shared.NextDouble() * 1f;
    
    // Executa comportamento
    switch (aiState.CurrentBehavior)
    {
        case AIBehavior.Idle:
            // 30% chance de começar a andar
            if (Random.Shared.NextSingle() < 0.3f)
                aiState.CurrentBehavior = AIBehavior.Wander;
            break;
        
        case AIBehavior.Wander:
            ChooseRandomDirection(ref vel, ref facing);
            break;
    }
    
    Events.RaiseFacingChanged(e, facing.DirectionX, facing.DirectionY);
}

private void ChooseRandomDirection(ref Velocity vel, ref Facing facing)
{
    int dir = Random.Shared.Next(0, 5);
    (vel.DirectionX, vel.DirectionY) = dir switch
    {
        0 => (1, 0),
        1 => (-1, 0),
        2 => (0, 1),
        3 => (0, -1),
        _ => (0, 0)
    };

    facing.DirectionX = vel.DirectionX;
    facing.DirectionY = vel.DirectionY;
    vel.Speed = 3f;
}
```

### Benchmark Esperado

```
ANTES:  1000 NPCs × 60 FPS = 60,000 checks/segundo
DEPOIS: 1000 NPCs × 1 decisão/seg = ~1,000 checks/segundo
GANHO:  98% de redução!
```

---

## ✅ Quick Win #4: Validação de Range em CombatSystem

### Status: 🔨 Pronto para implementar
**Tempo:** ~3 horas | **Impacto:** MÉDIO

### Passo 1: Adicionar Constante

```csharp
// SimulationConfig.cs
public static class SimulationConfig
{
    // ... existentes
    
    /// <summary>
    /// Range máximo de ataque corpo-a-corpo (em células).
    /// </summary>
    public const int MaxMeleeAttackRange = 1;
    
    /// <summary>
    /// Range máximo de ataque ranged (em células).
    /// </summary>
    public const int MaxRangedAttackRange = 10;
}
```

### Passo 2: Adicionar Helper em Position

```csharp
// Components.cs
public struct Position 
{ 
    public int X; 
    public int Y; 
    public int Z;
    
    /// <summary>
    /// Distância Manhattan (taxicab).
    /// </summary>
    public readonly int ManhattanDistance(Position other) 
        => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
}
```

### Passo 3: Refatorar TryDamage

```csharp
// CombatSystem.cs
public bool TryAttack(Entity attacker, Entity target)
{
    // Validações de existência
    if (!World.IsAlive(attacker) || !World.IsAlive(target))
        return false;

    // Componentes necessários
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

    // ⭐ VALIDAÇÃO 2: Range check
    int distance = attackerPos.ManhattanDistance(targetPos);
    if (distance > SimulationConfig.MaxMeleeAttackRange)
        return false;

    // ⭐ VALIDAÇÃO 3: Target não está morto/invulnerável
    if (World.Has<Dead>(target) || World.Has<Invulnerable>(target))
        return false;

    // Calcula e aplica dano
    int damage = CalculateDamage(attackPower, defense);
    return ApplyDamageInternal(target, damage, attacker);
}

private bool ApplyDamageInternal(Entity target, int damage, Entity attacker)
{
    ref Health health = ref World.Get<Health>(target);
    
    int previous = health.Current;
    health.Current = Math.Max(0, previous - damage);

    Events.RaiseDamage(attacker, target, damage);
    
    if (health.Current <= 0 && !World.Has<Dead>(target))
    {
        World.Add<Dead>(target);
        Events.RaiseDeath(target, attacker);
    }
    
    return true;
}
```

---

## ✅ Quick Win #5: DirtyFlags para Sincronização

### Status: 🔨 Pronto para implementar
**Tempo:** ~3 horas | **Impacto:** ALTO

### Passo 1: Criar Componente

```csharp
// Components.cs ou NetworkComponents.cs
public struct DirtyFlags 
{ 
    public ushort Flags;
    
    public void MarkDirty(ComponentType type) 
        => Flags |= (ushort)(1 << (int)type);
    
    public void ClearDirty(ComponentType type) 
        => Flags &= (ushort)~(1 << (int)type);
    
    public bool IsDirty(ComponentType type) 
        => (Flags & (ushort)(1 << (int)type)) != 0;
    
    public void ClearAll() => Flags = 0;
}

public enum ComponentType : byte
{
    Position = 0,
    Health = 1,
    Mana = 2,
    Facing = 3,
    Combat = 4,
}
```

### Passo 2: Adicionar ao Arquétipo

```csharp
// GameArchetypes.cs
public static readonly ComponentType[] PlayerCharacter = new[]
{
    // ... existentes
    Component<DirtyFlags>.ComponentType, // ⭐ ADICIONAR
};
```

### Passo 3: Marcar Dirty em Sistemas

```csharp
// MovementSystem.cs
[Query]
[All<Position, Movement, Velocity, DirtyFlags>]
[None<Dead>]
private void ProcessMovement(
    in Entity e, 
    ref Position pos,
    ref DirtyFlags dirty, // ⭐ ADICIONAR
    // ... resto
    [Data] float deltaTime)
{
    if (TryStep(e, ref pos, ref movement, ref velocity, deltaTime))
    {
        dirty.MarkDirty(ComponentType.Position); // ⭐ MARCAR
        Events.RaisePositionChanged(e, pos.X, pos.Y);
    }
}

// CombatSystem.cs - ApplyDamageInternal
private bool ApplyDamageInternal(Entity target, int damage, Entity attacker)
{
    ref Health health = ref World.Get<Health>(target);
    ref DirtyFlags dirty = ref World.Get<DirtyFlags>(target);
    
    health.Current = Math.Max(0, health.Current - damage);
    dirty.MarkDirty(ComponentType.Health); // ⭐ MARCAR
    
    // ...
}
```

### Passo 4: Sistema de Sincronização

```csharp
// Criar novo arquivo: Systems/SyncSystem.cs
public sealed partial class SyncSystem : GameSystem
{
    [Query]
    [All<NetworkId, DirtyFlags>]
    private void SyncDirtyEntities(
        in Entity e, 
        in NetworkId netId, 
        ref DirtyFlags dirty,
        [Data] float deltaTime)
    {
        if (dirty.Flags == 0)
            return; // Nada para sincronizar
        
        // Sincroniza apenas componentes dirty
        if (dirty.IsDirty(ComponentType.Position))
        {
            var pos = World.Get<Position>(e);
            SendPositionUpdate(netId.Value, pos);
        }
        
        if (dirty.IsDirty(ComponentType.Health))
        {
            var health = World.Get<Health>(e);
            SendHealthUpdate(netId.Value, health);
        }
        
        // ... outros componentes
        
        // Limpa flags após sincronizar
        dirty.ClearAll();
    }
    
    private void SendPositionUpdate(int netId, Position pos)
    {
        // TODO: Enviar pela rede
        Console.WriteLine($"[SYNC] Entity {netId} moved to ({pos.X}, {pos.Y})");
    }
    
    private void SendHealthUpdate(int netId, Health health)
    {
        // TODO: Enviar pela rede
        Console.WriteLine($"[SYNC] Entity {netId} health: {health.Current}/{health.Max}");
    }

    public override void BeforeUpdate(in float t) { }
    public override void AfterUpdate(in float t) { }
}
```

### Ganho Esperado

```
SEM DIRTY FLAGS:
- Envia TODOS os componentes de TODAS as entidades todo tick
- 100 players × 10 components × 60 FPS = 60,000 updates/segundo

COM DIRTY FLAGS:
- Envia APENAS componentes que MUDARAM
- 100 players × ~2 components dirty × 60 FPS = 12,000 updates/segundo
- REDUÇÃO: 80%!
```

---

## 📊 Checklist de Implementação

### Semana 1: Quick Wins

- [ ] **Dia 1:** Quick Win #1 - MapService integration (4h)
  - [ ] Modificar MovementSystem
  - [ ] Atualizar ServerGameSimulation
  - [ ] Teste manual
  
- [ ] **Dia 2:** Quick Win #2 + #3 (5h)
  - [ ] Adicionar [None<Dead>] em todas queries (2h)
  - [ ] Implementar AIState e decision timer (3h)
  - [ ] Benchmark antes/depois
  
- [ ] **Dia 3:** Quick Win #4 + #5 (6h)
  - [ ] Range validation em CombatSystem (3h)
  - [ ] DirtyFlags implementation (3h)
  - [ ] Criar SyncSystem
  
- [ ] **Dia 4-5:** Testes e ajustes
  - [ ] Testes manuais completos
  - [ ] Profile performance
  - [ ] Documentar resultados

### Métricas de Sucesso

- ✅ 0 entidades atravessam paredes
- ✅ 0 entidades mortas processadas
- ✅ AI CPU usage < 5% (antes: ~30%)
- ✅ Network bandwidth reduzido 70-80%
- ✅ Todos os testes passam

---

## 🔧 Troubleshooting

### Problema: "IMapService not found"
**Solução:** Adicionar `using Game.ECS.Services;`

### Problema: Entidades param de se mover após mudanças
**Solução:** Verificar se MapService está registrado com mapa padrão

### Problema: DirtyFlags não limpa
**Solução:** Garantir que SyncSystem está sendo executado após outros sistemas

### Problema: AI ainda consumindo muito CPU
**Solução:** Verificar se DecisionCooldown está sendo decrementado corretamente

---

## 📚 Próximos Passos

Após completar os Quick Wins:

1. **Testes Unitários** - Criar suite de testes
2. **Telemetria** - Adicionar métricas de performance
3. **Refatoração Arquitetural** - Reorganizar pastas
4. **Documentation** - Atualizar docs com novas features

---

**Última atualização:** 21/10/2025
