using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using MemoryPack;
using Simulation.Core.ECS.Components;
using Simulation.Core.Network.Contracts;
using Simulation.Core.Options;

namespace Simulation.Core.ECS.Systems;

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
/// no <see cref="SyncAttribute"/> associado ao tipo:
///  - <see cref="SyncTrigger.OnChange"/>: envia somente quando detecta diferença em relação ao Shadow.
///  - <see cref="SyncTrigger.OnTick"/>: envia a cada N ticks (SyncRateTicks) ou todo tick se 0/1.
///  - <see cref="SyncTrigger.OneShot"/>: envia uma vez e remove o componente.
/// 
/// Este sistema é registrado automaticamente somente no lado que detém a Authority declarada.
/// </summary>
/// <typeparam name="T">Tipo de componente sincronizado.</typeparam>
public class ComponentSenderSystem<T> : BaseSystem<World, float> where T : struct, IEquatable<T>
{
    private readonly IChannelEndpoint _channelEndpoint;
    private readonly SyncAttribute _attribute;
    private ulong _tickCounter = 0;

    // Queries reutilizadas para minimizar custo.
    private readonly Query _onChangeEnsureShadow;
    private readonly Query _onChangeQuery;
    private readonly Query _onTickQuery;
    private readonly Query _oneShotQuery;

    /// <summary>
    /// Constrói o sistema configurando queries conforme a composição de entidades.
    /// </summary>
    public ComponentSenderSystem(World world, IChannelEndpoint channelEndpoint, SyncAttribute attribute) : base(world)
    {
        _channelEndpoint = channelEndpoint;
        _attribute = attribute;

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

        if (_attribute.Trigger.HasFlag(SyncTrigger.OneShot))  SyncOneShot();
        if (_attribute.Trigger.HasFlag(SyncTrigger.OnChange)) EnsureShadowExists(); SyncOnChange();
        if (_attribute.Trigger.HasFlag(SyncTrigger.OnTick))   SyncOnTick();
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

                SendComponentUpdate(playerId.Value, component, _attribute.DeliveryMethod);
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
        if (_attribute.SyncRateTicks > 1 && _tickCounter % _attribute.SyncRateTicks != 0) 
            return;
        
        foreach (ref var chunk in _onTickQuery.GetChunkIterator())
        {
            ref var playerIdFirstElement = ref chunk.GetFirst<PlayerId>();
            ref var componentFirstElement = ref chunk.GetFirst<T>();

            foreach (var entityIndex in chunk)
            {
                ref var playerId = ref Unsafe.Add(ref playerIdFirstElement, entityIndex);
                ref var component = ref Unsafe.Add(ref componentFirstElement, entityIndex);

                SendComponentUpdate(playerId.Value, component, _attribute.DeliveryMethod);
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

                SendComponentUpdate(playerId.Value, component, _attribute.DeliveryMethod);
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
        switch (_attribute.Target)
        {
            // Envia apenas para o jogador dono da entidade
            case SyncTarget.Unicast:
                if (_attribute.Authority == Authority.Client)
                    _channelEndpoint.SendToServer(packet, method);
                else
                    _channelEndpoint.SendToPeerId(playerId, packet, method);
                break;

            // Envia para todos os jogadores (comportamento padrão)
            case SyncTarget.Broadcast:
            default:
                _channelEndpoint.SendToAll(packet, method);
                break;
        }
    }
}
