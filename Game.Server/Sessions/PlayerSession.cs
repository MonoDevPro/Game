using System;
using Arch.Core;
using Game.Abstractions.Network;
using Game.Domain.Entities;

namespace Game.Server.Sessions;

/// <summary>
/// Tracks runtime data for a connected player.
/// </summary>
public sealed class PlayerSession
{
    public PlayerSession(INetPeerAdapter peer, Account account, Character character)
    {
        Peer = peer;
        Account = account;
        Character = character;
        ConnectedAt = DateTimeOffset.UtcNow;
    }

    public INetPeerAdapter Peer { get; }
    public Account Account { get; }
    public Character Character { get; }

    public Entity Entity { get; set; } = Entity.Null;
    public DateTimeOffset ConnectedAt { get; }
    public uint LastInputSequence { get; set; }
}
