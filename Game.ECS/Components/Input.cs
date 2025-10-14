using System.Runtime.InteropServices;
using Game.Abstractions;
using Game.Domain.VOs;

namespace Game.ECS.Components;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PlayerInput
{
    public DirectionOffset Movement; // -1 a 1
    public DirectionOffset MouseLook;     // Mouse look Position relative from center
    public InputFlags Flags;     // Botões pressionados
    public uint Sequence;        // Sequência do input para reconciliação
}