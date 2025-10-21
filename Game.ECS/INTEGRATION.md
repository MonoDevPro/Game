# 🎮 Game ECS - Integration Guide

## Overview

O sistema ECS está **100% completo e compilado**. Este documento descreve como integrar com o resto do projeto (`Game.Server`, `Game.Network`, `Game.Persistence`).

---

## 📦 Build Status

```
Game.ECS/Game.ECS.csproj
├── ✅ Build: SUCCESS
├── ✅ Compilation: 0 Errors
├── ⚠️ Warnings: 14 (non-critical, generated code)
└── 📦 Output: bin/Debug/net8.0/Game.ECS.dll
```

---

## 🔗 Integration Points

### 1. Game.Server Integration

**Location:** `Game.Server/` → `Program.cs` or `GameServer.cs`

#### Setup

```csharp
using Game.ECS;
using Game.ECS.Examples;

public class GameServer
{
    private ServerGameSimulation _simulation;
    private Dictionary<int, Entity> _playerEntities = new();
    
    public void Initialize()
    {
        _simulation = new ServerGameSimulation();
        Console.WriteLine("✅ Game server initialized with ECS");
    }
    
    public void OnPlayerConnected(int playerId, int networkId)
    {
        // Register player in ECS
        _simulation.RegisterNewPlayer(playerId, networkId);
        
        // Store entity reference for later
        var entity = _simulation.World.GetEntity(networkId);
        _playerEntities[playerId] = entity;
        
        Console.WriteLine($"✅ Player {playerId} spawned at ECS");
    }
    
    public void OnPlayerInput(int playerId, sbyte inputX, sbyte inputY, InputFlags flags)
    {
        if (_playerEntities.TryGetValue(playerId, out var entity))
        {
            _simulation.TryApplyPlayerInput(entity, new PlayerInput
            {
                InputX = inputX,
                InputY = inputY,
                Flags = flags
            });
        }
    }
    
    public void OnPlayerDisconnected(int playerId)
    {
        if (_playerEntities.TryGetValue(playerId, out var entity))
        {
            _simulation.DespawnEntity(entity);
            _playerEntities.Remove(playerId);
            Console.WriteLine($"✅ Player {playerId} despawned from ECS");
        }
    }
    
    public void Update(float deltaTime)
    {
        _simulation.Update(deltaTime);
    }
}
```

#### In Main Loop

```csharp
var server = new GameServer();
server.Initialize();

float deltaTime = 1f / 60f;
var stopwatch = Stopwatch.StartNew();

while (gameRunning)
{
    server.Update(deltaTime);
    
    // Send state updates to clients
    SendNetworkUpdates(server._simulation);
    
    // Throttle to 60 FPS
    while (stopwatch.Elapsed.TotalSeconds < deltaTime)
    {
        Thread.Sleep(1);
    }
    stopwatch.Restart();
}
```

---

### 2. Game.Network Integration

**Location:** `Game.Network/` → Serialization and network layer

#### Serialize State for Network

```csharp
using Game.ECS.Systems;
using MemoryPack;

public class NetworkSyncService
{
    private ServerGameSimulation _simulation;
    private SyncSystem _syncSystem;
    
    public NetworkSyncService(ServerGameSimulation simulation)
    {
        _simulation = simulation;
        _syncSystem = _simulation.Systems.OfType<SyncSystem>().First();
        
        // Subscribe to snapshots
        _syncSystem.OnPlayerStateSnapshot += OnPlayerStateSnapshot;
        _syncSystem.OnPlayerVitalsSnapshot += OnPlayerVitalsSnapshot;
        _syncSystem.OnPlayerInputSnapshot += OnPlayerInputSnapshot;
    }
    
    private void OnPlayerStateSnapshot(PlayerStateSnapshot snapshot)
    {
        // Serialize with MemoryPack
        byte[] bytes = MemoryPackSerializer.Serialize(snapshot);
        
        // Send to other clients
        BroadcastToOthers(bytes, snapshot.NetworkId);
        
        Console.WriteLine($"📤 Synced state: Player {snapshot.NetworkId}");
    }
    
    private void OnPlayerVitalsSnapshot(PlayerVitalsSnapshot snapshot)
    {
        byte[] bytes = MemoryPackSerializer.Serialize(snapshot);
        BroadcastToAll(bytes);
        
        Console.WriteLine($"📤 Synced vitals: Player {snapshot.NetworkId}");
    }
    
    private void OnPlayerInputSnapshot(PlayerInputSnapshot snapshot)
    {
        // Validate input on server
        // (anti-cheat checks here)
        
        Console.WriteLine($"📤 Synced input: Player {snapshot.NetworkId}");
    }
}
```

#### Receive State from Network

```csharp
public void OnNetworkMessageReceived(byte[] data)
{
    // Determine packet type
    var type = GetPacketType(data);
    
    if (type == PacketType.PlayerInput)
    {
        var input = MemoryPackSerializer.Deserialize<PlayerInputSnapshot>(data);
        OnPlayerInput(input.NetworkId, input.InputX, input.InputY, input.Flags);
    }
    else if (type == PacketType.PlayerState)
    {
        var state = MemoryPackSerializer.Deserialize<PlayerStateSnapshot>(data);
        OnPlayerStateUpdate(state);
    }
}
```

---

### 3. Game.Persistence Integration

**Location:** `Game.Persistence/` → Save/Load character data

#### Save on Player Logout

```csharp
using Game.ECS.Systems;
using Game.Persistence;

public class PersistenceService
{
    private ServerGameSimulation _simulation;
    private GameDbContext _dbContext;
    
    public PersistenceService(ServerGameSimulation simulation, GameDbContext dbContext)
    {
        _simulation = simulation;
        _dbContext = dbContext;
        
        // Subscribe to events
        _simulation.EventSystem.OnDeath += OnPlayerDeath;
        _simulation.EventSystem.OnEntitySpawned += OnEntitySpawned;
    }
    
    public void SavePlayerState(int playerId, Entity entity)
    {
        // Get current state
        if (!_simulation.TryGetPlayerState(entity, out var state))
            return;
        
        if (!_simulation.TryGetPlayerVitals(entity, out var vitals))
            return;
        
        // Save to database
        var character = new Character
        {
            Id = playerId,
            X = state.PositionX,
            Y = state.PositionY,
            Z = state.PositionZ,
            Hp = vitals.CurrentHp,
            MaxHp = vitals.MaxHp,
            Mp = vitals.CurrentMp,
            MaxMp = vitals.MaxMp,
            LastUpdated = DateTime.UtcNow
        };
        
        _dbContext.Characters.Update(character);
        _dbContext.SaveChanges();
        
        Console.WriteLine($"💾 Saved player {playerId} to database");
    }
    
    public void LoadPlayerState(int playerId, Entity entity)
    {
        var character = _dbContext.Characters.Find(playerId);
        if (character == null)
            return;
        
        // Apply saved position
        ref Position pos = ref _simulation.World.Get<Position>(entity);
        pos.X = character.X;
        pos.Y = character.Y;
        pos.Z = character.Z;
        
        // Apply saved vitals
        ref Health health = ref _simulation.World.Get<Health>(entity);
        health.Current = character.Hp;
        
        ref Mana mana = ref _simulation.World.Get<Mana>(entity);
        mana.Current = character.Mp;
        
        Console.WriteLine($"📖 Loaded player {playerId} from database");
    }
    
    private void OnPlayerDeath(Entity deadEntity, Entity? killer)
    {
        // Optional: save death statistics
        Console.WriteLine($"💀 Player died, recording statistics...");
    }
    
    private void OnEntitySpawned(Entity entity)
    {
        // Could auto-save periodically
    }
}
```

---

## 🔄 Full Integration Example

### Complete Server Setup

```csharp
using Game.ECS;
using Game.ECS.Examples;
using Game.Network;
using Game.Persistence;
using Arch.Core;

public class GameServerFull
{
    private ServerGameSimulation _simulation;
    private NetworkSyncService _networkSync;
    private PersistenceService _persistence;
    private Dictionary<int, Entity> _players = new();
    
    public void Initialize(GameDbContext dbContext)
    {
        Console.WriteLine("🎮 Initializing Game Server with ECS...\n");
        
        // 1. Initialize ECS
        _simulation = new ServerGameSimulation();
        Console.WriteLine("✅ ECS initialized");
        
        // 2. Initialize Network Sync
        _networkSync = new NetworkSyncService(_simulation);
        Console.WriteLine("✅ Network sync initialized");
        
        // 3. Initialize Persistence
        _persistence = new PersistenceService(_simulation, dbContext);
        Console.WriteLine("✅ Persistence initialized");
        
        // 4. Subscribe to events
        SubscribeToEvents();
        Console.WriteLine("✅ Event subscriptions registered\n");
    }
    
    private void SubscribeToEvents()
    {
        var eventSystem = _simulation.EventSystem;
        
        eventSystem.OnPlayerJoined += (networkId) =>
        {
            Console.WriteLine($"🟢 Player {networkId} joined");
        };
        
        eventSystem.OnPlayerLeft += (networkId) =>
        {
            Console.WriteLine($"🔴 Player {networkId} left");
        };
        
        eventSystem.OnDamage += (attacker, victim, damage) =>
        {
            Console.WriteLine($"⚔️ Damage: {damage} HP");
        };
        
        eventSystem.OnDeath += (dead, killer) =>
        {
            Console.WriteLine($"💀 Death event!");
        };
        
        eventSystem.OnCombatEnter += (entity) =>
        {
            Console.WriteLine($"🗡️ Combat started");
        };
    }
    
    public void OnClientConnected(int playerId, int networkId, string playerName)
    {
        Console.WriteLine($"\n🔗 Player connecting: {playerName} (ID: {playerId}, Network: {networkId})");
        
        // Register in ECS
        _simulation.RegisterNewPlayer(playerId, networkId);
        
        // Get entity reference
        var player = _simulation.World.CreateEntity();
        _players[playerId] = player;
        
        // Load from database
        _persistence.LoadPlayerState(playerId, player);
        
        Console.WriteLine($"✅ {playerName} ready to play!\n");
    }
    
    public void OnClientInput(int playerId, sbyte inputX, sbyte inputY, InputFlags flags)
    {
        if (_players.TryGetValue(playerId, out var entity))
        {
            _simulation.TryApplyPlayerInput(entity, new PlayerInput
            {
                InputX = inputX,
                InputY = inputY,
                Flags = flags
            });
        }
    }
    
    public void OnClientDisconnected(int playerId)
    {
        if (_players.TryGetValue(playerId, out var entity))
        {
            // Save state before despawn
            _persistence.SavePlayerState(playerId, entity);
            
            // Remove from ECS
            _simulation.DespawnEntity(entity);
            _players.Remove(playerId);
            
            Console.WriteLine($"✅ Player {playerId} saved and removed");
        }
    }
    
    public void Update(float deltaTime)
    {
        _simulation.Update(deltaTime);
    }
    
    public void Shutdown()
    {
        Console.WriteLine("\n🛑 Saving all players...");
        
        foreach (var (playerId, entity) in _players)
        {
            _persistence.SavePlayerState(playerId, entity);
        }
        
        Console.WriteLine("✅ Server shutdown complete");
    }
}
```

### Main Program

```csharp
[Program.cs or Main.cs]

using Game.Persistence;

var dbContext = new GameDbContext();
var gameServer = new GameServerFull();
gameServer.Initialize(dbContext);

// Simulate some players
gameServer.OnClientConnected(1, 1001, "Player1");
gameServer.OnClientConnected(2, 1002, "Player2");

// Game loop
float deltaTime = 1f / 60f;
var sw = Stopwatch.StartNew();

while (sw.Elapsed.TotalSeconds < 10) // Run for 10 seconds
{
    gameServer.Update(deltaTime);
    
    // Simulate input
    gameServer.OnClientInput(1, 1, 0, InputFlags.None);
    gameServer.OnClientInput(2, -1, 0, InputFlags.None);
    
    Thread.Sleep(16); // Throttle to ~60 FPS
}

gameServer.OnClientDisconnected(1);
gameServer.OnClientDisconnected(2);
gameServer.Shutdown();
```

---

## 📋 Integration Checklist

- [ ] Add `using Game.ECS;` to Game.Server
- [ ] Create `ServerGameSimulation` instance
- [ ] Subscribe to network events
- [ ] Add player on connect
- [ ] Apply input on receive
- [ ] Send snapshots on sync
- [ ] Deserialize network messages
- [ ] Save state on disconnect
- [ ] Load state on connect
- [ ] Subscribe to ECS events
- [ ] Handle player death/respawn
- [ ] Test full game loop

---

## 🐛 Common Issues & Solutions

### Issue: "Cannot find namespace Game.ECS"
**Solution:** Ensure project reference exists in `.csproj`:
```xml
<ProjectReference Include="../Game.ECS/Game.ECS.csproj" />
```

### Issue: "World not found"
**Solution:** Use `_simulation.World` not just `World`:
```csharp
ref Position pos = ref _simulation.World.Get<Position>(entity);
```

### Issue: "Entity not alive"
**Solution:** Check entity is valid before operations:
```csharp
if (_simulation.World.IsAlive(entity))
{
    // Safe to use
}
```

### Issue: "SyncSystem not found in Systems"
**Solution:** Systems are in a `Group<float>`, iterate correctly:
```csharp
var syncSystem = _simulation.Systems.OfType<SyncSystem>().First();
```

---

## 🚀 Next Steps

1. **Integrate with Game.Server**
   - [ ] Add ECS initialization to `GameServer.cs`
   - [ ] Connect player lifecycle events
   - [ ] Add to game loop

2. **Integrate with Game.Network**
   - [ ] Implement snapshot serialization
   - [ ] Handle network messages
   - [ ] Add state synchronization

3. **Integrate with Game.Persistence**
   - [ ] Map ECS components to database models
   - [ ] Implement save/load logic
   - [ ] Add migration if needed

4. **Testing**
   - [ ] Unit tests for each system
   - [ ] Integration tests for server
   - [ ] Load testing (100+ players)
   - [ ] Network latency simulation

---

## 📊 Performance Tips

1. **Batch Updates**
   - Group player inputs before applying
   - Send network updates in batches

2. **Spatial Partitioning**
   - Use `MapSpatial` for area queries
   - Reduces iteration count

3. **Dirty Flags**
   - Only sync entities marked dirty
   - Clear after network transmission

4. **System Order**
   - Input → Movement → Combat → Health → Sync
   - Minimize state changes between systems

---

## 📚 Documentation Files

- `README_ECS.md` - Architecture and components
- `QUICKSTART.md` - Quick start examples
- `ECS_COMPLETION_STATUS.md` - Implementation checklist
- `INTEGRATION.md` - This file

---

**Ready to integrate! 🚀**
