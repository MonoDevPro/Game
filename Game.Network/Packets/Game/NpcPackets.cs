using MemoryPack;

namespace Game.Network.Packets.Game;

[MemoryPackable]
public readonly partial record struct NpcSpawnPacket(NpcSpawnRequest[] Npcs);

[MemoryPackable]
public readonly partial record struct NpcDespawnPacket(int[] NetworkIds);

[MemoryPackable]
public readonly partial record struct NpcStatePacket(NpcStateUpdate[] States);

[MemoryPackable]
public readonly partial record struct NpcHealthPacket(NpcVitalsUpdate[] Healths);