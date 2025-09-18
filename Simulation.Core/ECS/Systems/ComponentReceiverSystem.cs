using Arch.Core;
using Arch.System;
using Simulation.Core.ECS.Indexes;
using Simulation.Core.Network.Contracts;
using Simulation.Core.Options;

namespace Simulation.Core.ECS.Systems;

/// <summary>
/// Sistema genérico responsável exclusivamente por receber da rede as atualizações
/// de um componente <typeparamref name="T"/> sincronizado e aplicá-las à entidade
/// correspondente no <see cref="World"/>.
/// 
/// NOTA: Este sistema é registrado automaticamente apenas no lado oposto
/// ao que detém a <see cref="SyncAttribute.Authority"/>. Ex.:
///  - Se Authority = Server, este sistema roda no Client (para receber do servidor)
///  - Se Authority = Client, este sistema roda no Server (para receber do cliente)
/// </summary>
/// <typeparam name="T">Tipo de componente sincronizado. Deve ser struct e IEquatable para comparação eficiente.</typeparam>
public class ComponentReceiverSystem<T> : BaseSystem<World, float> where T : struct, IEquatable<T>
{
    /// <summary>
    /// Construtor registra um handler de rede para <see cref="ComponentSyncPacket{T}"/>.
    /// Ao receber um pacote, localiza a entidade do jogador e aplica/insere o componente.
    /// </summary>
    /// <param name="world">Instância do mundo ECS.</param>
    /// <param name="channelEndpoint">Canal de rede para registrar o handler.</param>
    /// <param name="index">Índice para mapear PlayerId -> Entity.</param>
    public ComponentReceiverSystem(World world, IChannelEndpoint channelEndpoint, IPlayerIndex index) 
        : base(world)
    {
        // Registra callback reativo: nenhuma lógica é feita em Update.
        channelEndpoint.RegisterHandler<ComponentSyncPacket<T>>((peer, packet) =>
        {
            if (!index.TryGetPlayerEntity(packet.PlayerId, out var playerEntity)) 
                return;
            
            Console.WriteLine($"[Receiver] Recebido {typeof(T).Name} para PlayerId {packet.PlayerId} (Entity {playerEntity})");
            
            // Garante que o componente exista e aplica o valor recebido.
            ref var component = ref world.AddOrGet<T>(playerEntity);
            component = packet.Data;
        });
    }

    /// <summary>
    /// Sistema é reativo (event-driven). Método de atualização fica vazio.
    /// </summary>
    public override void Update(in float t) { }
}
