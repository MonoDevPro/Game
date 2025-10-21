# 🚀 ECS Quick Start Guide

## Installation & Setup

### Prerequisites
- .NET 8.0 SDK
- Visual Studio / Rider / VS Code
- NuGet packages already referenced in `.csproj`

### Build

```bash
cd /home/filipe/GameOpen/GameSimulation
dotnet build Game.ECS/Game.ECS.csproj
```

**Expected Output:**
```
Build succeeded.
    0 Erro(s)
    14 Aviso(s)
```

---

## 📖 Basic Usage Examples

### 1️⃣ Server Simulation (Complete Game Logic)

```csharp
using Game.ECS;
using Game.ECS.Entities.Data;
using Game.ECS.Examples;

// Create server simulation
var serverSim = new ServerGameSimulation();

// Register a player joining
int playerId = 1;
int networkId = 1001;
serverSim.RegisterNewPlayer(playerId, networkId);

// Game loop (60 FPS)
float deltaTime = 1f / 60f;
for (int frame = 0; frame < 3600; frame++)
{
    serverSim.Update(deltaTime);
    
    // Get player state if needed
    if (serverSim.TryGetPlayerState(playerEntity, out var state))
    {
        Console.WriteLine($"Player at ({state.PositionX}, {state.PositionY})");
    }
}
```

### 2️⃣ Client Simulation (Local Prediction + Server Sync)

```csharp
using Game.ECS;
using Game.ECS.Examples;

// Create client simulation
var clientSim = new ClientGameSimulation();

// Spawn your local player
clientSim.SpawnLocalPlayer(playerId: 1, networkId: 1001);

// Handle player input
clientSim.HandlePlayerInput(
    inputX: 1,   // Move right
    inputY: 0,
    flags: InputFlags.Sprint
);

// Game loop
float deltaTime = 1f / 60f;
while (true)
{
    clientSim.Update(deltaTime);
}
```

### 3️⃣ Creating Entities

```csharp
// Using EntityFactory directly
var factory = new EntityFactory(world);

// Create a player
var playerData = new PlayerCharacter(
    PlayerId: 1,
    NetworkId: 1001,
    Name: "Hero",
    Level: 10,
    ClassId: 0,
    SpawnX: 50, SpawnY: 50, SpawnZ: 0,
    FacingX: 0, FacingY: 1,
    Hp: 150, MaxHp: 150, HpRegen: 2f,
    Mp: 100, MaxMp: 100, MpRegen: 1f,
    MovementSpeed: 1f, AttackSpeed: 1f,
    PhysicalAttack: 20, MagicAttack: 10,
    PhysicalDefense: 5, MagicDefense: 3
);
var player = factory.CreateLocalPlayer(playerData);

// Create an NPC
var npcData = new NPCCharacter(
    NetworkId: 2000,
    Name: "Goblin",
    PositionX: 60, PositionY: 60, PositionZ: 0,
    Hp: 30, MaxHp: 30, HpRegen: 0.5f,
    PhysicalAttack: 5, MagicAttack: 0,
    PhysicalDefense: 1, MagicDefense: 0
);
var npc = factory.CreateNPC(npcData);

// Create a projectile
var projectileData = new ProjectileData(
    NetworkId: 3000,
    ShooterId: 1001,
    StartX: 50, StartY: 50, StartZ: 0,
    DirectionX: 1, DirectionY: 0,
    Speed: 10f,
    PhysicalDamage: 15, MagicalDamage: 0
);
var projectile = factory.CreateProjectile(projectileData);
```

### 4️⃣ Working with Components

```csharp
using Game.ECS.Components;
using Arch.Core;

// Add a component to entity
world.Add<Invulnerable>(entity);

// Read a component
ref Health health = ref world.Get<Health>(entity);
health.Current -= 10;

// Check if entity has component
if (world.Has<Dead>(entity))
{
    Console.WriteLine("Entity is dead!");
}

// Remove a component
world.Remove<Stun>(entity);

// Try get component (safe)
if (world.TryGet(entity, out Position pos))
{
    Console.WriteLine($"At {pos.X}, {pos.Y}");
}
```

### 5️⃣ Event System

```csharp
using Game.ECS.Systems;

var eventSystem = new GameEventSystem();

// Subscribe to events
eventSystem.OnDeath += (deadEntity, killer) =>
{
    Console.WriteLine($"Entity {deadEntity} died!");
};

eventSystem.OnPlayerJoined += (networkId) =>
{
    Console.WriteLine($"Player {networkId} joined!");
};

eventSystem.OnPositionChanged += (entity, x, y) =>
{
    Console.WriteLine($"Entity moved to {x}, {y}");
};

eventSystem.OnCombatEnter += (entity) =>
{
    Console.WriteLine($"Combat started for {entity}");
};

// Raise events (from your systems)
eventSystem.RaisePlayerJoined(1001);
eventSystem.RaiseCombatEnter(entity);
```

### 6️⃣ Map Services

```csharp
using Game.ECS.Services;

// Create a map
var mapGrid = new MapGrid(width: 100, height: 100);

// Check bounds
var pos = new Position { X: 50, Y: 50, Z: 0 };
if (mapGrid.InBounds(pos))
{
    Console.WriteLine("Position is valid!");
}

// Clamp position to bounds
var clamped = mapGrid.ClampToBounds(pos);

// Check if blocked
if (!mapGrid.IsBlocked(pos))
{
    Console.WriteLine("Can move here");
}

// Use spatial hashing for fast queries
var spatial = new MapSpatial();
spatial.Insert(pos, entity);

// Find entities at position
if (spatial.TryGetFirstAt(pos, out var foundEntity))
{
    Console.WriteLine($"Found entity at position!");
}

// Query nearby entities (radius)
var nearby = new List<Entity>();
spatial.QueryNearby(pos, radius: 5, nearby);
foreach (var nearbyEntity in nearby)
{
    Console.WriteLine($"Nearby: {nearbyEntity}");
}

// Multiple maps with MapService
var mapService = new MapService();
var map0 = mapService.GetMapGrid(0);
var map1 = mapService.CreateMap(1, 100, 100);
```

### 7️⃣ Network Dirty Flags

```csharp
using Game.ECS.Utils;

// Mark entity as dirty for specific data types
entity.MarkNetworkDirty(SyncFlags.Movement);
entity.MarkNetworkDirty(SyncFlags.Vitals);
entity.MarkNetworkDirty(SyncFlags.All); // All flags

// Check if dirty
ref NetworkDirty dirty = ref world.Get<NetworkDirty>(entity);
bool isMovementDirty = (dirty.Flags & SyncFlags.Movement) != 0;

// Clear flags after sync
entity.ClearNetworkDirty(SyncFlags.Movement);
entity.ClearNetworkDirty(SyncFlags.All);
```

---

## 🎮 Game Loop Pattern

### Recommended Server Loop

```csharp
var server = new ServerGameSimulation();
float deltaTime = 1f / 60f; // 60 FPS

while (gameRunning)
{
    // 1. Process input from clients
    foreach (var clientMessage in networkQueue)
    {
        server.HandleNetworkMessage(clientMessage);
    }
    
    // 2. Update simulation
    server.Update(deltaTime);
    
    // 3. Send state to clients
    foreach (var entity in dirtyEntities)
    {
        SendPlayerState(entity);
    }
}
```

### Recommended Client Loop

```csharp
var client = new ClientGameSimulation();
float deltaTime = 1f / 60f;

while (gameRunning)
{
    // 1. Collect user input
    var (inputX, inputY) = GetUserInput();
    client.HandlePlayerInput(inputX, inputY, flags);
    
    // 2. Update local simulation
    client.Update(deltaTime);
    
    // 3. Send input to server
    SendPlayerInput(inputX, inputY, flags);
    
    // 4. Receive & apply server updates
    foreach (var update in networkQueue)
    {
        ApplyServerUpdate(update);
    }
    
    // 5. Render
    Render();
}
```

---

## 🧪 Testing & Validation

### Run Integrity Checks

```csharp
using Game.ECS.Validation;

// Validate entire ECS
ECSIntegrityValidator.ValidateAll();
// Output:
// === ECS INTEGRITY VALIDATION ===
// [1] Validando componentes...
// [2] Validando sistemas...
// [3] Validando EntityFactory...
// [4] Validando Archetypes...
// [5] Validando Serviços...
// ✅ Todas as validações passaram!

// Print feature checklist
FeatureCheckList.PrintCheckList();
```

---

## 📊 Configuration

Edit `SimulationConfig.cs` to customize:

```csharp
public static class SimulationConfig
{
    // Timestep
    public const float TickDelta = 1f / 60f;  // 60 ticks per second
    
    // ECS configuration
    public const int ChunkSizeInBytes = 4096;
    public const int MinimumAmountOfEntitiesPerChunk = 256;
    public const int ArchetypeCapacity = 1024;
    public const int EntityCapacity = 10000;
    
    public const string SimulationName = "GameSimulation";
}
```

---

## 🐛 Debugging Tips

### Print Entity State

```csharp
// Print all components of an entity
foreach (var component in world.GetComponentTypes(entity))
{
    Console.WriteLine($"Has: {component.Type.Name}");
}

// Print position
if (world.TryGet(entity, out Position pos))
{
    Console.WriteLine($"Position: ({pos.X}, {pos.Y}, {pos.Z})");
}

// Print health
if (world.TryGet(entity, out Health health))
{
    Console.WriteLine($"Health: {health.Current}/{health.Max}");
}
```

### Check Network Dirty Status

```csharp
if (world.TryGet(entity, out NetworkDirty dirty))
{
    if ((dirty.Flags & SyncFlags.Movement) != 0)
        Console.WriteLine("Movement is dirty!");
    if ((dirty.Flags & SyncFlags.Vitals) != 0)
        Console.WriteLine("Vitals are dirty!");
}
```

### Monitor Events

```csharp
eventSystem.OnDamage += (attacker, victim, damage) =>
{
    Console.WriteLine($"[DAMAGE] {attacker} dealt {damage} to {victim}");
};

eventSystem.OnDeath += (dead, killer) =>
{
    Console.WriteLine($"[DEATH] {dead} killed by {killer}");
};

eventSystem.OnNetworkDirty += (entity) =>
{
    Console.WriteLine($"[DIRTY] {entity} needs sync");
};
```

---

## 📚 Further Reading

- `README_ECS.md` - Detailed architecture documentation
- `ECS_COMPLETION_STATUS.md` - Complete implementation checklist
- `Examples/ServerGameSimulation.cs` - Full server example
- `Examples/ClientGameSimulation.cs` - Full client example
- `Examples/ECSUsageExample.cs` - Basic usage example

---

## 🤝 Integration Points

### With Game.Network
- Listen to `SyncSystem.OnPlayerStateSnapshot`
- Serialize snapshots with MemoryPack
- Send over network
- Deserialize and apply on client

### With Game.Persistence
- Listen to `GameEventSystem.OnDeath`
- Save character stats when needed
- Load saved state on player spawn

### With Game.Server
- Create `ServerGameSimulation` in server loop
- Register players when they connect
- Apply input from network messages
- Send state updates periodically

---

**Happy coding! 🎮**
