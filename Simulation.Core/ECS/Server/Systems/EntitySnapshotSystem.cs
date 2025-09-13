using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Server.Systems.Builders;
using Simulation.Core.ECS.Server.Systems.Relationships;
using Simulation.Core.ECS.Shared;
using Simulation.Core.ECS.Shared.Data;
using Simulation.Core.ECS.Shared.Systems;
using Simulation.Core.ECS.Shared.Systems.Indexes;
using Simulation.Core.Network;
using Simulation.Core.Network.Packets;

namespace Simulation.Core.ECS.Server.Systems;

public sealed partial class EntitySnapshotSystem(World world, EntityIndexSystem index, NetworkManager networkManager, ILogger<EntitySnapshotSystem> logger)
    : BaseSystem<World, float>(world)
{
    // Reuso de listas (apenas no thread principal)
    private readonly List<Entity> _entities = new();
    private readonly List<PlayerData> _players = new();
    
    [Query]
    [All<NewlyCreated, PlayerId, MapId>]
    private void ProcessJoin(in Entity playerEntity, in PlayerId pId, in MapId mapId)
    {
        
        if (!index.TryGetMap(mapId.Value, out var map) || !World.IsAlive(map.Entity))
        {
            logger.LogWarning("CharId {CharId} tentou entrar no mapa {MapId}, mas o mapa não está carregado no mundo. O login será descartado.", pId.Value, mapId.Value);
            World.Add<NewlyDestroyed>(playerEntity, new NewlyDestroyed());
            return;
        }
        
        ProcessJoinGame(World.BuildPlayerData(playerEntity), map);
        World.Remove<NewlyCreated>(playerEntity);
    }

    [Query]
    [All<NewlyDestroyed, PlayerId, MapId>]
    private void ProcessLeave(in Entity playerEntity, in PlayerId pId, in MapId mapId)
    {
        if (!index.TryGetMap(mapId.Value, out var map) || !World.IsAlive(map.Entity))
            return;
        
        ProcessLeaveGame(pId.Value, map);
    }

    private void ProcessJoinGame(PlayerData joiningPlayerData, MapInstance mapInstance)
    {
        var initialState = new InitialStatePacket
        {
            PlayerId = joiningPlayerData.Id,
            CurrentMap = mapInstance.GetMapData(),
            OtherPlayers = GetPlayersDataInMap(mapInstance).ToArray()
        };
        networkManager.SendTo(joiningPlayerData.Id, initialState, DeliveryMethod.ReliableOrdered);
        
        var spawnPacketForOthers = new PlayerSpawnPacket
        {
            PlayerId = joiningPlayerData.Id,
            Data = joiningPlayerData
        };
        networkManager.BroadcastToOthersInMap(joiningPlayerData.Id, joiningPlayerData.MapId, spawnPacketForOthers, DeliveryMethod.ReliableOrdered);
    }
    
    private void ProcessLeaveGame(int leavingPlayerId, MapInstance mapInstance)
    {
        var despawnPacket = new PlayerDespawnPacket
        {
            PlayerId = leavingPlayerId
        };
        networkManager.BroadcastToOthersInMap(leavingPlayerId, mapInstance.MapId, despawnPacket, DeliveryMethod.ReliableOrdered);
    }
    
    private List<PlayerData> GetPlayersDataInMap(MapInstance map, int excludingPlayerId = -1)
    {
        _entities.Clear();
        _players.Clear();
        
        World.GetMapPlayerEntities(map, _entities);
        
        for (var i = 0; i < _entities.Count; i++)
        {
            var ent = _entities[i];
            if (World.IsAlive(ent))
            {
                ref var pId = ref World.Get<PlayerId>(ent);
                if (excludingPlayerId == -1 || pId.Value != excludingPlayerId)
                    _players.Add(World.BuildPlayerData(ent));
            }
        }
        
        return _players;
    }
}
