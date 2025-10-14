using System.Runtime.InteropServices;
using Game.Abstractions;
using Game.Domain.VOs;

namespace Game.ECS.Components;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PlayerInput
{
    public GridOffset Movement; // -1 a 1
    public GridOffset MouseLook;     // Mouse look Position relative from center
    public InputFlags Flags;     // Botões pressionados
    public uint Sequence;        // Sequência do input para reconciliação
}