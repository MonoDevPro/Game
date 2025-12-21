using Game.ECS.Shared.Core.Entities;
using Game.ECS.Shared.Core.Navigation;
using MemoryPack;

namespace Game.ECS.Shared.Data.Entities;

[MemoryPackable]
public readonly partial record struct PlayerJoinPacket(MapData MapData, PlayerData LocalPlayer);

[MemoryPackable]
public readonly partial record struct PlayerSpawnPacket(PlayerData[] PlayerData);

[MemoryPackable]
public readonly partial record struct LeftPacket(int[] Ids);

[MemoryPackable]
public readonly partial record struct NpcSpawnPacket(NpcData[] Npcs);
