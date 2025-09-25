using Simulation.Core.ECS.Adapters;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Services;
using Simulation.Core.Network.Contracts;
using Simulation.Core.Ports;

namespace Simulation.Core.ECS;

/// <summary>
/// Serviço central para a gestão do mundo do jogo.
/// Contém o índice espacial e os dados do mapa, atuando como a única fonte da verdade.
/// </summary>
public class WorldManager
{
    public INetworkManager NetworkManager { get; private set; }
    public IChannelEndpoint SimulationEndpoint { get; private set; }
    public WorldSpatial WorldSpatial { get; private set; }
    public MapService MapService { get; private set; }
    public IWorldSaver WorldSaver { get; private set; }
    public WorldStaging WorldStaging { get; private set; } = new();
    
    /// <summary>
    /// Serviço central para a gestão do mundo do jogo.
    /// Contém o índice espacial e os dados do mapa, atuando como a única fonte da verdade.
    /// </summary>
    public WorldManager(MapService mapService, IWorldSaver saver, INetworkManager networkManager, IChannelEndpoint simulationEndpoint)
    {
        NetworkManager = networkManager;
        SimulationEndpoint = simulationEndpoint;
        WorldSpatial = new WorldSpatial(
            minX: 0, 
            minY: 0, 
            width: mapService.Width, 
            height: mapService.Height);
        MapService = mapService;
        WorldSaver = saver;
    }
}