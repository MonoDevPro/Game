using Game.DTOs.Map;
using Game.DTOs.Npc;
using Game.DTOs.Player;
using MemoryPack;

namespace Game.DTOs;

[MemoryPackable]
public readonly partial record struct InputPacket(InputRequest Input);

[MemoryPackable]
public readonly partial record struct StatePacket(StateSnapshot[] States);

[MemoryPackable]
public readonly partial record struct VitalsPacket(VitalsSnapshot[] Vitals);

[MemoryPackable]
public readonly partial record struct LeftPacket(int[] NetworkIds);

[MemoryPackable]
public readonly partial record struct PlayerJoinPacket(MapMetadataDto MapData, PlayerSnapshot LocalPlayer);

[MemoryPackable]
public readonly partial record struct PlayerSpawnPacket(PlayerSnapshot[] PlayerData);

[MemoryPackable]
public readonly partial record struct NpcSpawnPacket(NpcData[] Npcs);

[MemoryPackable]
public readonly partial record struct AttackPacket(AttackSnapshot[] Attacks);