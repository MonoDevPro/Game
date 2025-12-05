using Game.ECS.Schema.Components;
using MemoryPack;

namespace Game.Network.Packets.Game;

[MemoryPackable]
public readonly partial record struct InputPacket(InputData Input);

[MemoryPackable]
public readonly partial record struct StatePacket(StateData[] States);

[MemoryPackable]
public readonly partial record struct VitalsPacket(VitalsData[] Vitals);

[MemoryPackable]
public readonly partial record struct LeftPacket(int[] NetworkIds);

[MemoryPackable]
public readonly partial record struct PlayerJoinPacket(MapDataPacket MapDataPacket, PlayerData LocalPlayer);

[MemoryPackable]
public readonly partial record struct PlayerSpawnPacket(PlayerData[] PlayerData);

[MemoryPackable]
public readonly partial record struct NpcSpawnPacket(NpcData[] Npcs);