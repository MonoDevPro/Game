using System.Runtime.InteropServices;
using Game.ECS.Shared.Components.Entities;
using MemoryPack;

namespace Game.DTOs.Menu;

[MemoryPackable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly partial record struct CharMenuData(
    int Id,
    string Name,
    int Level,
    VocationType Vocation,
    Gender Gender);