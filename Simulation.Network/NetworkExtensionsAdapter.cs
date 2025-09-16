using LiteNetLib;
using Simulation.Core.Network.Contracts;

namespace Simulation.Network;

/// <summary>
/// Extensões e mapeamentos entre o enum local DeliveryMethod e o LiteNetLib.DeliveryMethod.
/// </summary>
public static class DeliveryMethodAdapter
{
    public static DeliveryMethod ToLite(this NetworkDeliveryMethod dm)
    {
        return dm switch
        {
            NetworkDeliveryMethod.ReliableUnordered => LiteNetLib.DeliveryMethod.ReliableUnordered,
            NetworkDeliveryMethod.Sequenced => LiteNetLib.DeliveryMethod.Sequenced,
            NetworkDeliveryMethod.ReliableOrdered => LiteNetLib.DeliveryMethod.ReliableOrdered,
            NetworkDeliveryMethod.ReliableSequenced => LiteNetLib.DeliveryMethod.ReliableSequenced,
            NetworkDeliveryMethod.Unreliable => LiteNetLib.DeliveryMethod.Unreliable,
            _ => throw new ArgumentOutOfRangeException(nameof(dm), dm, "Unknown DeliveryMethod")
        };
    }

    public static NetworkDeliveryMethod FromLite(this LiteNetLib.DeliveryMethod lite)
    {
        return lite switch
        {
            LiteNetLib.DeliveryMethod.ReliableUnordered => NetworkDeliveryMethod.ReliableUnordered,
            LiteNetLib.DeliveryMethod.Sequenced => NetworkDeliveryMethod.Sequenced,
            LiteNetLib.DeliveryMethod.ReliableOrdered => NetworkDeliveryMethod.ReliableOrdered,
            LiteNetLib.DeliveryMethod.ReliableSequenced => NetworkDeliveryMethod.ReliableSequenced,
            LiteNetLib.DeliveryMethod.Unreliable => NetworkDeliveryMethod.Unreliable,
            _ => throw new ArgumentOutOfRangeException(nameof(lite), lite, "Unknown LiteNetLib.DeliveryMethod")
        };
    }

    /// <summary>
    /// Retorna se o método é considerado "reliable" (não sujeito a perda).
    /// </summary>
    public static bool IsReliable(this NetworkDeliveryMethod dm)
    {
        return dm == NetworkDeliveryMethod.ReliableUnordered
               || dm == NetworkDeliveryMethod.ReliableOrdered
               || dm == NetworkDeliveryMethod.ReliableSequenced;
    }

    /// <summary>
    /// Retorna se o método garante ordem.
    /// </summary>
    public static bool IsOrdered(this NetworkDeliveryMethod dm)
    {
        return dm == NetworkDeliveryMethod.ReliableOrdered
               || dm == NetworkDeliveryMethod.ReliableSequenced
               || dm == NetworkDeliveryMethod.Sequenced;
    }

    /// <summary>
    /// Retorna se o método permite fragmentação (nem todos permitem).
    /// De acordo com a implementação do LiteNetLib, apenas ReliableOrdered e ReliableUnordered podem ser fragmentados.
    /// </summary>
    public static bool AllowsFragmentation(this NetworkDeliveryMethod dm)
    {
        return dm == NetworkDeliveryMethod.ReliableOrdered
               || dm == NetworkDeliveryMethod.ReliableUnordered;
    }
}