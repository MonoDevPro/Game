using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using MemoryPack;
using Simulation.Core.Network.Contracts;
using Simulation.Core.Options;

namespace Simulation.Core.ECS.Shared.Systems;

internal readonly record struct Shadow<T>(T Value) where T : struct, IEquatable<T>;

[MemoryPackable]
public readonly partial record struct ComponentSyncPacket<T>(int PlayerId, ulong Tick, T Data) : IPacket 
    where T : struct, IEquatable<T>;

public class GenericSyncSystem<T> : BaseSystem<World, float> where T : struct, IEquatable<T>
{
    private readonly IChannelEndpoint _channelEndpoint;
    private readonly SyncOptions _options;
    private ulong _tickCounter = 0;

    // --- Queries (inalteradas, já estavam ótimas) ---
    private readonly Query _initialStateQuery;
    private readonly Query _onChangeQuery;
    private readonly Query _onTickQuery;
    private readonly Query _clientIntentQuery;

    // O construtor agora só precisa do que é essencial para a sua lógica.
    public GenericSyncSystem(World world, IChannelEndpoint channelEndpoint, SyncOptions options) : base(world)
    {
        _channelEndpoint = channelEndpoint;
        _options = options;

        _initialStateQuery = world.Query(new QueryDescription().WithAll<PlayerId, T>().WithNone<Shadow<T>>());
        _onChangeQuery = world.Query(new QueryDescription().WithAll<PlayerId, T, Shadow<T>>());
        _onTickQuery = world.Query(new QueryDescription().WithAll<PlayerId, T>());
        _clientIntentQuery = world.Query(new QueryDescription().WithAll<PlayerId, T>());
    }
    
    public override void Update(in float t)
    {
        _tickCounter++;

        if (_options.Authority == Authority.Server)
        {
            SyncInitialState();
            if (_options.Trigger == SyncTrigger.OnChange) SyncOnChange();
            else if (_options.Trigger == SyncTrigger.OnTick) SyncOnTick();
        }
        else if (_options.Authority == Authority.Client)
        {
            SyncClientIntent();
        }
    }
    
    // Método auxiliar para criar e enviar o pacote
    private void SendComponentUpdate(int playerId, T component, NetworkDeliveryMethod method)
    {
        var packet = new ComponentSyncPacket<T>(playerId, _tickCounter, component);
        _channelEndpoint.SendToAll(packet, method);
    }
    
    private void SendClientIntentUpdate(int playerId, T component, NetworkDeliveryMethod method)
    {
        var packet = new ComponentSyncPacket<T>(playerId, _tickCounter, component);
        _channelEndpoint.SendToServer(packet, method);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SyncInitialState()
    {
        foreach (ref var chunk in _initialStateQuery.GetChunkIterator())
        {
            ref var entityFirstElement = ref chunk.Entity(0);
            ref var playerIdFirstElement = ref chunk.GetFirst<PlayerId>();
            ref var componentFirstElement = ref chunk.GetFirst<T>();

            foreach (var entityIndex in chunk)
            {
                ref readonly var entity = ref Unsafe.Add(ref entityFirstElement, entityIndex);
                ref var playerId = ref Unsafe.Add(ref playerIdFirstElement, entityIndex);
                ref var component = ref Unsafe.Add(ref componentFirstElement, entityIndex);
                
                // O estado inicial deve ser enviado de forma confiável (Reliable).
                SendComponentUpdate(playerId.Value, component, NetworkDeliveryMethod.ReliableOrdered);
                World.Add<Shadow<T>>(entity, new Shadow<T>(component));
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SyncOnChange()
    {
        foreach (ref var chunk in _onChangeQuery.GetChunkIterator())
        {
            ref var playerIdFirstElement = ref chunk.GetFirst<PlayerId>();
            ref var componentFirstElement = ref chunk.GetFirst<T>();
            ref var shadowFirstElement = ref chunk.GetFirst<Shadow<T>>();

            foreach (var entityIndex in chunk)
            {
                ref var playerId = ref Unsafe.Add(ref playerIdFirstElement, entityIndex);
                ref var component = ref Unsafe.Add(ref componentFirstElement, entityIndex);
                ref var shadow = ref Unsafe.Add(ref shadowFirstElement, entityIndex);

                if (component.Equals(shadow.Value)) 
                    continue;

                SendComponentUpdate(playerId.Value, component, NetworkDeliveryMethod.Unreliable);
                shadow = new Shadow<T>(component);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SyncOnTick()
    {
        if (_options.SyncRateTicks > 1 && _tickCounter % _options.SyncRateTicks != 0) return;
        
        foreach (ref var chunk in _onTickQuery.GetChunkIterator())
        {
            ref var playerIdFirstElement = ref chunk.GetFirst<PlayerId>();
            ref var componentFirstElement = ref chunk.GetFirst<T>();

            foreach (var entityIndex in chunk)
            {
                ref var playerId = ref Unsafe.Add(ref playerIdFirstElement, entityIndex);
                ref var component = ref Unsafe.Add(ref componentFirstElement, entityIndex);

                SendComponentUpdate(playerId.Value, component, NetworkDeliveryMethod.Unreliable);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SyncClientIntent()
    {
        foreach (ref var chunk in _clientIntentQuery.GetChunkIterator())
        {
            ref var entityFirstElement = ref chunk.Entity(0);
            ref var playerIdFirstElement = ref chunk.GetFirst<PlayerId>();
            ref var componentFirstElement = ref chunk.GetFirst<T>();

            foreach (var entityIndex in chunk)
            {
                ref readonly var entity = ref Unsafe.Add(ref entityFirstElement, entityIndex);
                ref var playerId = ref Unsafe.Add(ref playerIdFirstElement, entityIndex);
                ref var component = ref Unsafe.Add(ref componentFirstElement, entityIndex);

                SendClientIntentUpdate(playerId.Value, component, NetworkDeliveryMethod.ReliableOrdered);
                World.Remove<T>(entity);
            }
        }
    }
}