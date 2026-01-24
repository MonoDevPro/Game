using Arch.Core;
using Game.Contracts;

namespace Game.Simulation;

public sealed class WorldState
{
    private readonly Dictionary<int, PlayerState> _players = new();

    public void UpsertPlayer(int characterId, string name, int x, int y)
    {
        _players[characterId] = new PlayerState(characterId, name, x, y);
    }

    public void Move(int characterId, int dx, int dy)
    {
        if (_players.TryGetValue(characterId, out var player))
        {
            _players[characterId] = player with { X = player.X + dx, Y = player.Y + dy };
        }
    }

    public WorldSnapshot BuildSnapshot()
        => new(_players.Values.ToList());
}
