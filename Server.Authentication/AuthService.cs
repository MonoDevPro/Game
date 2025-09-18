using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Server.Authentication.Session;
using Simulation.Core.Auth.Messages;
using Simulation.Core.Network.Contracts;
using Simulation.Core.Options;
using Simulation.Core.Persistence.Contracts;
using Simulation.Core.Persistence.Contracts.Repositories;
using Simulation.Core.Persistence.Models;

namespace Server.Authentication;

public class AuthService
{
    private readonly AuthOptions _options;
    private readonly IChannelEndpoint _endpoint;
    private readonly SessionManager _sessionManager;
    private readonly IBackgroundTaskQueue _backgroundQueue;
    private readonly TimeProvider _time;
    private readonly ILogger<AuthService>? _logger;

    // rate-limiting simples por username (in-memory)
    private readonly ConcurrentDictionary<string, AttemptInfo> _attempts = new();

    public AuthService(IOptions<AuthOptions> authOptions,
        IChannelProcessorFactory processorFactory,
        SessionManager sessionManager,
        IBackgroundTaskQueue backgroundQueue,
        TimeProvider time,
        ILogger<AuthService>? logger)
    {
        _options = authOptions.Value;
        _endpoint = processorFactory.CreateOrGet(NetworkChannel.Authentication);
        _sessionManager = sessionManager;
        _backgroundQueue = backgroundQueue;
        _time = time;
        _logger = logger;

        _endpoint.RegisterHandler<LoginRequest>(HandleLogin);
        _endpoint.RegisterHandler<RegisterRequest>(HandleRegister);
    }
    
    public void AuthUpdate(float deltaTime)
    {
        // usa TimeProvider para consistência/testabilidade
        _sessionManager.CleanupExpired();
    }

    private void HandleLogin(INetPeerAdapter peer, LoginRequest req)
    {
        // quick validation / early rejection (no DB) to avoid enqueuing useless work
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
        {
            // send immediate response without enqueuing
            _endpoint.SendToPeerId(peer.Id, new LoginResponse(false, "Credenciais inválidas", 0, null, null), NetworkDeliveryMethod.ReliableOrdered);
            return;
        }

        // simple rate-limit check (fast-path)
        if (IsRateLimited(req.Username))
        {
            _endpoint.SendToPeerId(peer.Id, new LoginResponse(false, "Muitas tentativas, tente mais tarde", 0, null, null), NetworkDeliveryMethod.ReliableOrdered);
            return;
        }

        // Enfileira o trabalho que fará DB e demais operações em um scope
        _backgroundQueue.QueueBackgroundWorkItem(async (sp, ct) =>
        {
            try
            {
                var repo = sp.GetService<IAccountRepository>();
                var timeProvider = sp.GetService<TimeProvider>();
                
                if (repo == null)
                {
                    _logger?.LogError("IAccountRepository não disponível no scope");
                    _endpoint.SendToPeerId(peer.Id, new LoginResponse(false, "Erro interno", 0, null, null), NetworkDeliveryMethod.ReliableOrdered);
                    return;
                }

                var account = await repo.GetByUsernameAsync(req.Username, ct);
                if (account == null || !BCrypt.Net.BCrypt.Verify(req.Password, account.PasswordHash))
                {
                    RegisterFailure(req.Username);
                    _endpoint.SendToPeerId(peer.Id, new LoginResponse(false, "Usuário ou senha inválidos", 0, null, null), NetworkDeliveryMethod.ReliableOrdered);
                    return;
                }

                var now = timeProvider?.GetUtcNow() ?? DateTimeOffset.UtcNow;
                await repo.UpdateLastLoginAsync(account.Id, ct);

                var token = _sessionManager.CreateSession(account.Id, peer.Id);

                _endpoint.SendToPeerId(peer.Id, new LoginResponse(true, "Autenticado", account.Id, now.DateTime, token), NetworkDeliveryMethod.ReliableOrdered);

                // sucesso: reset rate-limit info
                _attempts.TryRemove(req.Username, out _);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro tratando login para {User}", req.Username);
                _endpoint.SendToPeerId(peer.Id, new LoginResponse(false, "Erro interno", 0, null, null), NetworkDeliveryMethod.ReliableOrdered);
            }
        });
    }
    
    private void HandleRegister(INetPeerAdapter peer, RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
        {
            _endpoint.SendToPeerId(peer.Id, new RegisterResponse(false, "Username ou senha inválidos"), NetworkDeliveryMethod.ReliableOrdered);
            return;
        }

        // Enfileira criação — feita dentro de scope
        _backgroundQueue.QueueBackgroundWorkItem(async (sp, ct) =>
        {
            try
            {
                var repo = sp.GetService(typeof(IAccountRepository)) as IAccountRepository;
                if (repo == null)
                {
                    _logger?.LogError("IAccountRepository não disponível no scope");
                    _endpoint.SendToPeerId(peer.Id, new RegisterResponse(false, "Erro interno"), NetworkDeliveryMethod.ReliableOrdered);
                    return;
                }

                var existing = await repo.GetByUsernameAsync(req.Username, ct);
                if (existing != null)
                {
                    _endpoint.SendToPeerId(peer.Id, new RegisterResponse(false, "Username já existe"), NetworkDeliveryMethod.ReliableOrdered);
                    return;
                }

                var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);
                var model = new AccountModel
                {
                    Username = req.Username,
                    PasswordHash = hash,
                    CreatedAt = _time.GetUtcNow(),
                    LastLoginAt = null
                };

                var id = await repo.CreateAsync(model, ct);
                _endpoint.SendToPeerId(peer.Id, new RegisterResponse(true, "Conta criada"), NetworkDeliveryMethod.ReliableOrdered);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro registrando {User}", req.Username);
                _endpoint.SendToPeerId(peer.Id, new RegisterResponse(false, "Erro interno"), NetworkDeliveryMethod.ReliableOrdered);
            }
        });
    }
    
    #region Rate-limit helpers (in-memory)
    
    private bool IsRateLimited(string username)
    {
        var info = _attempts.GetOrAdd(username, _ => new AttemptInfo());
        lock (info.Lock)
        {
            var now = _time.GetUtcNow();
            // cleanup interval vindo de options (você já usa _options.CleanupIntervalMinutes)
            var intervalMinutes = _options?.CleanupIntervalMinutes ?? 1;
            if (now - info.FirstAttempt > TimeSpan.FromMinutes(intervalMinutes))
            {
                info.Count = 0;
                info.FirstAttempt = now;
            }

            return info.Count >= 5;
        }
    }

    private void RegisterFailure(string username)
    {
        var info = _attempts.GetOrAdd(username, _ => new AttemptInfo());
        lock (info.Lock)
        {
            var now = _time.GetUtcNow();
            // aqui usamos janela fixa de 1 minuto para contagem
            if (now - info.FirstAttempt > TimeSpan.FromMinutes(1))
            {
                info.FirstAttempt = now;
                info.Count = 1;
            }
            else
            {
                info.Count++;
            }
        }
    }

    private class AttemptInfo
    {
        public int Count;
        public DateTimeOffset FirstAttempt = DateTime.UtcNow;
        public readonly object Lock = new();
    }
    #endregion
}