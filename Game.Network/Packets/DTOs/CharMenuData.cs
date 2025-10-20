using System.Runtime.InteropServices;
using Game.Domain.Enums;
using MemoryPack;

namespace Game.Network.Packets.DTOs;

[MemoryPackable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly partial record struct CharMenuData(
    int Id,
    string Name,
    int Level,
    VocationType Vocation,
    Gender Gender);