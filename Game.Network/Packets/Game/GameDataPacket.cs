using System.Runtime.InteropServices;
using Game.Domain.Enums;
using Game.ECS.Entities.Factories;
using MemoryPack;

namespace Game.Network.Packets.Game;

[MemoryPackable]
public readonly partial record struct GameDataPacket(
    MapDto MapDto,
    PlayerSnapshot LocalPlayer,
    PlayerSnapshot[] OtherPlayers);