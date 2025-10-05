using System.Buffers;
using System.Runtime.InteropServices;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Abstractions.Network;
using Game.ECS.Components;
using MemoryPack;

namespace Game.ECS.Systems;

/// <summary>
/// Sistema de sincronização de rede otimizado
/// Autor: MonoDevPro
/// Data: 2025-10-05 23:24:10
/// </summary>
public sealed partial class NetworkSyncSystem(World world, INetworkManager network) : GameSystem(world)
{
    // Pooling
    private TransformState[] _stateBuffer = ArrayPool<TransformState>.Shared.Rent(256);
    private int _stateCount = 0;
    
    // Throttling
    private float _timeSinceLastSync;
    private const float SyncInterval = 1f / 20f; // 20 Hz
    
    // Tick tracking
    private uint _currentTick = 0;
    
    // Stats (debug)
    private int _totalSyncedThisFrame;

    [Query]
    [All<NetworkId, NetworkSync, Position, Rotation>]
    [None<Dead>] // Não sincronizar mortos
    private void GatherDirtyStates(
        ref NetworkId netId, 
        ref NetworkSync sync, 
        ref Position pos, 
        ref Rotation rot)
    {
        if (!sync.IsDirty)
            return;

        // Validar posição
        if (!IsValidPosition(pos.Value))
        {
            sync.IsDirty = false;
            return;
        }

        // Se não especificou flags, sincronizar tudo
        if (sync.Flags == SyncFlags.None)
            sync.Flags = SyncFlags.All;

        // Sincronizar Position e Rotation
        if ((sync.Flags & (SyncFlags.Position | SyncFlags.Rotation)) != 0)
        {
            // Expandir buffer se necessário
            if (_stateCount >= _stateBuffer.Length)
            {
                var newBuffer = ArrayPool<TransformState>.Shared.Rent(_stateBuffer.Length * 2);
                Array.Copy(_stateBuffer, newBuffer, _stateCount);
                ArrayPool<TransformState>.Shared.Return(_stateBuffer);
                _stateBuffer = newBuffer;
            }

            _stateBuffer[_stateCount++] = new TransformState
            {
                NetworkId = netId.Value,
                Position = pos.Value,
                Rotation = CompressRotation(rot.Value)
            };

            // Marcar como sincronizado
            sync.Flags &= ~(SyncFlags.Position | SyncFlags.Rotation);
        }

        // TODO: Implementar sync de Health, Animation, etc

        // Marcar como limpo se processou todas as flags
        if (sync.Flags == SyncFlags.None)
        {
            sync.IsDirty = false;
        }

        sync.LastUpdateTick = _currentTick;
    }

    public override void BeforeUpdate(in float deltaTime)
    {
        _timeSinceLastSync += deltaTime;
        _totalSyncedThisFrame = 0;
    }

    public override void AfterUpdate(in float deltaTime)
    {
        // Throttling - enviar apenas a cada SyncInterval
        if (_stateCount > 0 && _timeSinceLastSync >= SyncInterval)
        {
            SendSnapshot();
            _timeSinceLastSync = 0f;
            _currentTick++;
        }

        base.AfterUpdate(in deltaTime);
    }

    private void SendSnapshot()
    {
        var snapshot = new WorldSnapshotMessage
        {
            Tick = _currentTick,
            States = _stateBuffer.AsSpan(0, _stateCount).ToArray()
        };

        network.SendToAll(
            snapshot,
            NetworkChannel.Simulation,
            NetworkDeliveryMethod.Sequenced); // Sequenced = mais recente descarta antiga

        _totalSyncedThisFrame = _stateCount;
        _stateCount = 0; // Reset buffer
    }

    private bool IsValidPosition(Coordinate pos)
    {
        const int maxCoord = 10000;
        return pos.X is >= -maxCoord and <= maxCoord &&
               pos.Y is >= -maxCoord and <= maxCoord;
    }

    private short CompressRotation(int rotation)
    {
        // Assumindo rotação 0-360
        return (short)(rotation % 360);
    }

    public override void Dispose()
    {
        ArrayPool<TransformState>.Shared.Return(_stateBuffer);
        base.Dispose();
    }
}

// Network/Messages/WorldSnapshotMessage.cs
[MemoryPackable]
public partial struct WorldSnapshotMessage : IPacket
{
    public uint Tick;
    public TransformState[] States;
}

[MemoryPackable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct TransformState
{
    public int NetworkId;
    public Coordinate Position;
    public short Rotation;
}