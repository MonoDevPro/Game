using MemoryPack;

namespace Game.DTOs;

[MemoryPackable]
public readonly partial record struct LeftPacket(int[] NetworkIds);