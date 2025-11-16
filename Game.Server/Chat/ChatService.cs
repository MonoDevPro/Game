using System;
using System.Collections.Generic;
using System.Linq;
using Game.Server.Sessions;
using Microsoft.Extensions.Logging;

namespace Game.Server.Chat;

public sealed class ChatService
{
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ChatService> _logger;
    private readonly int _maxHistory;
    private readonly int _maxMessageLength;
    private readonly TimeSpan _rateLimit;
    private readonly HashSet<string> _blacklist;
    private readonly Queue<ChatMessage> _history = new();
    private readonly Dictionary<int, DateTimeOffset> _lastMessageByNetwork = new();
    private readonly object _syncRoot = new();

    public ChatService(
        TimeProvider timeProvider,
        ILogger<ChatService> logger,
        int maxHistory = 50,
        int maxMessageLength = 200,
        double minSecondsBetweenMessages = 0.8)
    {
        _timeProvider = timeProvider;
        _logger = logger;
        _maxHistory = Math.Max(10, maxHistory);
        _maxMessageLength = Math.Max(32, maxMessageLength);
        _rateLimit = TimeSpan.FromSeconds(Math.Max(0.2, minSecondsBetweenMessages));
        _blacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "<script>",
            "drop table",
            "hack",
            "cheat"
        };
    }

    public bool TryCreatePlayerMessage(
        PlayerSession session,
        string rawMessage,
        out ChatMessage chatMessage,
        out string? error)
    {
        chatMessage = default;
        error = null;

        if (session.SelectedCharacter is null)
        {
            error = "Character not selected.";
            return false;
        }

        var sanitized = Sanitize(rawMessage);
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            error = "Mensagem vazia.";
            return false;
        }

        if (sanitized.Length > _maxMessageLength)
        {
            sanitized = sanitized[.._maxMessageLength];
        }

        if (ContainsBlacklistedWord(sanitized))
        {
            error = "Mensagem contém termos bloqueados.";
            _logger.LogWarning(
                "Blocked chat message from account {Account} due to blacklist.",
                session.Account.Username);
            return false;
        }

        ChatMessage pending;
        lock (_syncRoot)
        {
            var now = _timeProvider.GetUtcNow();
            if (_lastMessageByNetwork.TryGetValue(session.Peer.Id, out var lastSent) &&
                now - lastSent < _rateLimit)
            {
                error = "Você está enviando mensagens rápido demais.";
                return false;
            }

            _lastMessageByNetwork[session.Peer.Id] = now;

            pending = new ChatMessage(
                session.SelectedCharacter.Id,
                session.Peer.Id,
                session.SelectedCharacter.Name,
                sanitized,
                now,
                false);
        }

        chatMessage = pending;
        AppendHistory(chatMessage);
        return true;
    }

    public IReadOnlyList<ChatMessage> GetHistory()
    {
        lock (_syncRoot)
        {
            return _history.ToArray();
        }
    }

    public ChatMessage CreateSystemMessage(string content)
    {
        var now = _timeProvider.GetUtcNow();
        var message = new ChatMessage(0, 0, "System", content, now, true);
        AppendHistory(message);
        return message;
    }

    private void AppendHistory(ChatMessage message)
    {
        lock (_syncRoot)
        {
            _history.Enqueue(message);
            while (_history.Count > _maxHistory)
            {
                _history.Dequeue();
            }
        }
    }

    private static string Sanitize(string message)
    {
        var replaced = message
            .Replace('\r', ' ') 
            .Replace('\n', ' ');

        // Remove duplicadas e espaços extras
        return string.Join(' ', replaced
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Trim();
    }

    private bool ContainsBlacklistedWord(string message)
    {
        if (_blacklist.Count == 0)
            return false;

        return _blacklist.Any(word => message.Contains(word, StringComparison.OrdinalIgnoreCase));
    }
}
