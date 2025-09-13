using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using LiteNetLib;
using Simulation.Abstractions.Network;
using Simulation.Core.Network;
using Simulation.Core.Options;

// Necessário para Unsafe

namespace Simulation.Core.ECS.Shared.Systems;

/// <summary>
/// Componente interno para rastrear o último estado sincronizado de um componente.
/// </summary>
internal readonly record struct Shadow<T>(T Value) where T : struct, IEquatable<T>;

/// <summary>
/// Um sistema genérico e performático que sincroniza um componente do tipo T,
/// utilizando iteração de chunks de baixo nível para máxima performance.
/// </summary>
/// <typeparam name="T">O tipo do componente a ser sincronizado.</typeparam>
public class GenericSyncSystem<T> : BaseSystem<World, float> where T : struct, IEquatable<T>
{
    private readonly NetworkManager _networkManager;
    private readonly SyncOptions _options;
    private ulong _tickCounter = 0;

    // --- Consultas (Queries) cacheadas para alta performance ---
    private readonly Query _initialStateQuery;
    private readonly Query _onChangeQuery;
    private readonly Query _onTickQuery;
    private readonly Query _clientIntentQuery;

    public GenericSyncSystem(World world, NetworkManager networkManager, SyncOptions options) : base(world)
    {
        _networkManager = networkManager;
        _options = options;
        
        // Criamos as queries uma vez no construtor
        _initialStateQuery = World.Query(new QueryDescription().WithAll<PlayerId, T>().WithNone<Shadow<T>>());
        _onChangeQuery = World.Query(new QueryDescription().WithAll<PlayerId, T, Shadow<T>>());
        _onTickQuery = World.Query(new QueryDescription().WithAll<PlayerId, T>());
        _clientIntentQuery = World.Query(new QueryDescription().WithAll<PlayerId, T>());
    }

    public override void BeforeUpdate(in float t)
    {
        _networkManager.PollEvents();
    }

    public override void Update(in float t)
    {
        _tickCounter++;

        if (_options.Authority == Authority.Server)
        {
            SyncInitialState();

            if (_options.Trigger == SyncTrigger.OnChange)
            {
                SyncOnChange();
            }
            else if (_options.Trigger == SyncTrigger.OnTick)
            {
                SyncOnTick();
            }
        }
        else if (_options.Authority == Authority.Client)
        {
            SyncClientIntent();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SyncInitialState()
    {
        foreach (ref var chunk in _initialStateQuery.GetChunkIterator())
        {
            // Obtém ponteiros para o início dos arrays de componentes no chunk
            ref var entityFirstElement = ref chunk.Entity(0);
            ref var playerIdFirstElement = ref chunk.GetFirst<PlayerId>();
            ref var componentFirstElement = ref chunk.GetFirst<T>();

            foreach (var entityIndex in chunk)
            {
                // Move os ponteiros para a entidade atual
                ref readonly var entity = ref Unsafe.Add(ref entityFirstElement, entityIndex);
                ref var playerId = ref Unsafe.Add(ref playerIdFirstElement, entityIndex);
                ref var component = ref Unsafe.Add(ref componentFirstElement, entityIndex);
                
                _networkManager.SendComponent(playerId.Value, component, DeliveryMethod.ReliableOrdered);
                World.Add<Shadow<T>>(entity, new Shadow<T> { Value = component });
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

                _networkManager.SendComponent(playerId.Value, component, DeliveryMethod.Unreliable);
                shadow = new Shadow<T>(component);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SyncOnTick()
    {
        if (_options.SyncRateTicks > 1 && _tickCounter % _options.SyncRateTicks != 0)
        {
            return;
        }
        
        foreach (ref var chunk in _onTickQuery.GetChunkIterator())
        {
            ref var playerIdFirstElement = ref chunk.GetFirst<PlayerId>();
            ref var componentFirstElement = ref chunk.GetFirst<T>();

            foreach (var entityIndex in chunk)
            {
                ref var playerId = ref Unsafe.Add(ref playerIdFirstElement, entityIndex);
                ref var component = ref Unsafe.Add(ref componentFirstElement, entityIndex);

                _networkManager.SendComponent(playerId.Value, component, DeliveryMethod.Unreliable);
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

                _networkManager.SendComponent(playerId.Value, component, DeliveryMethod.ReliableOrdered);
                World.Remove<T>(entity);
            }
        }
    }
}