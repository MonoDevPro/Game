using Arch.Core;
using Game.Contracts;

namespace Game.Simulation;

/// <summary>
/// Classe legada para compatibilidade. 
/// Use <see cref="ServerWorldSimulation"/> para nova implementação baseada em ECS.
/// </summary>
[Obsolete("Use ServerWorldSimulation para implementação baseada em ECS. Esta classe será removida em versões futuras.")]
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

    public void RemovePlayer(int characterId)
    {
        _players.Remove(characterId);
    }

    public WorldSnapshot BuildSnapshot()
        => new(_players.Values.ToList());
}
