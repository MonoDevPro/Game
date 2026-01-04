using Game.DTOs.Map;
using Game.DTOs.Npc;
using Game.DTOs.Player;
using MemoryPack;

namespace Game.DTOs;

[MemoryPackable]
public readonly partial record struct InputPacket(InputData Input);

[MemoryPackable]
public readonly partial record struct StatePacket(StateData[] States);

[MemoryPackable]
public readonly partial record struct VitalsPacket(VitalsData[] Vitals);

[MemoryPackable]
public readonly partial record struct LeftPacket(int[] NetworkIds);

[MemoryPackable]
public readonly partial record struct PlayerJoinPacket(MapData MapData, PlayerData LocalPlayer);

[MemoryPackable]
public readonly partial record struct PlayerSpawnPacket(PlayerData[] PlayerData);

[MemoryPackable]
public readonly partial record struct NpcSpawnPacket(NpcData[] Npcs);

[MemoryPackable]
public readonly partial record struct AttackPacket(AttackData[] Attacks);