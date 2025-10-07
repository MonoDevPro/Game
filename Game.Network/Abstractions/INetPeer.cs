using System.Net;

namespace Game.Abstractions.Network;

/// <summary>
/// Define um contrato para um repositório que armazena e gere os peers conectados.
/// Atua como a única fonte de verdade sobre quem está online.
/// </summary>
public interface IPeerRepository
{
    /// <summary>
    /// Tenta obter um peer através do seu ID.
    /// </summary>
    /// <param name="peerId">O ID do peer a ser procurado.</param>
    /// <param name="peer">O peer encontrado, se existir.</param>
    /// <returns>True se o peer for encontrado, caso contrário, false.</returns>
    bool TryGetPeer(int peerId, out INetPeerAdapter? peer);

    /// <summary>
    /// Obtém uma coleção de todos os peers atualmente conectados.
    /// </summary>
    IEnumerable<INetPeerAdapter> GetAllPeers();

    /// <summary>
    /// O número de peers atualmente conectados.
    /// </summary>
    int PeerCount { get; }
}

/// <summary>
/// Abstração mínima sobre LiteNetLib.NetPeer para reduzir acoplamento direto.
/// </summary>
public interface INetPeerAdapter
{
    int Id { get; }
    IPEndPoint EndPoint { get; }
    int Ping { get; }
    int RoundTripTime { get; }
    int Mtu { get; }
    bool IsConnected { get; }
    object Tag { get; set; }

    int GetPacketsCountInReliableQueue(byte channelNumber, bool ordered);
    int GetMaxSinglePacketSize(NetworkDeliveryMethod method);

    // Sends
    void Send(byte[] data, NetworkDeliveryMethod networkDeliveryMethod);
    void Send(byte[] data, int start, int length, NetworkDeliveryMethod networkDeliveryMethod);
    void Send(byte[] data, byte channelNumber, NetworkDeliveryMethod networkDeliveryMethod);
    void Send(ReadOnlySpan<byte> data, NetworkDeliveryMethod networkDeliveryMethod);
    void Send(ReadOnlySpan<byte> data, byte channelNumber, NetworkDeliveryMethod networkDeliveryMethod);

    void SendWithDeliveryEvent(byte[] data, byte channelNumber, NetworkDeliveryMethod networkDeliveryMethod, object userData);
    void SendWithDeliveryEvent(ReadOnlySpan<byte> data, byte channelNumber, NetworkDeliveryMethod networkDeliveryMethod, object userData);

    // Disconnects
    void Disconnect();
    void Disconnect(byte[] data);
    void Disconnect(byte[] data, int start, int count);
}