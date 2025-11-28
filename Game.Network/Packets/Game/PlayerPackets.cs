using Game.ECS.Components;
using MemoryPack;

namespace Game.Network.Packets.Game;

[MemoryPackable]
public readonly partial record struct PlayerJoinPacket(MapDataPacket MapDataPacket, PlayerSpawn LocalPlayer, PlayerSpawn[] OtherPlayers);

[MemoryPackable]
public readonly partial record struct PlayerSpawnPacket(PlayerSpawn[] PlayerData);

[MemoryPackable]
public readonly partial record struct PlayerStatePacket(PlayerStateUpdate[] States);

[MemoryPackable]
public readonly partial record struct PlayerVitalsPacket(PlayerVitalsUpdate[] Vitals);

[MemoryPackable]
public readonly partial record struct PlayerInputPacket(sbyte InputX, sbyte InputY, InputFlags Flags);
    
[MemoryPackable]
public readonly partial record struct LeftPacket(int NetworkId);