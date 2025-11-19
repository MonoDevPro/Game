using Game.ECS.Components;
using Game.ECS.Entities.Factories;
using MemoryPack;

namespace Game.Network.Packets.Game;

[MemoryPackable]
public readonly partial record struct PlayerJoinPacket(MapDataPacket MapDataPacket, PlayerSnapshot LocalPlayer, PlayerSnapshot[] OtherPlayers);

[MemoryPackable]
public readonly partial record struct PlayerSpawnPacket(PlayerSnapshot[] PlayerData);

[MemoryPackable]
public readonly partial record struct PlayerStatePacket(PlayerStateSnapshot[] States);

[MemoryPackable]
public readonly partial record struct PlayerVitalsPacket(PlayerVitalsSnapshot[] Vitals);

[MemoryPackable]
public readonly partial record struct PlayerInputPacket(sbyte InputX, sbyte InputY, InputFlags Flags);
    
[MemoryPackable]
public readonly partial record struct LeftPacket(int NetworkId);