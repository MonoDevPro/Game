using System.Runtime.InteropServices;
using Game.Abstractions.Network;
using Game.ECS.Components;
using MemoryPack;

namespace Game.ECS.Messages;



[MemoryPackable] [StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PlayerInputMessage : IPacket
{
    public Coordinate Movement; // Movimento em X,Y
    public Coordinate Look; // Olhar em X,Y
    public InputFlags Flags; // Flags de input (ataque, interagir, correr, etc)
    public uint SequenceNumber; // Número de sequência para ordenação
}