using System.Collections.Generic;
using System.Linq;
using Game.ECS.Shared.Services.Network;
using Microsoft.Extensions.Logging;

namespace Game.Server.Sessions;

/// <summary>
/// Thread-safe in-memory registry of connected player sessions.
/// </summary>
public class PlayerSessionManager(ILogger<PlayerSessionManager> logger)
{
    private readonly Dictionary<int, PlayerSession> _sessionsByPeer = new();
    private readonly Dictionary<int, PlayerSession> _sessionsByAccount = new();
    private readonly object _syncRoot = new();

    public bool TryAddSession(PlayerSession session, out string? error)
    {
        lock (_syncRoot)
        {
            if (_sessionsByAccount.ContainsKey(session.Account.Id))
            {
                error = "Account already connected.";
                return false;
            }

            _sessionsByPeer[session.Peer.Id] = session;
            _sessionsByAccount[session.Account.Id] = session;
        }

        logger.LogInformation("Session created for account {Account} (peer {PeerId})", session.Account.Username, session.Peer.Id);
        error = null;
        return true;
    }

    public bool TryGetByPeer(INetPeerAdapter peer, out PlayerSession? session)
    {
        lock (_syncRoot)
        {
            return _sessionsByPeer.TryGetValue(peer.Id, out session);
        }
    }

    public bool TryRemoveByPeer(INetPeerAdapter peer, out PlayerSession? session)
    {
        lock (_syncRoot)
        {
            if (!_sessionsByPeer.Remove(peer.Id, out session))
            {
                return false;
            }

            _sessionsByAccount.Remove(session.Account.Id);
        }

        logger.LogInformation("Session removed for account {Account} (peer {PeerId})", session.Account.Username, peer.Id);
        return true;
    }

    public List<PlayerSession> GetSnapshot()
    {
        lock (_syncRoot)
        {
            return _sessionsByPeer.Values.ToList();
        }
    }

    public List<PlayerSession> GetSnapshotExcluding(int peerId)
    {
        lock (_syncRoot)
        {
            return _sessionsByPeer
                .Where(pair => pair.Key != peerId)
                .Select(pair => pair.Value)
                .ToList();
        }
    }
}
