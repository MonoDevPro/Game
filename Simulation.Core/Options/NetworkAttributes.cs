// filepath: Simulation.Core/Options/SyncAttribute.cs

using Simulation.Core.Network.Contracts;

namespace Simulation.Core.Options;

// Nova enum para o alvo do envio
public enum SyncTarget : byte
{
    Broadcast, // Envia para todos (padrão)
    Unicast    // Envia apenas para o dono da entidade
}

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
[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class SyncAttribute : Attribute
{
    public Authority Authority { get; init; } = Authority.Server;
    public SyncTrigger Trigger { get; init; } = SyncTrigger.OnChange;
    public SyncTarget Target { get; init; } = SyncTarget.Broadcast; // Definindo um padrão explícito
    public ushort SyncRateTicks { get; init; } = 0;
    public NetworkDeliveryMethod DeliveryMethod { get; init; } = NetworkDeliveryMethod.ReliableOrdered;
}
