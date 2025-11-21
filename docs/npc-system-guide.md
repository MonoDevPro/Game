# NPC System – Quick Start & How-To

This guide explains how to bring an NPC to life inside the current pipeline (Server → ECS → Snapshots → Client). It covers which components you must set, how the behaviour state machine works, and how to test everything end-to-end.

---

## 1. Architecture at a Glance

| Layer | Responsibility | Key Files |
| --- | --- | --- |
| **Spawner / Boot** | Create NPC entities and keep reference IDs. | `Game.Server/Npc/NpcSpawnService.cs` |
| **ECS Components** | Stats, AI state, patrol/home, behaviour knobs. | `Game.ECS/Components/*.cs` (`NpcAIState`, `NpcBehavior`, `NpcTarget`, `NpcPatrol`, `DirtyFlags`, etc.) |
| **Systems (Server)** | Drive perception, AI transitions, movement intents, and combat flags. | `Game.Server/ECS/Systems/NpcPerceptionSystem.cs`, `NpcAISystem.cs`, `NpcMovementSystem.cs`, `NpcCombatSystem.cs` |
| **Sync** | Packs spawn/state snapshots and sends them to every peer. | `Game.Server/ECS/Systems/ServerSyncSystem.cs` |
| **Network Packets** | Defines `NpcSpawnPacket` + `NpcStatePacket`. | `Game.Network/Packets/Game/NpcPackets.cs` |
| **Client Visual** | Godot script that spawns the sprite and reacts to snapshots. | `Simulation.Client/godot-client/Scripts/Simulation/NpcVisual.cs` |

As soon as an entity has `AIControlled`, `NpcAIState`, `NpcBehavior`, `NpcTarget`, `NpcPatrol`, `Input`, `Walkable`, `Attackable`, `CombatState`, and `DirtyFlags`, the registered systems will pick it up automatically.

---

## 2. Spawning Your First NPC

1. **Describe the stat block** inside `NpcSpawnService`. Add or change entries in `BuildDefaultDefinitions()`:
   ```csharp
   new NpcSpawnDefinition {
       MapId = 0,
       PositionX = 32,
       PositionY = 14,
       PositionZ = 0,
       Hp = 90,
       MaxHp = 90,
       HpRegen = 0.2f,
       PhysicalAttack = 15,
       MagicAttack = 0,
       PhysicalDefense = 4,
       MagicDefense = 2
   }
   ```

2. **Spawn on boot**. `GameServer` calls `NpcSpawnService.SpawnInitialNpcs()` during startup (see `Game.Server/GameServer.cs`), so every definition becomes a live entity.

3. **Tune behaviour right after creation** (optional but recommended):
   ```csharp
   public void SpawnInitialNpcs()
   {
       foreach (var definition in _definitions)
       {
           var npcData = BuildNpcData(definition);
           var entity = simulation.CreateNpc(npcData);

           if (simulation.World.TryGet(entity, out NpcBehavior behavior))
           {
               behavior.Type = NpcBehaviorType.Aggressive;   // Passive, Defensive, Aggressive
               behavior.VisionRange = 8f;                    // Tiles
               behavior.AttackRange = 1.5f;                  // Tiles
               behavior.LeashRange = 14f;                    // Tiles
               behavior.PatrolRadius = 4f;                   // Used by NpcMovementSystem
               behavior.IdleDurationMin = 1.5f;
               behavior.IdleDurationMax = 4f;
           }

           if (simulation.World.TryGet(entity, out NpcPatrol patrol))
           {
               patrol.HomePosition = new Position { X = npcData.PositionX, Y = npcData.PositionY, Z = npcData.PositionZ };
               patrol.Radius = 4f;
               patrol.ResetDestination();
           }
       }
   }
   ```
   Instead of changing `NpcFactory`, prefer configuring the components right after spawning so different definitions can share the same archetype.

4. **Keep the network id**. `NpcSpawnService` stores every spawned `NetworkId` in `_activeNetworkIds`, which is used to build snapshots for late joiners. When despawning manually, call `TryDespawnNpc(id)` so indexes stay consistent.

---

## 3. Behaviour Reference

### 3.1 Components that matter

- `NpcBehavior`
  | Field | Meaning |
  | --- | --- |
  | `Type` | `Passive` (never aggro), `Defensive` (aggro only if attacked – hook not implemented yet), `Aggressive` (hunt any target detected). |
  | `VisionRange` | Radius (tiles) used by `NpcPerceptionSystem` to scan players. |
  | `AttackRange` | Allowed distance before switching from `Chasing` to `Attacking`. |
  | `LeashRange` | Hard limit; if the target exceeds it, the NPC gives up and returns home. |
  | `PatrolRadius` | Radius around `NpcPatrol.HomePosition` used when you script custom patrol logic. |
  | `IdleDurationMin/Max` | Reserved for future idle animation logic; can already be used by custom systems if needed.

- `NpcAIState`
  | State | When it triggers |
  | --- | --- |
  | `Idle` | Default. NPC stands still until it finds a target. |
  | `Patrolling` | (Hook available; you can push the state manually for wandering NPCs.) |
  | `Chasing` | Target spotted but still outside attack range. Movement inputs point to the target. |
  | `Attacking` | Within range; `NpcCombatSystem` raises the attack input flag so `AttackSystem` will execute hits. |
  | `Returning` | No target or target leashed away; NPC walks back to `NpcPatrol.HomePosition` and then idles.

- `NpcTarget`
  - Filled automatically by `NpcPerceptionSystem`. Contains `Entity` handle, last known position, squared distance, and the `TargetNetworkId` sent to clients.

- `NpcPatrol`
  - Stores home position + optional destination. `NpcMovementSystem` only uses `Destination` when `HasDestination` is true, so you can schedule patrol routes by flipping that flag in a custom patrol system.

### 3.2 System flow per tick

1. **Perception** (`NpcPerceptionSystem`)
   - Queries the map’s spatial index (`MapService`) in a square area = `VisionRange`.
   - Picks the closest `PlayerControlled` entity that is alive on the same map.
   - Clears the target automatically if it despawns, dies, or exceeds `LeashRange`.

2. **State Machine** (`NpcAISystem`)
   - Drives transitions between Idle → Chasing → Attacking → Returning.
   - Marks `DirtyComponentType.State` whenever the state changes so the client can show the proper animation if desired.

3. **Movement driver** (`NpcMovementSystem`)
   - Converts the high-level state into `Input.InputX/Y` deltas (Chasing/Returning/Patrolling).
   - Marks `DirtyComponentType.Input`, which eventually affects velocity and snapshots.

4. **Combat driver** (`NpcCombatSystem`)
   - Raises or clears the `InputFlags.Attack` bit when the NPC is within `AttackRange`.
   - `AttackSystem` and `CombatSystem` reuse the same code path as players, so damage, cooldown, and dirty flags work automatically.

5. **Sync** (`ServerSyncSystem`)
   - Whenever the NPC’s dirty flags include `State` or `Vitals`, it sends an `NpcStatePacket` to everyone.
   - When the entity is first created (dirty `All`), it sends the full `NpcSpawnPacket` with stats and map position.

---

## 4. Creating Behaviour Variants

| Goal | What to tweak |
| --- | --- |
| Passive villager | Set `behavior.Type = NpcBehaviorType.Passive` and optionally leave `VisionRange` small. They will never transition into Chase/Attack. |
| Territorial guard | Aggressive type, `VisionRange = 5`, `AttackRange = 1.5`, `LeashRange = 7` so they do not chase too far. |
| Ranged caster (future work) | Keep `AttackRange` > 4 so they stop earlier. You can also lower `NpcMovementSystem` speed by modifying `Walkable.CurrentModifier`. |
| Patrol route | After spawning, set `patrol.Destination = Home + offset; patrol.HasDestination = true;` and optionally schedule a custom system to swap destinations every few seconds. |

Remember: every field lives in ECS components, so you can create authoring helpers, load JSON, or hook an editor to set them.

---

## 5. Testing & Troubleshooting

1. **Unit tests**
   - `dotnet test Game.Tests/Game.Tests.csproj --filter "CombatSyncTests"` verifies that combat dirty flags still emit packets.
   - Add new tests under `Game.Tests` to cover perception or AI flows if you change them.

2. **Manual dedicated-server test**
   - Run `dotnet run --project Game.Server`.
   - Connect with the Godot client (`Simulation.Client/godot-client`).
   - NPCs should appear immediately thanks to the spawn packet sent at login.

3. **Common issues**
   | Symptom | Fix |
   | --- | --- |
   | NPC never moves | Check that it has `AIControlled`, `Input`, `NpcAIState`, and `NpcBehavior`. State must not stay in `Idle` (likely `VisionRange` too small or `Passive` type). |
   | NPC slides forever after target disappears | Ensure `LeashRange` is larger than `VisionRange`. Returning state triggers only when `NpcTarget.HasTarget` becomes false. |
   | Client does not show new NPCs | Confirm `ServerSyncSystem` is flushing spawn/state buffers (called via `Systems.AfterUpdate`). Also verify the NPC has `DirtyFlags` component. |

---

## 6. Next Steps

- Externalize `NpcSpawnDefinition` into JSON/Scriptable data so designers can add NPCs without recompiling.
- Implement an `NpcPatrolSystem` that periodically selects a new `patrol.Destination` inside `patrol.Radius`.
- Extend `NpcBehaviorType.Defensive` to react only after taking damage (hook into `VitalsSystem.ProcessDamage`).
- Update `NpcVisual` to change sprite/animation based on `NpcBehaviorType` or other metadata carried in `NPCData`.

With these building blocks you can create aggressive mobs, passive townsfolk, or scripted quest givers while reusing the same ECS pipeline already integrated with networking and the Godot client.
