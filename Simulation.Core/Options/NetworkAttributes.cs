// filepath: Simulation.Core/Options/SyncAttribute.cs

using Simulation.Core.Ports.Network;

namespace Simulation.Core.Options;

// Nova enum para o alvo do envio
public enum SyncTarget : byte
{
    Broadcast, // Envia para todos (padrão)
    Unicast,   // Envia apenas para o dono da entidade
    Server,    // Envia apenas para o servidor
}

/// <summary>
/// Flags que definem quando o componente deve ser sincronizado.
/// Combináveis via bitwise.
/// </summary>
[Flags]
public enum SyncFrequency : byte
{
    OnChange = 1 << 0, // Envia quando houver mudança detectada
    OnTick   = 1 << 1, // Envia em intervalos de tick (respeitando SyncRateTicks)
    OneShot  = 1 << 2, // Envia uma única vez e remove
}

/// <summary>
/// Atributo aplicado a structs que devem ser sincronizados via rede.
/// Controla autoridade, gatilhos e parâmetros de transmissão.
/// </summary>
public record SyncOptions(
    SyncFrequency Frequency,
    SyncTarget Target,
    NetworkDeliveryMethod DeliveryMethod,
    NetworkChannel Channel,
    ushort SyncRateTicks);
