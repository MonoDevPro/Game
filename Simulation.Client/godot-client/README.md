# Godot Client (C#) — Autoload GameClient

This project is a Godot 4.5 (.NET 8) client that boots the existing Simulation ECS + Networking and connects to the server.

## What was added
- Autoload singleton: `GameClient` (res://Scripts/GameClient.cs)
- DI bootstrap: logging, options, MapService, Networking, ClientSimulationBuilder
- Fixed-step update loop inside `_Process()` (60Hz)
- Starts networking on ready, polls events and runs ECS pipeline

## Build
```bash
cd Simulation.Client/godot-client
# First restore/build Godot project and referenced libs
dotnet build
```

If you open the project in the Godot editor, it will build C# automatically.

## Run
Open the project in Godot and Play, or run the editor from CLI and press Play.

Expected console output on startup:
- `[GameClient] Initializing...`
- `[GameClient] Initialized and connected (attempting) to server.`

With the server running, you should see network connection logs on both sides and the ECS client systems applying state updates.

## Notes
- Networking settings are set programmatically to `127.0.0.1:7777` with key `MinhaChaveDeProducao` and Authority=Client.
- For a visual demo, add a simple scene and a system that maps Position -> Node2D transform.
- For multi-user or dynamic PlayerId assignment, implement a small handshake packet (AssignPlayer) on connect.
