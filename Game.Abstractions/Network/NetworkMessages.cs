using System.Runtime.InteropServices;

namespace Game.Network.Messages;

// Mensagem de input (otimizada para tamanho)
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct InputMessage
{
    public uint SequenceNumber;
    public short MovementX; // -1000 a 1000 (dividir por 1000)
    public short MovementY;
    public short LookX;
    public short LookY;
    public ushort Flags; // InputFlags
}

// Snapshot compacto de entidade
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct EntitySnapshot
{
    public uint NetworkId;
    public float PositionX;
    public float PositionY;
    public byte SyncFlags;
}