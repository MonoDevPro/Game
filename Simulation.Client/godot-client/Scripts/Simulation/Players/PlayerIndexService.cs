using System.Collections.Generic;
using Game.ECS.Entities.Factories;

namespace GodotClient.Simulation.Players;

public class PlayerIndexService
{
    private readonly Dictionary<int, PlayerSnapshot> _players = new();
    
    public PlayerSnapshot RegisterPlayer(PlayerSnapshot data)
    {
        _players[data.NetworkId] = data;
        return data;
    }
    
    public void UnregisterPlayer(int networkId)
    {
        _players.Remove(networkId);
    }
    
    public bool TryGetPlayer(int networkId, out PlayerSnapshot data)
    {
        return _players.TryGetValue(networkId, out data);
    }
    
    public IEnumerable<PlayerSnapshot> GetAllPlayers()
    {
        return _players.Values;
    }
    
    public void Clear()
    {
        _players.Clear();
    }
    
}