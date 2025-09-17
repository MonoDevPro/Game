using System.Reflection;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Systems;
using Simulation.Core.Network.Contracts;
using Simulation.Core.Options;

namespace Simulation.Core.ECS.Sync;

public static class SyncAutoRegistrar
{
    /// <summary>
    /// Registra automaticamente sistemas de envio (ComponentSenderSystem) ou recepção (ComponentReceiverSystem)
    /// para todos os componentes marcados com <see cref="SyncAttribute"/>.
    /// A lógica baseia-se na autoridade declarada no atributo e no lado atual (server/client):
    ///  - Se Authority == Server: o servidor envia e o cliente recebe.
    ///  - Se Authority == Client: o cliente envia e o servidor recebe.
    /// </summary>
    /// <param name="group">Grupo de sistemas alvo.</param>
    /// <param name="provider">Service provider para obter dependências (World, Endpoint, Logger).</param>
    /// <param name="isServer">Define se o lado atual é o servidor.</param>
    public static void RegisterAttributedSyncSystems<TGroup>(this TGroup group, IServiceProvider provider, bool isServer = true)
        where TGroup : Group<float>
    {
        // Index necessário apenas para sistemas que recebem (para mapear PlayerId -> Entity)
        var index = group.Get<IndexSystem>();
        var world = provider.GetRequiredService<World>();
        var endpoint = provider.GetRequiredService<IChannelEndpoint>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("SyncAutoRegistrar");

        foreach (var (componentType, attribute) in ScanComponentsWithSyncAttribute())
        {
            var authorityIsServer = attribute.Authority == Authority.Server;

            // Decide o papel (Sender / Receiver) para este lado.
            var registerSender = (isServer && authorityIsServer) || (!isServer && !authorityIsServer); // lado que detém a autoridade envia
            var role = registerSender ? "Sender" : "Receiver";

            try
            {
                ISystem<float> systemInstance;
                if (registerSender)
                {
                    // Cria ComponentSenderSystem<T>
                    var senderGeneric = typeof(ComponentSenderSystem<>).MakeGenericType(componentType);
                    systemInstance = (ISystem<float>)Activator.CreateInstance(senderGeneric, world, endpoint, attribute)!;
                }
                else
                {
                    // Cria ComponentReceiverSystem<T>
                    var receiverGeneric = typeof(ComponentReceiverSystem<>).MakeGenericType(componentType);
                    systemInstance = (ISystem<float>)Activator.CreateInstance(receiverGeneric, world, endpoint, index)!;
                }

                group.Add(systemInstance);
                logger.LogInformation("[SyncAuto] Registrado {Role} para componente {Component} (Authority={Authority}, Lado={Side})", role, componentType.Name, attribute.Authority, isServer ? "Server" : "Client");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SyncAuto] Falha ao registrar {Role} para componente {Component}", role, componentType.Name);
            }
        }
    }

    private static IEnumerable<(Type Component, SyncAttribute Attribute)> ScanComponentsWithSyncAttribute()
    {
        var asm = typeof(SyncAutoRegistrar).Assembly;
        foreach (var t in asm.GetTypes())
        {
            if (!t.IsValueType || t.IsPrimitive) continue; // somente structs / records structs não primitivos
            var attr = t.GetCustomAttribute<SyncAttribute>();
            if (attr is null) continue;
            var equatable = typeof(IEquatable<>).MakeGenericType(t);
            if (!equatable.IsAssignableFrom(t)) continue; // exige IEquatable<T> para comparação eficiente
            yield return (t, attr);
        }
    }
}