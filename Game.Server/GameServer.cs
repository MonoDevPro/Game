using Game.Abstractions;
using Game.Domain.Enums;
using Game.Domain.VOs;
using Game.Network.Abstractions;
using Game.Network.Packets;
using Game.Network.Packets.DTOs;
using Game.Server.Authentication;
using Game.Server.Players;
using Game.Server.Security;
using Game.Server.Sessions;
using Game.Server.Simulation;

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
        RegisterAndValidate<CharacterCreationRequestPacket>(HandleCharacterCreationRequest);
        RegisterAndValidate<CharacterSelectionRequestPacket>(ProcessCharacterSelectionAsync);
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

        if (_simulation.TryApplyPlayerInput(session.Entity, packet.MoveX, packet.MoveY, packet.Buttons))
            _logger.LogDebug("Applied input from peer {PeerId}: MoveX={MoveX}, MoveY={MoveY}, Buttons={Buttons}", peer.Id, packet.MoveX, packet.MoveY, packet.Buttons);
        
        _logger.LogInformation("Processed input from peer {PeerId}: MoveX={MoveX}, MoveY={MoveY}, Buttons={Buttons}", peer.Id, packet.MoveX, packet.MoveY, packet.Buttons);
    }

    private async Task ProcessLoginAsync(INetPeerAdapter peer, LoginRequestPacket packet)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var loginService = scope.ServiceProvider.GetRequiredService<AccountLoginService>();

            var loginResult = await loginService.AuthenticateAsync(packet.Username, packet.Password, CancellationToken.None);

            if (!loginResult.Success || loginResult.Account is null)
            {
                var response = LoginResponsePacket.Failure(loginResult.Message);
                _networkManager.SendToPeer(peer, response, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
                return;
            }

            var session = new PlayerSession(peer, loginResult.Account, loginResult.Characters);
            if (!_sessionManager.TryAddSession(session, out var error))
            {
                _spawnService.DespawnPlayer(session);
                var response = LoginResponsePacket.Failure(error ?? "Falha ao criar sessão.");
                _networkManager.SendToPeer(peer, response, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
                return;
            }
            
            var playerCharacters = loginResult.Characters.Select(c => new PlayerCharData
            {
                Id = c.Id,
                Name = c.Name,
                Level = c.Stats?.Level ?? 1,
                Vocation = c.Vocation,
                Gender = c.Gender
            }).ToArray();
            
            var successResponse = LoginResponsePacket.SuccessResponse(playerCharacters);
            _networkManager.SendToPeer(peer, successResponse, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            
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
            var loginResult = await loginService.AuthenticateAsync(packet.Username, packet.Password, CancellationToken.None);

            if (!loginResult.Success || loginResult.Account is null)
            {
                var response = LoginResponsePacket.Failure(loginResult.Message);
                _networkManager.SendToPeer(peer, response, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
                return;
            }
            
            var session = new PlayerSession(peer, loginResult.Account, loginResult.Characters);
            
            if (!_sessionManager.TryAddSession(session, out var error))
            {
                _spawnService.DespawnPlayer(session);
                var response = LoginResponsePacket.Failure(error ?? "Falha ao criar sessão.");
                _networkManager.SendToPeer(peer, response, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
                return;
            }
            
            var playerCharacters = loginResult.Characters.Select(c => new PlayerCharData
            {
                Id = c.Id,
                Name = c.Name,
                Level = c.Stats?.Level ?? 1,
                Vocation = c.Vocation,
                Gender = c.Gender
            }).ToArray();
            
            var successResponse = LoginResponsePacket.SuccessResponse(playerCharacters);
            _networkManager.SendToPeer(peer, successResponse, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar registro para peer {PeerId}", peer.Id);
            var response = RegistrationResponsePacket.Failure("Erro interno no servidor.");
            _networkManager.SendToPeer(peer, response, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        }
    }
    
    private void HandleCharacterCreationRequest(INetPeerAdapter peer, CharacterCreationRequestPacket requestPacket)
    {
        _ = ProcessCharacterCreationAsync(peer, requestPacket);
    }

    private async Task ProcessCharacterCreationAsync(INetPeerAdapter peer, CharacterCreationRequestPacket requestPacket)
    {
        if (!_sessionManager.TryGetByPeer(peer, out var session) || session is null)
        {
            _logger.LogWarning("Received CreateCharacter request from unknown peer {PeerId}", peer.Id);
            var response = CharacterCreationResponsePacket.Failure("Sessão não encontrada.");
            _networkManager.SendToPeer(peer, response, NetworkChannel.Simulation,
                NetworkDeliveryMethod.ReliableOrdered);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var characterService = scope.ServiceProvider.GetRequiredService<AccountCharacterService>();
        var creationResult = await characterService.CreateCharacterAsync( 
            new AccountCharacterService.CharacterInfo(
                requestPacket.Name,
                1,
                requestPacket.Vocation,
                requestPacket.Gender
            ), CancellationToken.None);

        if (!creationResult.Success)
        {
            var response = CharacterCreationResponsePacket.Failure(creationResult.Message);
            _networkManager.SendToPeer(peer, response, NetworkChannel.Simulation,
                NetworkDeliveryMethod.ReliableOrdered);
            return;
        }
        session.Account.Characters.Add(creationResult.Character!);
        var charData = new PlayerCharData
        {
            Id = creationResult.Character!.Id,
            Name = creationResult.Character.Name,
            Level = creationResult.Character.Stats?.Level ?? 1,
            Vocation = creationResult.Character.Vocation,
            Gender = creationResult.Character.Gender
        };
        var successResponse = CharacterCreationResponsePacket.Ok(charData);
        _networkManager.SendToPeer(peer, successResponse, NetworkChannel.Simulation,
            NetworkDeliveryMethod.ReliableOrdered);
        _logger.LogInformation("Character created: {CharacterName} (peer {PeerId})", creationResult.Character.Name, peer.Id);
    }
    
    private void HandleCharacterSelection(INetPeerAdapter peer, CharacterSelectionRequestPacket requestPacket)
    {
        ProcessCharacterSelectionAsync(peer, requestPacket);
    }

    private void ProcessCharacterSelectionAsync(INetPeerAdapter peer, CharacterSelectionRequestPacket requestPacket)
    {
        if (!_sessionManager.TryGetByPeer(peer, out var session) || session is null)
        {
            _logger.LogWarning("Received SelectCharacter request from unknown peer {PeerId}", peer.Id);
            var response = LoginResponsePacket.Failure("Sessão não encontrada.");
            _networkManager.SendToPeer(peer, response, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            return;
        }
        
        if (session.SelectedCharacter is not null)
        {
            _logger.LogWarning("Peer {PeerId} attempted to select a character but already has one selected", peer.Id);
            var response = LoginResponsePacket.Failure("Personagem já selecionado.");
            _networkManager.SendToPeer(peer, response, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            return;
        }
        
        var characterId = requestPacket.CharacterId;
        var character = session.Account.Characters.FirstOrDefault(c => c.Id == characterId);
        
        if (character is null)
        {
            _logger.LogWarning("Character {CharacterId} not found for account {AccountId}", characterId, session.Account.Id);
            var response = LoginResponsePacket.Failure("Personagem não encontrado.");
            _networkManager.SendToPeer(peer, response, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            return;
        }
        
        session.SelectedCharacter = character;
        var responsePacket = CharacterSelectionResponsePacket.Ok(character.Id);
        _networkManager.SendToPeer(peer, responsePacket, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        
        _spawnService.SpawnPlayer(session);
        var localSnapshot = _spawnService.BuildSnapshot(session);
        var othersSnapshots = _sessionManager
            .GetSnapshotExcluding(peer.Id)
            .Select(existing => _spawnService.BuildSnapshot(existing))
            .ToArray();
        var spawnPacket = new PlayerSpawnPacket(localSnapshot);
        _networkManager.SendToAllExcept(peer, spawnPacket, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        
        
        var mapService = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<MapService>();
        var gameDataPacket = new GameDataPacket
        {
            MapData = new MapData
            {
                Name = mapService.Name,
                Width = mapService.Width,
                Height = mapService.Height,
                TileData = mapService.Tiles,
                CollisionData = mapService.CollisionMask
            },
            LocalPlayer = localSnapshot,
            OtherPlayers = othersSnapshots
        };
        _networkManager.SendToPeer(peer, gameDataPacket, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        
        _logger.LogInformation("Player Selected Character {} (peer {PeerId})", character.Name, peer.Id);
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
        _networkManager.UnregisterPacketHandler<CharacterCreationRequestPacket>();
        _networkManager.UnregisterPacketHandler<CharacterSelectionRequestPacket>();
        _networkManager.UnregisterPacketHandler<PlayerInputPacket>();
    }

    private async Task PersistCharacterAsync(PlayerSession session, Coordinate position, DirectionEnum facing)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var persistence = scope.ServiceProvider.GetRequiredService<PlayerPersistenceService>();
            if (session.SelectedCharacter is null)
                return;

            session.SelectedCharacter.PositionX = position.X;
            session.SelectedCharacter.PositionY = position.Y;
            session.SelectedCharacter.DirectionEnum = facing;
            session.SelectedCharacter.LastUpdatedAt = DateTime.UtcNow;

            if (_simulation.TryGetPlayerVitals(session.Entity, out var vitals))
            {
                session.SelectedCharacter.Stats.CurrentHp = vitals.CurrentHp;
                session.SelectedCharacter.Stats.CurrentMp = vitals.CurrentMp;
            }

            await persistence.PersistAsync(session.SelectedCharacter, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist character {Character}", session.SelectedCharacter?.Name);
        }
    }
}