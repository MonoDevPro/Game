# Game Multiplayer MVP Server

This repository contains the server-side simulation for a grid-based multiplayer RPG prototype. The current MVP implements the end-to-end flow for a player to authenticate, join the world, move across the map, and disconnect gracefully.

## Features

- **Account authentication** backed by SQLite and BCrypt password verification.
- **Session management** preventing duplicate logins per account and tying network peers to spawned ECS entities.
- **Deterministic simulation** running at 60 TPS with a fixed time step and ECS-based systems.
- **Grid movement** with collision checks, fractional accumulation, and network dirty flag tracking.
- **Network messaging** (LiteNetLib + MemoryPack) for login, player input, spawn/despawn notifications, and state snapshots.
- **Persistence** of character position/direction on logout, with automatic database migrations on startup.

## Player lifecycle

1. Client connects to the LiteNetLib server and issues a `LoginRequest`.
2. Credentials are validated against the database; on success the server spawns an ECS entity at the character's saved location.
3. The client receives a `LoginResponse` containing its player snapshot and current online players.
4. Movement inputs are sent as `PlayerInput` packets. The server applies them during the simulation loop and marks entities dirty when they move.
5. After each simulation tick, dirty entities are broadcast through `PlayerState` packets so every client stays in sync.
6. On disconnect, the session is removed, the entity is despawned, a `PlayerDespawn` notification is broadcast, and the latest position/direction are stored in the database.

## Running the server

1. Ensure the bundled `identifier.sqlite` file is present (it is copied next to the binaries automatically).
2. Build and run the worker:

```bash
dotnet run --project Game.Server
```

The server listens on `0.0.0.0:7777` with the connection key `default`.

## Tests

A lightweight test suite validates key behaviours for session management and movement syncing. Run them with:

```bash
dotnet test
```

## Next steps

- Expand persistence to save stats/inventory changes during play.
- Implement authoritative combat and damage systems.
- Add reconciliation or lag compensation for smoother client prediction.
