using System.Collections.Concurrent;
using System.Net;
using Game.Abstractions;
using Game.Domain.Enums;
using Game.Domain.VOs;
using Game.Network.Abstractions;
using Game.Network.Packets;
using Game.Network.Packets.DTOs;
using Game.Network.Packets.Simulation;
using Game.Persistence;
using Game.Server.Authentication;
using Game.Server.Players;
using Game.Server.Security;
using Game.Server.Sessions;
using Game.Server.Simulation;
using Microsoft.EntityFrameworkCore;

namespace Game.Server;

/// <summary>
/// High-level orchestration for networking, authentication and player lifecycle.
/// Autor: MonoDevPro
/// Data: 2025-01-12 06:04:47
/// </summary>
public sealed class GameServer : IDisposable
{
    /// <summary>
    /// Representa uma conexão pendente aguardando autenticação.
    /// </summary>
    private sealed class PendingConnection
    {
        public INetPeerAdapter Peer { get; init; } = null!;
        public DateTime ConnectedAt { get; init; }
        public CancellationTokenSource TimeoutCts { get; init; } = new();
    }
    
    private readonly INetworkManager _networkManager;
    private readonly PlayerSessionManager _sessionManager;
    private readonly PlayerSpawnService _spawnService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GameServer> _logger;
    private readonly GameSimulation _simulation;
    private readonly NetworkSecurity? _security;
    private readonly SessionTokenManager _tokenManager;
    private readonly int _connectionTimeoutSeconds;
    
    private readonly ConcurrentDictionary<int, PendingConnection> _pendingConnections = new();

    private bool _disposed;

    public GameServer(
        IConfiguration configuration,
        INetworkManager networkManager,
        PlayerSessionManager sessionManager,
        PlayerSpawnService spawnService,
        IServiceScopeFactory scopeFactory,
        GameSimulation simulation,
        ILogger<GameServer> logger,
        SessionTokenManager tokenManager, // ✅ Injetar
        NetworkSecurity? security = null)
    {
        _networkManager = networkManager;
        _sessionManager = sessionManager;
        _spawnService = spawnService;
        _scopeFactory = scopeFactory;
        _simulation = simulation;
        _logger = logger;
        _security = security;
        _tokenManager = tokenManager;

        _connectionTimeoutSeconds = configuration.GetValue("GameServer:ConnectionTimeoutSeconds", 10);
        
        _networkManager.OnPeerConnected += OnPeerConnected;
        _networkManager.OnPeerDisconnected += OnPeerDisconnected;

        // ✅ UNCONNECTED PACKETS (Menu - sem conexão)
        RegisterUnconnectedAndValidate<UnconnectedLoginRequestPacket>(HandleLoginRequest);
        RegisterUnconnectedAndValidate<UnconnectedRegistrationRequestPacket>(HandleRegistrationRequest);
        RegisterUnconnectedAndValidate<UnconnectedCharacterCreationRequestPacket>(HandleCharacterCreationRequest);
        RegisterUnconnectedAndValidate<UnconnectedCharacterSelectionRequestPacket>(HandleCharacterSelectionRequest);
        RegisterUnconnectedAndValidate<UnconnectedCharacterDeleteRequestPacket>(HandleCharacterDeleteRequest); // ✅ NOVO
        
        // ✅ CONNECTED PACKETS (In-game)
        RegisterAndValidate<GameConnectPacket>(HandleGameConnect);
        RegisterAndValidate<PlayerInputPacket>(HandlePlayerInput);
    }

    
    /// <summary>
    /// Registra handler UNCONNECTED com validação de segurança.
    /// </summary>
    private void RegisterUnconnectedAndValidate<T>(UnconnectedPacketHandler<T> handler) where T : struct, IPacket
    {
        if (_security is not null)
        {
            _networkManager.RegisterUnconnectedPacketHandler<T>(WrappedHandler);
            return;
        }
        
        _networkManager.RegisterUnconnectedPacketHandler<T>(handler);
        return;
        
        void WrappedHandler(IPEndPoint remoteEndPoint, T packet)
        {
            if (_disposed)
                return;
            
            if (_security is null)
            {
                handler(remoteEndPoint, packet);
                return;
            }
                
            if (_security.ValidateUnconnectedMessage(remoteEndPoint, packet))
            {
                handler(remoteEndPoint, packet);
            }
            else
            {
                _logger.LogWarning("Invalid unconnected message from endpoint {Endpoint}, ignoring", remoteEndPoint);
            }
        }
    }
    
    /// <summary>
    /// Registra handler CONNECTED com validação de segurança.
    /// </summary>
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

        _networkManager.Initialize();
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
        _logger.LogInformation("Peer connected: {PeerId} - Waiting for GameConnectPacket (timeout: {Timeout}s)", 
            peer.Id, _connectionTimeoutSeconds);
    
        // ✅ Adiciona à lista de conexões pendentes
        var pending = new PendingConnection
        {
            Peer = peer,
            ConnectedAt = DateTime.UtcNow
        };

        _pendingConnections[peer.Id] = pending;

        // ✅ Inicia timeout de 10 segundos
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_connectionTimeoutSeconds), pending.TimeoutCts.Token);

                // ✅ Se chegou aqui, timeout expirou
                if (_pendingConnections.TryRemove(peer.Id, out _))
                {
                    _logger.LogWarning(
                        "Peer {PeerId} did not authenticate within {Timeout} seconds - Disconnecting",
                        peer.Id,
                        _connectionTimeoutSeconds
                    );

                    peer.Disconnect();
                }
            }
            catch (TaskCanceledException)
            {
                // ✅ Timeout foi cancelado (peer autenticou com sucesso)
                _logger.LogDebug("Authentication timeout cancelled for peer {PeerId} (authenticated successfully)", peer.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in authentication timeout for peer {PeerId}", peer.Id);
            }
        }, pending.TimeoutCts.Token);
    }

    private void OnPeerDisconnected(INetPeerAdapter peer)
    {
        _logger.LogInformation("Peer disconnected: {PeerId}", peer.Id);

        // ✅ Remove da lista de pendentes e cancela timeout
        if (_pendingConnections.TryRemove(peer.Id, out var pending))
        {
            pending.TimeoutCts.Cancel();
            pending.TimeoutCts.Dispose();
            _logger.LogDebug("Pending connection removed for peer {PeerId}", peer.Id);
        }

        // ✅ Remove sessão de jogo (se existir)
        if (_sessionManager.TryRemoveByPeer(peer, out var session))
        {
            if (session is not null && session.SelectedCharacter is not null)
            {
                // ✅ Persistir dados de desconexão (leve e rápido)
                if (_simulation.TryGetPlayerState(session.Entity, out var position, out var direction))
                {
                    int? currentHp = null;
                    int? currentMp = null;

                    // ✅ Tentar obter vitals da simulação
                    if (_simulation.TryGetPlayerVitals(session.Entity, out var vitals))
                    {
                        currentHp = vitals.CurrentHp;
                        currentMp = vitals.CurrentMp;
                    }

                    // ✅ Persistir de forma assíncrona (fire-and-forget com tratamento de erro)
                    _ = PersistDisconnectDataAsync(
                        session.SelectedCharacter.Id,
                        position,
                        direction,
                        currentHp,
                        currentMp);
                }
                else
                {
                    _logger.LogWarning(
                        "Could not get player state for character {CharacterId} on disconnect",
                        session.SelectedCharacter.Id);
                }

                // ✅ Despawn do player
                _spawnService.DespawnPlayer(session);
            }
    
            // ✅ Remove peer da segurança
            _security?.RemovePeer(peer);

            // ✅ Notifica outros jogadores sobre o despawn
            var packet = new PlayerDespawnPacket(peer.Id);
            _networkManager.SendToAll(
                packet, 
                NetworkChannel.Simulation, 
                NetworkDeliveryMethod.ReliableOrdered);
        
            _logger.LogInformation(
                "Session removed for account {AccountName} (peer {PeerId})",
                session?.Account.Username,
                peer.Id);
        }
    }
    private async Task PersistDisconnectDataAsync(
        int characterId,
        Coordinate position,
        DirectionEnum direction,
        int? currentHp,
        int? currentMp)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var persistence = scope.ServiceProvider.GetRequiredService<PlayerPersistenceService>();
        
            await persistence.PersistDisconnectAsync(
                characterId,
                position.X,
                position.Y,
                direction,
                currentHp,
                currentMp,
                CancellationToken.None);
        
            _logger.LogDebug(
                "Disconnect data persisted successfully for character {CharacterId}",
                characterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, 
                "Failed to persist disconnect data for character {CharacterId}", 
                characterId);
        }
    }

    // ========== UNCONNECTED HANDLERS (Menu - sem conexão) ==========

    /// <summary>
    /// ✅ Handler UNCONNECTED de login.
    /// </summary>
    private void HandleLoginRequest(IPEndPoint remoteEndPoint, UnconnectedLoginRequestPacket packet)
    {
        _ = ProcessLoginAsync(remoteEndPoint, packet);
    }

    /// <summary>
    /// ✅ Handler UNCONNECTED de registro.
    /// </summary>
    private void HandleRegistrationRequest(IPEndPoint remoteEndPoint, UnconnectedRegistrationRequestPacket packet)
    {
        _ = ProcessRegistrationAsync(remoteEndPoint, packet);
    }
    
    /// <summary>
    /// ✅ Handler UNCONNECTED de criação de personagem.
    /// PROBLEMA: Não temos como identificar a conta sem conexão!
    /// SOLUÇÃO: Implementar sistema de tokens de sessão.
    /// </summary>
    private void HandleCharacterCreationRequest(IPEndPoint remoteEndPoint, UnconnectedCharacterCreationRequestPacket packet)
    {
        _ = ProcessCharacterCreationAsync(remoteEndPoint, packet);
    }
    
    /// <summary>
    /// ✅ Handler UNCONNECTED de seleção de personagem.
    /// PROBLEMA: Não temos como identificar a conta sem conexão!
    /// SOLUÇÃO: Implementar sistema de tokens de sessão.
    /// </summary>
    private void HandleCharacterSelectionRequest(IPEndPoint remoteEndPoint, UnconnectedCharacterSelectionRequestPacket packet)
    {
        _ = ProcessCharacterSelectionAsync(remoteEndPoint, packet);
    }
    
    private async Task ProcessLoginAsync(IPEndPoint remoteEndPoint, UnconnectedLoginRequestPacket packet)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var loginService = scope.ServiceProvider.GetRequiredService<AccountLoginService>();

            var loginResult = await loginService.AuthenticateAsync(packet.Username, packet.Password, CancellationToken.None);

            if (!loginResult.Success || loginResult.Account is null)
            {
                var response = UnconnectedLoginResponsePacket.Failure(loginResult.Message);
                _networkManager.SendUnconnected(remoteEndPoint, response);
                return;
            }

            // ✅ Cria token de sessão
            var sessionToken = _tokenManager.CreateSession(loginResult.Account.Id, loginResult.Account);

            var playerCharacters = loginResult.Characters.Select(c => new PlayerCharData
            {
                Id = c.Id,
                Name = c.Name,
                Level = c.Stats?.Level ?? 1,
                Vocation = c.Vocation,
                Gender = c.Gender
            }).ToArray();
        
            var successResponse = UnconnectedLoginResponsePacket.SuccessResponse(sessionToken, playerCharacters);
            _networkManager.SendUnconnected(remoteEndPoint, successResponse);
        
            _logger.LogInformation("Unconnected login successful for {Username} from {EndPoint} (Token: {Token})", 
                packet.Username, remoteEndPoint, sessionToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing unconnected login from {EndPoint}", remoteEndPoint);
            var response = UnconnectedLoginResponsePacket.Failure("Erro interno no servidor.");
            _networkManager.SendUnconnected(remoteEndPoint, response);
        }
    }

    private async Task ProcessRegistrationAsync(IPEndPoint remoteEndPoint, UnconnectedRegistrationRequestPacket packet)
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
                var failure = UnconnectedRegistrationResponsePacket.Failure(registrationResult.Message);
                _networkManager.SendUnconnected(remoteEndPoint, failure);
                return;
            }

            var success = UnconnectedRegistrationResponsePacket.Ok();
            _networkManager.SendUnconnected(remoteEndPoint, success);

            // ✅ Auto-login após registro
            var loginService = scope.ServiceProvider.GetRequiredService<AccountLoginService>();
            var loginResult = await loginService.AuthenticateAsync(packet.Username, packet.Password, CancellationToken.None);

            if (!loginResult.Success || loginResult.Account is null)
            {
                var response = UnconnectedLoginResponsePacket.Failure(loginResult.Message);
                _networkManager.SendUnconnected(remoteEndPoint, response);
                return;
            }
        
            // ✅ Cria token de sessão
            var sessionToken = _tokenManager.CreateSession(loginResult.Account.Id, loginResult.Account);

            var playerCharacters = loginResult.Characters.Select(c => new PlayerCharData
            {
                Id = c.Id,
                Name = c.Name,
                Level = c.Stats?.Level ?? 1,
                Vocation = c.Vocation,
                Gender = c.Gender
            }).ToArray();
        
            var successResponse = UnconnectedLoginResponsePacket.SuccessResponse(sessionToken, playerCharacters);
            _networkManager.SendUnconnected(remoteEndPoint, successResponse);
        
            _logger.LogInformation("Account registered and logged in: {Username} from {EndPoint} (Token: {Token})", 
                packet.Username, remoteEndPoint, sessionToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing unconnected registration from {EndPoint}", remoteEndPoint);
            var response = UnconnectedRegistrationResponsePacket.Failure("Erro interno no servidor.");
            _networkManager.SendUnconnected(remoteEndPoint, response);
        }
    }

// ✅ IMPLEMENTAÇÃO COMPLETA DE CHARACTER CREATION
    private async Task ProcessCharacterCreationAsync(IPEndPoint remoteEndPoint, UnconnectedCharacterCreationRequestPacket packet)
    {
        try
        {
            // ✅ Valida token de sessão
            if (!_tokenManager.ValidateToken(packet.SessionToken, out var accountId, out var account))
            {
                var response = UnconnectedCharacterCreationResponsePacket.Failure("Sessão inválida ou expirada.");
                _networkManager.SendUnconnected(remoteEndPoint, response);
                _logger.LogWarning("Invalid session token for character creation from {EndPoint}", remoteEndPoint);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var characterService = scope.ServiceProvider.GetRequiredService<AccountCharacterService>();
        
            var creationResult = await characterService.CreateCharacterAsync( 
                new AccountCharacterService.CharacterInfo(
                    accountId,
                    packet.Name,
                    1,
                    packet.Vocation,
                    packet.Gender
                ), CancellationToken.None);

            if (!creationResult.Success || creationResult.Character is null)
            {
                var response = UnconnectedCharacterCreationResponsePacket.Failure(creationResult.Message);
                _networkManager.SendUnconnected(remoteEndPoint, response);
                return;
            }
        
            // ✅ Adiciona personagem à conta na sessão
            account!.Characters.Add(creationResult.Character);
        
            var charData = new PlayerCharData
            {
                Id = creationResult.Character.Id,
                Name = creationResult.Character.Name,
                Level = creationResult.Character.Stats?.Level ?? 1,
                Vocation = creationResult.Character.Vocation,
                Gender = creationResult.Character.Gender
            };
        
            var successResponse = UnconnectedCharacterCreationResponsePacket.Ok(charData);
            _networkManager.SendUnconnected(remoteEndPoint, successResponse);
        
            _logger.LogInformation("Character created via unconnected: {CharacterName} for account {AccountId} from {EndPoint}", 
                creationResult.Character.Name, accountId, remoteEndPoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing character creation from {EndPoint}", remoteEndPoint);
            var response = UnconnectedCharacterCreationResponsePacket.Failure("Erro interno no servidor.");
            _networkManager.SendUnconnected(remoteEndPoint, response);
        }
    }

// ✅ IMPLEMENTAÇÃO COMPLETA DE CHARACTER SELECTION
    private Task ProcessCharacterSelectionAsync(IPEndPoint remoteEndPoint, UnconnectedCharacterSelectionRequestPacket packet)
    {
        try
        {
            // ✅ Valida token de sessão
            if (!_tokenManager.ValidateToken(packet.SessionToken, out var accountId, out var account))
            {
                var response = UnconnectedCharacterSelectionResponsePacket.Failure("Sessão inválida ou expirada.");
                _networkManager.SendUnconnected(remoteEndPoint, response);
                _logger.LogWarning("Invalid session token for character selection from {EndPoint}", remoteEndPoint);
                return Task.CompletedTask;
            }
        
            var character = account!.Characters.FirstOrDefault(c => c.Id == packet.CharacterId);
        
            if (character is null)
            {
                var response = UnconnectedCharacterSelectionResponsePacket.Failure("Personagem não encontrado.");
                _networkManager.SendUnconnected(remoteEndPoint, response);
                _logger.LogWarning("Character {CharacterId} not found for account {AccountId}", packet.CharacterId, accountId);
                return Task.CompletedTask;
            }
        
            var successResponse = UnconnectedCharacterSelectionResponsePacket.Ok(character.Id);
            _networkManager.SendUnconnected(remoteEndPoint, successResponse);
        
            _logger.LogInformation("Character selected via unconnected: {CharacterName} (ID: {CharacterId}) for account {AccountId} from {EndPoint}", 
                character.Name, character.Id, accountId, remoteEndPoint);
            
            var sessionToken = _tokenManager.CreateGameToken(accountId, character.Id);
            _logger.LogDebug("Game token created for account {AccountId}, character {CharacterId}: {Token}", accountId, character.Id, sessionToken);
            
            // ✅ Envia token de jogo ao cliente
            var tokenPacket = new UnconnectedGameTokenResponsePacket(sessionToken);
            _networkManager.SendUnconnected(remoteEndPoint, tokenPacket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing character selection from {EndPoint}", remoteEndPoint);
            var response = UnconnectedCharacterSelectionResponsePacket.Failure("Erro interno no servidor.");
            _networkManager.SendUnconnected(remoteEndPoint, response);
        }
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// ✅ Handler UNCONNECTED de deleção de personagem.
    /// </summary>
    private void HandleCharacterDeleteRequest(IPEndPoint remoteEndPoint, UnconnectedCharacterDeleteRequestPacket packet)
    {
        _ = ProcessCharacterDeleteAsync(remoteEndPoint, packet);
    }

    /// <summary>
    /// Processa requisição de deleção de personagem.
    /// </summary>
    private async Task ProcessCharacterDeleteAsync(IPEndPoint remoteEndPoint, UnconnectedCharacterDeleteRequestPacket packet)
    {
        try
        {
            // ✅ 1. Valida token de sessão
            if (!_tokenManager.ValidateToken(packet.SessionToken, out var accountId, out var account))
            {
                var response = UnconnectedCharacterDeleteResponsePacket.Failure("Sessão inválida ou expirada.");
                _networkManager.SendUnconnected(remoteEndPoint, response);
                _logger.LogWarning(
                    "Invalid session token for character deletion from {EndPoint}", 
                    remoteEndPoint);
                return;
            }

            // ✅ 2. Validação adicional: personagem pertence à conta?
            var character = account!.Characters.FirstOrDefault(c => c.Id == packet.CharacterId);
        
            if (character is null)
            {
                var response = UnconnectedCharacterDeleteResponsePacket.Failure(
                    "Personagem não encontrado ou não pertence a esta conta.");
                _networkManager.SendUnconnected(remoteEndPoint, response);
                _logger.LogWarning(
                    "Account {AccountId} attempted to delete non-existent character {CharacterId} from {EndPoint}",
                    accountId,
                    packet.CharacterId,
                    remoteEndPoint);
                return;
            }

            // ✅ 3. Verificar se há pelo menos 1 personagem restante (opcional)
            // Descomente se quiser impedir deleção do último personagem
            /*
            if (account.Characters.Count <= 1)
            {
                var response = UnconnectedCharacterDeleteResponsePacket.Failure(
                    "Não é possível deletar o último personagem da conta.");
                _networkManager.SendUnconnected(remoteEndPoint, response);
                _logger.LogWarning(
                    "Account {AccountId} attempted to delete last character {CharacterId}",
                    accountId,
                    packet.CharacterId);
                return;
            }
            */

            // ✅ 4. Deleta personagem via service
            using var scope = _scopeFactory.CreateScope();
            var characterService = scope.ServiceProvider.GetRequiredService<AccountCharacterService>();
        
            var deletionResult = await characterService.DeleteCharacterAsync(
                accountId,
                packet.CharacterId,
                CancellationToken.None);

            if (!deletionResult.Success)
            {
                var response = UnconnectedCharacterDeleteResponsePacket.Failure(deletionResult.Message);
                _networkManager.SendUnconnected(remoteEndPoint, response);
                return;
            }

            // ✅ 5. Remove personagem da lista em memória da sessão
            var gameDbContext = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            account.Characters.Remove(character);
            gameDbContext.Accounts.Update(account);
            await gameDbContext.SaveChangesAsync(CancellationToken.None);
            
        
            // ✅ 6. Envia resposta de sucesso
            var successResponse = UnconnectedCharacterDeleteResponsePacket.Ok(packet.CharacterId);
            _networkManager.SendUnconnected(remoteEndPoint, successResponse);

            _logger.LogInformation(
                "Character deleted via unconnected: {CharacterName} (ID: {CharacterId}) for account {AccountId} from {EndPoint}",
                character.Name,
                packet.CharacterId,
                accountId,
                remoteEndPoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing character deletion from {EndPoint}", remoteEndPoint);
            var response = UnconnectedCharacterDeleteResponsePacket.Failure("Erro interno no servidor.");
            _networkManager.SendUnconnected(remoteEndPoint, response);
        }
    }
    
    /// <summary>
    /// ✅ Handler de conexão real com game token.
    /// AQUI é onde o GameDataPacket é enviado!
    /// </summary>
    private void HandleGameConnect(INetPeerAdapter peer, GameConnectPacket packet)
    {
        _ = ProcessGameConnectAsync(peer, packet);
    }

    private async Task ProcessGameConnectAsync(INetPeerAdapter peer, GameConnectPacket packet)
    {
        try
        {
            // ✅ Remove da lista de pendentes e cancela timeout
            if (_pendingConnections.TryRemove(peer.Id, out var pending))
            {
                await pending.TimeoutCts.CancelAsync();
                pending.TimeoutCts.Dispose();
            
                _logger.LogDebug("Peer {PeerId} authenticated - Timeout cancelled", peer.Id);
            }
            else
            {
                _logger.LogWarning("Peer {PeerId} sent GameConnectPacket but was not in pending list", peer.Id);
            }

            // ✅ 1. Valida game token
            if (!_tokenManager.ValidateAndConsumeGameToken(packet.GameToken, out var accountId, out var characterId))
            {
                _logger.LogWarning("Invalid or expired game token from peer {PeerId}", peer.Id);
                peer.Disconnect();
                return;
            }

            _logger.LogInformation(
                "Valid game token from peer {PeerId} - AccountId: {AccountId}, CharacterId: {CharacterId}",
                peer.Id, accountId, characterId
            );

            // ✅ 2. Carrega conta e personagem do banco
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        
            var account = await dbContext.Accounts
                .Include(a => a.Characters)
                .ThenInclude(c => c.Stats)
                .FirstOrDefaultAsync(a => a.Id == accountId);

            if (account is null)
            {
                _logger.LogError("Account {AccountId} not found for peer {PeerId}", accountId, peer.Id);
                peer.Disconnect();
                return;
            }

            var character = account.Characters.FirstOrDefault(c => c.Id == characterId);

            if (character is null)
            {
                _logger.LogError("Character {CharacterId} not found for peer {PeerId}", characterId, peer.Id);
                peer.Disconnect();
                return;
            }

            // ✅ 3. Cria sessão do jogador
            var session = new PlayerSession(peer, account, account.Characters.ToArray())
            {
                SelectedCharacter = character
            };

            if (!_sessionManager.TryAddSession(session, out var error))
            {
                _logger.LogError("Failed to create session for peer {PeerId}: {Error}", peer.Id, error);
                peer.Disconnect();
                return;
            }

            // ✅ 4. Spawna jogador no mundo
            _spawnService.SpawnPlayer(session);

            // ✅ 5. Monta dados do jogo
            var localSnapshot = _spawnService.BuildSnapshot(session);
            var othersSnapshots = _sessionManager
                .GetSnapshotExcluding(peer.Id)
                .Select(existing => _spawnService.BuildSnapshot(existing))
                .ToArray();

            // ✅ 6. Broadcasta spawn para outros jogadores
            var spawnPacket = new PlayerSpawnPacket(localSnapshot);
            _networkManager.SendToAllExcept(peer, spawnPacket, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);

            // ✅ 7. Envia dados do mapa
            var mapService = scope.ServiceProvider.GetRequiredService<MapService>();

            // ✅ 8. ENVIA GAMEDATAPACKET PARA O CLIENTE!
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

            _logger.LogInformation(
                "Player '{CharacterName}' entered the game (Peer: {PeerId}, NetID: {NetworkId})",
                character.Name, peer.Id, localSnapshot.NetworkId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing game connect for peer {PeerId}", peer.Id);
            peer.Disconnect();
        }
    }

    // ========== CONNECTED HANDLERS (In-game) ==========

    /// <summary>
    /// ✅ Handler CONNECTED de input de jogador.
    /// </summary>
    private void HandlePlayerInput(INetPeerAdapter peer, PlayerInputPacket packet)
    {
        if (!_sessionManager.TryGetByPeer(peer, out var session) || session is null)
        {
            _logger.LogWarning("Received PlayerInputPacket from unknown peer {PeerId}", peer.Id);
            return;
        }

        if (_simulation.TryApplyPlayerInput(session.Entity, packet.Movement, packet.MouseLook, packet.Buttons))
        {
            _logger.LogDebug(
                "Applied input from peer {PeerId}: Movement={Movement}, MouseLook={MouseLook}, Buttons={Buttons}", 
                peer.Id, 
                packet.Movement, 
                packet.MouseLook, 
                packet.Buttons
            );
        }
    }

    // ========== PERSISTENCE ==========

    private async Task PersistCharacterAsync(PlayerSession session, Coordinate position, DirectionEnum facing)
    {
        try
        {
            if (session.SelectedCharacter is null)
            {
                _logger.LogWarning("Cannot persist: session has no selected character");
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        
            // ✅ NOVO: Recarregar personagem completo do banco antes de persistir
            var character = await dbContext.Characters
                .AsSplitQuery()
                .Include(c => c.Stats)
                .Include(c => c.Equipment)
                .Include(c => c.Inventory)
                .ThenInclude(i => i.Slots)
                .FirstOrDefaultAsync(c => c.Id == session.SelectedCharacter.Id);

            if (character is null)
            {
                _logger.LogError(
                    "Cannot persist character {CharacterId}: not found in database",
                    session.SelectedCharacter.Id);
                return;
            }

            // ✅ Atualizar posição e direção
            character.PositionX = position.X;
            character.PositionY = position.Y;
            character.DirectionEnum = facing;
            character.LastUpdatedAt = DateTime.UtcNow;

            // ✅ Atualizar vitals (HP/MP) se disponível
            if (_simulation.TryGetPlayerVitals(session.Entity, out var vitals) && character.Stats is not null)
            {
                character.Stats.CurrentHp = vitals.CurrentHp;
                character.Stats.CurrentMp = vitals.CurrentMp;
            }

            // ✅ Salvar diretamente (não usar PersistAsync para evitar overhead)
            dbContext.Characters.Update(character);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            _logger.LogInformation(
                "Character {CharacterName} (ID: {CharacterId}) persisted: Position ({X},{Y}), Direction: {Direction}, HP: {HP}, MP: {MP}",
                character.Name,
                character.Id,
                position.X,
                position.Y,
                facing,
                character.Stats?.CurrentHp ?? 0,
                character.Stats?.CurrentMp ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist character {CharacterName}", session.SelectedCharacter?.Name ?? "Unknown");
        }
    }

    // ========== DISPOSE ==========

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    
        // ✅ Cancela todos os timeouts pendentes
        foreach (var pending in _pendingConnections.Values)
        {
            try
            {
                pending.TimeoutCts.Cancel();
                pending.TimeoutCts.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing pending connection for peer {PeerId}", pending.Peer.Id);
            }
        }
        _pendingConnections.Clear();
    
        _networkManager.OnPeerConnected -= OnPeerConnected;
        _networkManager.OnPeerDisconnected -= OnPeerDisconnected;
    
        // ✅ Desregistra handlers unconnected
        _networkManager.UnregisterUnconnectedPacketHandler<UnconnectedLoginRequestPacket>();
        _networkManager.UnregisterUnconnectedPacketHandler<UnconnectedRegistrationRequestPacket>();
        _networkManager.UnregisterUnconnectedPacketHandler<UnconnectedCharacterCreationRequestPacket>();
        _networkManager.UnregisterUnconnectedPacketHandler<UnconnectedCharacterSelectionRequestPacket>();
        _networkManager.UnregisterUnconnectedPacketHandler<UnconnectedCharacterDeleteRequestPacket>();
    
        // ✅ Desregistra handlers connected
        _networkManager.UnregisterPacketHandler<PlayerInputPacket>();
        _networkManager.UnregisterPacketHandler<GameConnectPacket>();
    }
}