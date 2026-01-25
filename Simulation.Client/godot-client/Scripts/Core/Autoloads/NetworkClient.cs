using System;
using Game.Contracts;
using Godot;
using LiteNetLib;
using LiteNetLib.Utils;
using MemoryPack;
using Microsoft.Extensions.Logging;

namespace GodotClient.Core.Autoloads;

/// <summary>
/// Gerenciador de rede do client Godot.
/// Inicializa e gerencia o ciclo de vida do sistema de networking do jogo.
/// Autor: MonoDevPro
/// Data: 2025-01-11 14:36:00
/// </summary>
public partial class NetworkClient : Node
{
    private static NetworkClient? _instance;

    public static NetworkClient Instance =>
        _instance ?? throw new InvalidOperationException("NetworkClient not initialized");

    private ILogger<NetworkClient>? _logger;
    private NetworkConfiguration _config = null!;

    public NetClientConnection AuthConnection { get; private set; } = null!;
    public NetClientConnection WorldConnection { get; private set; } = null!;
    public NetClientConnection ChatConnection { get; private set; } = null!;

    public override void _Ready()
    {
        base._Ready();
        _instance = this;

        _logger = ServicesManager.Instance.GetRequiredService<ILogger<NetworkClient>>();
        _config = ConfigManager.Instance.Configuration.Network ?? new NetworkConfiguration();

        AuthConnection = CreateConnection();
        WorldConnection = CreateConnection();
        ChatConnection = CreateConnection();
    }

    /// <summary>
    /// Inicia o cliente de rede.
    /// </summary>
    public void Start()
    {
        _logger?.LogInformation("Starting network client");
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        AuthConnection.PollEvents();
        WorldConnection.PollEvents();
        ChatConnection.PollEvents();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        AuthConnection.Dispose();
        WorldConnection.Dispose();
        ChatConnection.Dispose();

        _logger?.LogInformation("Stopping network client");
    }

    private NetClientConnection CreateConnection()
    {
        return new NetClientConnection(_config.ConnectionKey, _config.PingIntervalMs, _config.DisconnectTimeoutMs);
    }
}

public sealed class NetClientConnection : IDisposable
{
    private readonly EventBasedNetListener _listener;
    private readonly NetManager _manager;
    private readonly string _connectionKey;
    private NetPeer? _peer;

    public event Action? Connected;
    public event Action<DisconnectInfo>? Disconnected;
    public event Action<Envelope>? EnvelopeReceived;

    public NetClientConnection(string connectionKey, int pingIntervalMs, int disconnectTimeoutMs)
    {
        _connectionKey = connectionKey;
        _listener = new EventBasedNetListener();
        _manager = new NetManager(_listener)
        {
            AutoRecycle = true,
            PingInterval = pingIntervalMs,
            DisconnectTimeout = disconnectTimeoutMs
        };

        _listener.PeerConnectedEvent += peer =>
        {
            _peer = peer;
            Connected?.Invoke();
        };
        _listener.PeerDisconnectedEvent += (peer, info) =>
        {
            if (_peer == peer)
                _peer = null;
            Disconnected?.Invoke(info);
        };
        _listener.NetworkReceiveEvent += OnReceive;
    }

    public bool IsConnected => _peer is not null && _peer.ConnectionState == ConnectionState.Connected;

    public bool Connect(string host, int port)
    {
        if (IsConnected)
            return true;

        if (!_manager.IsRunning)
            _manager.Start();

        _peer = _manager.Connect(host, port, _connectionKey);
        return _peer is not null;
    }

    public void Disconnect()
    {
        _peer?.Disconnect();
        _peer = null;
    }

    public void Send(Envelope envelope, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
    {
        if (_peer is null)
            return;

        var data = MemoryPackSerializer.Serialize(envelope);
        var writer = new NetDataWriter();
        writer.Put(data);
        _peer.Send(writer, deliveryMethod);
    }

    public void PollEvents()
    {
        _manager.PollEvents();
    }

    private void OnReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        var payload = reader.GetRemainingBytes();
        reader.Recycle();

        try
        {
            var envelope = MemoryPackSerializer.Deserialize<Envelope>(payload);
            EnvelopeReceived?.Invoke(envelope);
        }
        catch
        {
            // Ignore malformed packets to avoid crashing the client.
        }
    }

    public void Dispose()
    {
        _manager.Stop();
    }
}
