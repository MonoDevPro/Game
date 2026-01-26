using System.Buffers.Binary;
using MemoryPack;

namespace Game.Contracts;

public static class EnvelopeSerializer
{
    public static byte[] Serialize(Envelope envelope)
    {
        var payloadBytes = SerializePayload(envelope.OpCode, envelope.Payload);
        var result = new byte[2 + payloadBytes.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(0, 2), (ushort)envelope.OpCode);
        if (payloadBytes.Length > 0)
        {
            Buffer.BlockCopy(payloadBytes, 0, result, 2, payloadBytes.Length);
        }

        return result;
    }

    public static Envelope Deserialize(ReadOnlySpan<byte> data)
    {
        if (data.Length < 2)
            throw new InvalidOperationException("Envelope payload too small.");

        var opCode = (OpCode)BinaryPrimitives.ReadUInt16LittleEndian(data);
        var payloadData = data.Slice(2);
        var payload = DeserializePayload(opCode, payloadData);
        return new Envelope(opCode, payload);
    }

    private static byte[] SerializePayload(OpCode opCode, IEnvelopePayload? payload)
    {
        if (payload is null)
        {
            if (opCode == OpCode.WorldSnapshotRequest)
                return Array.Empty<byte>();

            throw new InvalidOperationException($"Payload required for {opCode}.");
        }

        return opCode switch
        {
            OpCode.AuthLoginRequest => MemoryPackSerializer.Serialize((AuthLoginRequest)payload),
            OpCode.AuthLoginResponse => MemoryPackSerializer.Serialize((AuthLoginResponse)payload),
            OpCode.AuthCharacterListRequest => MemoryPackSerializer.Serialize((CharacterListRequest)payload),
            OpCode.AuthCharacterListResponse => MemoryPackSerializer.Serialize((CharacterListResponse)payload),
            OpCode.AuthSelectCharacterRequest => MemoryPackSerializer.Serialize((SelectCharacterRequest)payload),
            OpCode.AuthSelectCharacterResponse => MemoryPackSerializer.Serialize((SelectCharacterResponse)payload),
            OpCode.WorldEnterRequest => MemoryPackSerializer.Serialize((EnterWorldRequest)payload),
            OpCode.WorldEnterResponse => MemoryPackSerializer.Serialize((EnterWorldResponse)payload),
            OpCode.WorldMoveCommand => MemoryPackSerializer.Serialize((WorldMoveCommand)payload),
            OpCode.WorldNavigateCommand => MemoryPackSerializer.Serialize((WorldNavigateCommand)payload),
            OpCode.WorldStopCommand => MemoryPackSerializer.Serialize((WorldStopCommand)payload),
            OpCode.WorldBasicAttackCommand => MemoryPackSerializer.Serialize((WorldBasicAttackCommand)payload),
            OpCode.WorldSnapshot => MemoryPackSerializer.Serialize((WorldSnapshot)payload),
            OpCode.WorldSnapshotDelta => MemoryPackSerializer.Serialize((WorldSnapshotDelta)payload),
            OpCode.CombatEventBatch => MemoryPackSerializer.Serialize((CombatEventBatch)payload),
            OpCode.ChatSendRequest => MemoryPackSerializer.Serialize((ChatSendRequest)payload),
            OpCode.ChatMessage => MemoryPackSerializer.Serialize((ChatMessage)payload),
            _ => throw new InvalidOperationException($"Unsupported OpCode: {opCode}.")
        };
    }

    private static IEnvelopePayload? DeserializePayload(OpCode opCode, ReadOnlySpan<byte> payloadData)
    {
        if (payloadData.Length == 0 && opCode == OpCode.WorldSnapshotRequest)
            return null;

        return opCode switch
        {
            OpCode.AuthLoginRequest => MemoryPackSerializer.Deserialize<AuthLoginRequest>(payloadData),
            OpCode.AuthLoginResponse => MemoryPackSerializer.Deserialize<AuthLoginResponse>(payloadData),
            OpCode.AuthCharacterListRequest => MemoryPackSerializer.Deserialize<CharacterListRequest>(payloadData),
            OpCode.AuthCharacterListResponse => MemoryPackSerializer.Deserialize<CharacterListResponse>(payloadData),
            OpCode.AuthSelectCharacterRequest => MemoryPackSerializer.Deserialize<SelectCharacterRequest>(payloadData),
            OpCode.AuthSelectCharacterResponse => MemoryPackSerializer.Deserialize<SelectCharacterResponse>(payloadData),
            OpCode.WorldEnterRequest => MemoryPackSerializer.Deserialize<EnterWorldRequest>(payloadData),
            OpCode.WorldEnterResponse => MemoryPackSerializer.Deserialize<EnterWorldResponse>(payloadData),
            OpCode.WorldMoveCommand => MemoryPackSerializer.Deserialize<WorldMoveCommand>(payloadData),
            OpCode.WorldNavigateCommand => MemoryPackSerializer.Deserialize<WorldNavigateCommand>(payloadData),
            OpCode.WorldStopCommand => MemoryPackSerializer.Deserialize<WorldStopCommand>(payloadData),
            OpCode.WorldBasicAttackCommand => MemoryPackSerializer.Deserialize<WorldBasicAttackCommand>(payloadData),
            OpCode.WorldSnapshot => MemoryPackSerializer.Deserialize<WorldSnapshot>(payloadData),
            OpCode.WorldSnapshotDelta => MemoryPackSerializer.Deserialize<WorldSnapshotDelta>(payloadData),
            OpCode.CombatEventBatch => MemoryPackSerializer.Deserialize<CombatEventBatch>(payloadData),
            OpCode.ChatSendRequest => MemoryPackSerializer.Deserialize<ChatSendRequest>(payloadData),
            OpCode.ChatMessage => MemoryPackSerializer.Deserialize<ChatMessage>(payloadData),
            OpCode.WorldSnapshotRequest => null,
            _ => throw new InvalidOperationException($"Unsupported OpCode: {opCode}.")
        };
    }
}
