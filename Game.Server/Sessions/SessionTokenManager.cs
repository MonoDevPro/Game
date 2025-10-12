using System.Collections.Concurrent;
using Game.Domain.Entities;

namespace Game.Server.Sessions;

/// <summary>
/// Gerenciador de tokens de sessão para autenticação unconnected.
/// Autor: MonoDevPro
/// Data: 2025-01-12 06:09:41
/// </summary>
public sealed class SessionTokenManager
{
    private readonly ConcurrentDictionary<string, UnconnectedSession> _sessions = new();
    private readonly ConcurrentDictionary<string, GameToken> _gameTokens = new();
    private readonly Timer _cleanupTimer;

    public SessionTokenManager()
    {
        // Limpa sessões expiradas a cada 1 minuto
        _cleanupTimer = new Timer(CleanupExpiredSessions, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// Cria uma nova sessão unconnected e retorna o token.
    /// </summary>
    public string CreateSession(int accountId, Account account)
    {
        var token = Guid.NewGuid().ToString("N"); // Sem hífens

        _sessions[token] = new UnconnectedSession
        {
            Token = token,
            AccountId = accountId,
            Account = account,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5) // 5 minutos de validade
        };

        return token;
    }

    /// <summary>
    /// Valida um token e retorna os dados da sessão.
    /// </summary>
    public bool ValidateToken(string token, out int accountId, out Account? account)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            accountId = 0;
            account = null;
            return false;
        }

        if (_sessions.TryGetValue(token, out var session))
        {
            if (session.ExpiresAt > DateTime.UtcNow)
            {
                // ✅ Renova a sessão ao validar (sliding expiration)
                session.ExpiresAt = DateTime.UtcNow.AddMinutes(5);

                accountId = session.AccountId;
                account = session.Account;
                return true;
            }

            // Token expirado, remove
            _sessions.TryRemove(token, out _);
        }

        accountId = 0;
        account = null;
        return false;
    }

    /// <summary>
    /// Remove uma sessão (logout).
    /// </summary>
    public bool RemoveSession(string token)
    {
        return _sessions.TryRemove(token, out _);
    }

    /// <summary>
    /// Obtém estatísticas das sessões ativas.
    /// </summary>
    public SessionStats GetStats()
    {
        var now = DateTime.UtcNow;
        var active = _sessions.Count(s => s.Value.ExpiresAt > now);

        return new SessionStats
        {
            TotalSessions = _sessions.Count,
            ActiveSessions = active,
            ExpiredSessions = _sessions.Count - active
        };
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _sessions.Clear();
    }
    /// <summary>
    /// Cria um token de jogo (para conexão após seleção de personagem).
    /// </summary>
    public string CreateGameToken(int accountId, int characterId)
    {
        var token = Guid.NewGuid().ToString("N");

        _gameTokens[token] = new GameToken
        {
            Token = token,
            AccountId = accountId,
            CharacterId = characterId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(2) // 2 minutos para conectar
        };

        return token;
    }

    /// <summary>
    /// Valida e consome um game token (uso único).
    /// </summary>
    public bool ValidateAndConsumeGameToken(string token, out int accountId, out int characterId)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            accountId = 0;
            characterId = 0;
            return false;
        }

        if (_gameTokens.TryRemove(token, out var gameToken))
        {
            if (gameToken.ExpiresAt > DateTime.UtcNow)
            {
                accountId = gameToken.AccountId;
                characterId = gameToken.CharacterId;
                return true;
            }
        }

        accountId = 0;
        characterId = 0;
        return false;
    }

// Adicionar ao cleanup
    private void CleanupExpiredSessions(object? state)
    {
        var now = DateTime.UtcNow;

        // Limpa sessões expiradas
        var expiredTokens = _sessions
            .Where(kvp => kvp.Value.ExpiresAt < now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var token in expiredTokens)
        {
            _sessions.TryRemove(token, out _);
        }

        // ✅ Limpa game tokens expirados
        var expiredGameTokens = _gameTokens
            .Where(kvp => kvp.Value.ExpiresAt < now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var token in expiredGameTokens)
        {
            _gameTokens.TryRemove(token, out _);
        }

        if (expiredTokens.Count > 0 || expiredGameTokens.Count > 0)
        {
            Console.WriteLine(
                $"[SessionTokenManager] Cleaned up {expiredTokens.Count} sessions and {expiredGameTokens.Count} game tokens");
        }
    }
}

/// <summary>
/// Token de jogo para conexão.
/// </summary>
public sealed class GameToken
{
    public string Token { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public int CharacterId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
/// <summary>
/// Representa uma sessão não conectada (menu).
/// </summary>
public sealed class UnconnectedSession
{
    public string Token { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Estatísticas de sessões.
/// </summary>
public sealed class SessionStats
{
    public int TotalSessions { get; set; }
    public int ActiveSessions { get; set; }
    public int ExpiredSessions { get; set; }
}