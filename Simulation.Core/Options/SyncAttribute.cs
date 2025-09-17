// filepath: Simulation.Core/Options/SyncAttribute.cs
using Simulation.Core.Network.Contracts;

namespace Simulation.Core.Options;

/// <summary>
/// Define quem tem autoridade sobre o estado do componente.
/// Server = origem autoritativa normalmente; Client = entrada ou estado local.
/// </summary>
public enum Authority { Server, Client }

/// <summary>
/// Modo de envio (futuro: Unicast ou Broadcast explícito).
/// </summary>
public enum SendMode { Unicast, Broadcast }

/// <summary>
/// Flags que definem quando o componente deve ser sincronizado.
/// Combináveis via bitwise.
/// </summary>
[Flags]
public enum SyncTrigger : byte 
{
    OnChange = 1 << 0, // Envia quando houver mudança detectada
    OnTick   = 1 << 1, // Envia em intervalos de tick (respeitando SyncRateTicks)
    OneShot  = 1 << 2, // Envia uma única vez e remove
}

/// <summary>
/// Atributo aplicado a structs que devem ser sincronizados via rede.
/// Controla autoridade, gatilhos e parâmetros de transmissão.
/// </summary>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class SyncAttribute : Attribute
{
    /// <summary>
    /// Lado que possui a fonte autoritativa do valor (define onde o sender será registrado).
    /// </summary>
    public Authority Authority { get; init; } = Authority.Server;

    /// <summary>
    /// Gatilhos de sincronização combinados (OnChange, OnTick, OneShot).
    /// </summary>
    public SyncTrigger Trigger { get; init; } = SyncTrigger.OnChange;

    /// <summary>
    /// Intervalo em ticks para SyncTrigger.OnTick (0 ou 1 = todo tick).
    /// </summary>
    public ushort SyncRateTicks { get; init; } = 0;

    /// <summary>
    /// Modo de envio planejado (atualmente sem lógica diferenciada implementada).
    /// </summary>
    public SendMode SendMode { get; init; } = SendMode.Unicast;

    /// <summary>
    /// Método de entrega na camada de transporte.
    /// </summary>
    public NetworkDeliveryMethod DeliveryMethod { get; init; } = NetworkDeliveryMethod.ReliableOrdered;
}
