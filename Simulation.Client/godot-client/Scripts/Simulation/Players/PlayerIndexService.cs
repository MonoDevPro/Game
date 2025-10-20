using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace GodotClient.Simulation.Players;

public class PlayerIndexService
{
    private readonly Dictionary<int, PlayerData> _players = new();
    
    public PlayerData RegisterPlayer(PlayerData data)
    {
        _players[data.NetworkId] = data;
        return data;
    }
    
    public void UnregisterPlayer(int networkId)
    {
        _players.Remove(networkId);
    }
    
    public bool TryGetPlayer(int networkId, out PlayerData? data)
    {
        return _players.TryGetValue(networkId, out data);
    }
    
    public IEnumerable<PlayerData> GetAllPlayers()
    {
        return _players.Values;
    }
    
    public void Clear()
    {
        _players.Clear();
    }
    
}