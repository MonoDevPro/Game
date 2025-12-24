using Arch.Core;
using Game.Domain.Player;
using Game.Network.Abstractions;

namespace Game.Server.Sessions;

/// <summary>
/// Tracks runtime data for a connected player.
/// </summary>
public sealed class PlayerSession(INetPeerAdapter peer, Account account, Character[] characters)
{
    public INetPeerAdapter Peer { get; } = peer;
    public Account Account { get; } = account;
    public Character[] Characters { get; } = characters;
    public Character? SelectedCharacter { get; set; }

    public Entity Entity { get; set; } = Entity.Null;
    public DateTimeOffset ConnectedAt { get; } = DateTimeOffset.UtcNow;
}
