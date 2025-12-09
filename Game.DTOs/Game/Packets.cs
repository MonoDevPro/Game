using Game.DTOs.Game.Map;
using Game.DTOs.Game.Npc;
using Game.DTOs.Game.Player;
using MemoryPack;

namespace Game.DTOs.Game;

[MemoryPackable]
public readonly partial record struct InputPacket(InputData Input);

[MemoryPackable]
public readonly partial record struct StatePacket(PositionStateData[] States);

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