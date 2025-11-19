using MemoryPack;

namespace Game.Network.Packets.Game;

[MemoryPackable]
public readonly partial record struct NpcSpawnPacket(NpcSpawnSnapshot[] Npcs);

[MemoryPackable]
public readonly partial record struct NpcDespawnPacket(int NetworkId);

[MemoryPackable]
public readonly partial record struct NpcStatePacket(NpcStateSnapshot[] States);
