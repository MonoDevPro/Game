using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using MemoryPack;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Resource;
using Simulation.Core.Options;
using Simulation.Core.Ports.Network;

namespace Simulation.Core.ECS.Sync;

/// <summary>
/// Estrutura sombra para armazenar o último valor enviado de um componente
/// a fim de detectar mudanças (caso o gatilho seja OnChange).
/// </summary>
/// <typeparam name="T">Tipo do componente sincronizado.</typeparam>
internal readonly record struct Shadow<T>(T Value) where T : struct, IEquatable<T>;

/// <summary>
/// Pacote de sincronização para um componente específico <typeparamref name="T"/>.
/// Contém o identificador do jogador, o tick em que foi capturado e os dados.
/// </summary>
/// <typeparam name="T">Tipo do componente sincronizado.</typeparam>
[MemoryPackable]
public readonly partial record struct ComponentSyncPacket<T>(int PlayerId, ulong Tick, T Data) : IPacket 
    where T : struct, IEquatable<T>;

/// <summary>
/// Sistema que envia para a rede valores de componentes <typeparamref name="T"/> baseando-se
/// no <see cref="SyncOptions"/> associado ao tipo:
///  - <see cref="SyncFrequency.OnChange"/>: envia somente quando detecta diferença em relação ao Shadow.
///  - <see cref="SyncFrequency.OnTick"/>: envia a cada N ticks (SyncRateTicks) ou todo tick se 0/1.
///  - <see cref="SyncFrequency.OneShot"/>: envia uma vez e remove o componente.
/// 
/// Este sistema é registrado automaticamente somente no lado que detém a Authority declarada.
/// </summary>
/// <typeparam name="T">Tipo de componente sincronizado.</typeparam>
public class NetworkOutbox<T> : BaseSystem<World, float> where T : struct, IEquatable<T>
{
    private readonly PlayerNetResource _resource;
    private readonly SyncOptions _options;
    private ulong _tickCounter = 0;

    // Queries reutilizadas para minimizar custo.
    private readonly Query _onChangeEnsureShadow;
    private readonly Query _onChangeQuery;
    private readonly Query _onTickQuery;
    private readonly Query _oneShotQuery;

    /// <summary>
    /// Constrói o sistema configurando queries conforme a composição de entidades.
    /// </summary>
    public NetworkOutbox(World world, PlayerNetResource resource, SyncOptions options) : base(world)
    {
        _resource = resource;
        _options = options;

        _onChangeEnsureShadow = world.Query(new QueryDescription().WithAll<PlayerId, T>().WithNone<Shadow<T>>());
        _onChangeQuery = world.Query(new QueryDescription().WithAll<PlayerId, T, Shadow<T>>());
        _onTickQuery   = world.Query(new QueryDescription().WithAll<PlayerId, T>());
        _oneShotQuery  = world.Query(new QueryDescription().WithAll<PlayerId, T>());
    }
    
    /// <summary>
    /// Incrementa o contador de ticks e executa os modos de sincronização configurados.
    /// </summary>
    public override void Update(in float t)
    {
        _tickCounter++;

        if (_options.Frequency.HasFlag(SyncFrequency.OneShot))  SyncOneShot();
        if (_options.Frequency.HasFlag(SyncFrequency.OnChange)) EnsureShadowExists(); SyncOnChange();
        if (_options.Frequency.HasFlag(SyncFrequency.OnTick))   SyncOnTick();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureShadowExists()
    {
        foreach (ref var chunk in _onChangeEnsureShadow.GetChunkIterator())
        {
            ref var entityFirstElement = ref chunk.Entity(0);
            ref var playerIdFirstElement = ref chunk.GetFirst<PlayerId>();
            ref var componentFirstElement = ref chunk.GetFirst<T>();

            foreach (var entityIndex in chunk)
            {
                ref readonly var entity = ref Unsafe.Add(ref entityFirstElement, entityIndex);
                ref var playerId = ref Unsafe.Add(ref playerIdFirstElement, entityIndex);
                ref var component = ref Unsafe.Add(ref componentFirstElement, entityIndex);
                World.Add<Shadow<T>>(entity, new Shadow<T>(component));
            }
        }
    }

    /// <summary>
    /// Envia apenas quando o valor atual difere da sombra.
    /// </summary>
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

                SendComponentUpdate(playerId.Value, component, _options.DeliveryMethod);
                shadow = new Shadow<T>(component);
            }
        }
    }

    /// <summary>
    /// Envia em intervalos fixos definidos por SyncRateTicks (ou sempre se 0/1).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SyncOnTick()
    {
        if (_options.SyncRateTicks > 1 && _tickCounter % _options.SyncRateTicks != 0) 
            return;
        
        foreach (ref var chunk in _onTickQuery.GetChunkIterator())
        {
            ref var playerIdFirstElement = ref chunk.GetFirst<PlayerId>();
            ref var componentFirstElement = ref chunk.GetFirst<T>();

            foreach (var entityIndex in chunk)
            {
                ref var playerId = ref Unsafe.Add(ref playerIdFirstElement, entityIndex);
                ref var component = ref Unsafe.Add(ref componentFirstElement, entityIndex);

                SendComponentUpdate(playerId.Value, component, _options.DeliveryMethod);
            }
        }
    }

    /// <summary>
    /// Envia uma vez e remove o componente da entidade (one-shot).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SyncOneShot()
    {
        foreach (ref var chunk in _oneShotQuery.GetChunkIterator())
        {
            ref var entityFirstElement = ref chunk.Entity(0);
            ref var playerIdFirstElement = ref chunk.GetFirst<PlayerId>();
            ref var componentFirstElement = ref chunk.GetFirst<T>();

            foreach (var entityIndex in chunk)
            {
                ref readonly var entity = ref Unsafe.Add(ref entityFirstElement, entityIndex);
                ref var playerId = ref Unsafe.Add(ref playerIdFirstElement, entityIndex);
                ref var component = ref Unsafe.Add(ref componentFirstElement, entityIndex);

                SendComponentUpdate(playerId.Value, component, _options.DeliveryMethod);
                World.Remove<T>(entity);
            }
        }
    }
    
    /// <summary>
    /// Cria o pacote e envia via endpoint usando o método de entrega definido.
    /// </summary>
    /// <summary>
    /// Método auxiliar para criar e enviar o pacote, respeitando o SyncTarget.
    /// </summary>
    private void SendComponentUpdate(int playerId, T component, NetworkDeliveryMethod method)
    {
        var packet = new ComponentSyncPacket<T>(playerId, _tickCounter, component);

        // Decide como enviar com base no atributo
        switch (_options.Target)
        {
            // Envia apenas para o jogador dono da entidade
            case SyncTarget.Unicast:
                if (_options.Authority == Authority.Client)
                    _resource.SendToServer(packet, method);
                else
                    _resource.SendToPlayer(playerId, packet, method);
                break;

            // Envia para todos os jogadores (comportamento padrão)
            case SyncTarget.Broadcast:
            default:
                _resource.BroadcastToAll(packet, method);
                break;
        }
    }
}
