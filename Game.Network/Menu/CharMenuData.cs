using System.Runtime.InteropServices;
using Game.Domain.Enums;
using MemoryPack;

namespace Game.DTOs.Menu;

[MemoryPackable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly partial record struct CharMenuData(
    int Id,
    string Name,
    int Level,
    [property: MemoryPackAllowSerialize] VocationType Vocation,
    [property: MemoryPackAllowSerialize] GenderType Gender);