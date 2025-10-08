using Game.Abstractions.Network;
using Game.Core;
using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.Domain.VOs;
using Game.Network.Packets;
using Game.Server.Authentication;
using Game.Server.Players;
using Game.Server.Security;
using Game.Server.Sessions;

namespace Game.Server;

/// <summary>
/// High-level orchestration for networking, authentication and player lifecycle.
/// </summary>
public sealed class GameServer : IDisposable
{
    private readonly INetworkManager _networkManager;
    private readonly PlayerSessionManager _sessionManager;
    private readonly PlayerSpawnService _spawnService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GameServer> _logger;
    private readonly GameSimulation _simulation;
    private readonly NetworkSecurity? _security;

    private bool _disposed;

    public GameServer(
        INetworkManager networkManager,
        PlayerSessionManager sessionManager,
        PlayerSpawnService spawnService,
        IServiceScopeFactory scopeFactory,
        GameSimulation simulation,
        ILogger<GameServer> logger,
        NetworkSecurity? security = null)
    {
        _networkManager = networkManager;
        _sessionManager = sessionManager;
        _spawnService = spawnService;
        _scopeFactory = scopeFactory;
        _simulation = simulation;
        _logger = logger;
        _security = security;

        _networkManager.OnPeerConnected += OnPeerConnected;
        _networkManager.OnPeerDisconnected += OnPeerDisconnected;

        RegisterAndValidate<LoginRequestPacket>(HandleLoginRequest);
        RegisterAndValidate<RegistrationRequestPacket>(HandleRegistrationRequest);
        RegisterAndValidate<PlayerInputPacket>(HandlePlayerInput);
    }
    
    private void RegisterAndValidate<T>(PacketHandler<T> handler) where T : struct, IPacket
    {
        if (_security is not null)
        {
            _networkManager.RegisterPacketHandler<T>(WrappedHandler);
            return;
        }
        _networkManager.RegisterPacketHandler<T>(handler);
        return;
        
        void WrappedHandler(INetPeerAdapter peer, T packet)
        {
            if (_disposed)
                return;
            if (_security is null)
            {
                handler(peer, packet);
                return;
            }
                
            if (_security.ValidateMessage(peer, packet))
            {
                handler(peer, packet);
            }
            else
            {
                _logger.LogWarning("Invalid message from peer {PeerId}, disconnecting", peer.Id);
                peer.Disconnect();
            }
        }
    }

    public void Start()
    {
        if (_networkManager.IsRunning)
            return;

        _networkManager.Start();
        _logger.LogInformation("Network server started");
    }

    public void Stop()
    {
        if (!_networkManager.IsRunning)
            return;

        _networkManager.Stop();
        _logger.LogInformation("Network server stopped");
    }

    private void OnPeerConnected(INetPeerAdapter peer)
    {
        _logger.LogInformation("Peer connected: {PeerId}", peer.Id);
    }

    private void OnPeerDisconnected(INetPeerAdapter peer)
    {
        _logger.LogInformation("Peer disconnected: {PeerId}", peer.Id);

        if (_sessionManager.TryRemoveByPeer(peer, out var session))
        {
            if (session is not null && _simulation.TryGetPlayerState(session.Entity, out var position, out var direction))
                _ = PersistCharacterAsync(session, position, direction);

            if (session is not null)
                _spawnService.DespawnPlayer(session);
            
            _security?.RemovePeer(peer);

            var packet = new PlayerDespawnPacket(peer.Id);
            _networkManager.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        }
    }

    private void HandleLoginRequest(INetPeerAdapter peer, LoginRequestPacket packet)
    {
        _ = ProcessLoginAsync(peer, packet);
    }

    private void HandlePlayerInput(INetPeerAdapter peer, PlayerInputPacket packet)
    {
        if (!_sessionManager.TryGetByPeer(peer, out var session) || session is null)
        {
            _logger.LogWarning("Received PlayerInputPacket from unknown peer {PeerId}", peer.Id);
            return;
        }

        if (packet.Sequence <= session.LastInputSequence)
        {
            _logger.LogWarning("Out-of-order input packet from peer {PeerId}: {PacketSequence} <= {LastSequence}", peer.Id, packet.Sequence, session.LastInputSequence);
            return;
        }

        if (_simulation.TryApplyPlayerInput(session.Entity, packet.MoveX, packet.MoveY, packet.Buttons, packet.Sequence))
        {
            _logger.LogDebug("Applied input from peer {PeerId}: Seq={Sequence}, MoveX={MoveX}, MoveY={MoveY}, Buttons={Buttons}", peer.Id, packet.Sequence, packet.MoveX, packet.MoveY, packet.Buttons);
            session.LastInputSequence = packet.Sequence;
        }
        
        _logger.LogTrace("Processed input from peer {PeerId}: Seq={Sequence}, MoveX={MoveX}, MoveY={MoveY}, Buttons={Buttons}", peer.Id, packet.Sequence, packet.MoveX, packet.MoveY, packet.Buttons);
    }

    private async Task ProcessLoginAsync(INetPeerAdapter peer, LoginRequestPacket packet)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var loginService = scope.ServiceProvider.GetRequiredService<AccountLoginService>();

            var loginResult = await loginService.AuthenticateAsync(packet.Username, packet.Password, packet.CharacterName, CancellationToken.None);

            if (!loginResult.Success || loginResult.Account is null || loginResult.Character is null)
            {
                var response = LoginResponsePacket.Failure(loginResult.Message);
                _networkManager.SendToPeer(peer, response, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
                return;
            }

            if (!TryCompleteLogin(peer, loginResult.Account, loginResult.Character))
            {
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar login para peer {PeerId}", peer.Id);
            var response = LoginResponsePacket.Failure("Erro interno no servidor.");
            _networkManager.SendToPeer(peer, response, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        }
    }

    private void HandleRegistrationRequest(INetPeerAdapter peer, RegistrationRequestPacket packet)
    {
        _ = ProcessRegistrationAsync(peer, packet);
    }

    private async Task ProcessRegistrationAsync(INetPeerAdapter peer, RegistrationRequestPacket packet)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var registrationService = scope.ServiceProvider.GetRequiredService<AccountRegistrationService>();

            var registrationResult = await registrationService.RegisterAsync(
                packet.Username,
                packet.Email,
                packet.Password,
                packet.CharacterName,
                packet.Gender,
                packet.Vocation,
                CancellationToken.None);

            if (!registrationResult.IsSuccess)
            {
                var failure = RegistrationResponsePacket.Failure(registrationResult.Message);
                _networkManager.SendToPeer(peer, failure, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
                return;
            }

            var success = RegistrationResponsePacket.Ok();
            _networkManager.SendToPeer(peer, success, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);

            var loginService = scope.ServiceProvider.GetRequiredService<AccountLoginService>();
            var loginResult = await loginService.AuthenticateAsync(packet.Username, packet.Password, packet.CharacterName, CancellationToken.None);

            if (!loginResult.Success || loginResult.Account is null || loginResult.Character is null)
            {
                var response = LoginResponsePacket.Failure(loginResult.Message);
                _networkManager.SendToPeer(peer, response, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
                return;
            }

            if (!TryCompleteLogin(peer, loginResult.Account, loginResult.Character))
            {
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar registro para peer {PeerId}", peer.Id);
            var response = RegistrationResponsePacket.Failure("Erro interno no servidor.");
            _networkManager.SendToPeer(peer, response, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        }
    }

    private bool TryCompleteLogin(INetPeerAdapter peer, Account account, Character character)
    {
        var session = new PlayerSession(peer, account, character);
        _spawnService.SpawnPlayer(session);

        if (!_sessionManager.TryAddSession(session, out var error))
        {
            _spawnService.DespawnPlayer(session);
            var response = LoginResponsePacket.Failure(error ?? "Falha ao criar sessão.");
            _networkManager.SendToPeer(peer, response, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            return false;
        }

        var localSnapshot = _spawnService.BuildSnapshot(session);
        var othersSnapshots = _sessionManager
            .GetSnapshotExcluding(peer.Id)
            .Select(existing => _spawnService.BuildSnapshot(existing))
            .ToList();

        var successResponse = LoginResponsePacket.SuccessResponse(localSnapshot, othersSnapshots.ToArray());
        _networkManager.SendToPeer(peer, successResponse, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);

        var spawnPacket = new PlayerSpawnPacket(localSnapshot);
        _networkManager.SendToAllExcept(peer, spawnPacket, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);

        _logger.LogInformation("Player {Character} authenticated and spawned (peer {PeerId})", character.Name, peer.Id);
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _networkManager.OnPeerConnected -= OnPeerConnected;
        _networkManager.OnPeerDisconnected -= OnPeerDisconnected;
        _networkManager.UnregisterPacketHandler<LoginRequestPacket>();
        _networkManager.UnregisterPacketHandler<RegistrationRequestPacket>();
        _networkManager.UnregisterPacketHandler<PlayerInputPacket>();
    }

    private async Task PersistCharacterAsync(PlayerSession session, Coordinate position, DirectionEnum facing)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var persistence = scope.ServiceProvider.GetRequiredService<PlayerPersistenceService>();

            session.Character.PositionX = position.X;
            session.Character.PositionY = position.Y;
            session.Character.DirectionEnum = facing;
            session.Character.LastUpdatedAt = DateTime.UtcNow;

            if (session.Character.Stats is not null && _simulation.TryGetPlayerVitals(session.Entity, out var vitals))
            {
                session.Character.Stats.CurrentHp = vitals.CurrentHp;
                session.Character.Stats.CurrentMp = vitals.CurrentMp;
            }

            await persistence.PersistAsync(session.Character, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist character {Character}", session.Character.Name);
        }
    }
}
