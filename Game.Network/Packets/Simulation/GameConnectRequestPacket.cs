using Game.ECS.Components;
using Game.Network.Abstractions;
using MemoryPack;

namespace Game.Network.Packets.Simulation;

/// <summary>
/// Client -> Server: Conecta ao jogo com token (CONNECTED).
/// Autor: MonoDevPro
/// Data: 2025-01-12 06:37:40
/// </summary>
[MemoryPackable]
public readonly partial record struct GameConnectRequestPacket(string GameToken);
