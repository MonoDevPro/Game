using Arch.Core;
using Game.ECS.Systems;
using Game.Network.Abstractions;
using Game.Network.Packets.Game;

namespace Game.Server.ECS.Systems;

public sealed partial class ServerSyncSystem(
    World world,
    INetworkManager sender,
    ILogger<ServerSyncSystem>? logger = null)
    : GameSystem(world)
{
    private readonly List<CombatStateSnapshot> _combatBuffer = [];
    
    private readonly List<NpcSpawnRequest> _npcSpawnBuffer = [];
    private readonly List<NpcStateUpdate> _npcStateBuffer = [];
    private readonly List<NpcVitalsUpdate> _npcHealthBuffer = [];
    
    private readonly List<PlayerSpawn> _playerSpawnBuffer = [];
    private readonly List<StateUpdate> _playerStateBuffer = [];
    private readonly List<VitalsUpdate> _playerVitalsBuffer = [];
    
    private void FlushBuffers()
    {
        // ===========================
        // ======= COMBAT ============
        // ===========================
        
        if (_combatBuffer.Count > 0)
        {
            var packet = new CombatStatePacket(_combatBuffer.ToArray());
            sender.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            _combatBuffer.Clear();
        }
        
        // ===========================
        // ========== NPCs ===========
        // ===========================
            
        if (_npcSpawnBuffer.Count > 0)
        {
            var packet = new NpcSpawnPacket(_npcSpawnBuffer.ToArray());
            sender.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            _npcSpawnBuffer.Clear();
        }
        if (_npcStateBuffer.Count > 0)
        {
            var packet = new NpcStatePacket(_npcStateBuffer.ToArray());
            sender.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            _npcStateBuffer.Clear();
        }
        if (_npcHealthBuffer.Count > 0)
        {
            var packet = new NpcHealthPacket(_npcHealthBuffer.ToArray());
            sender.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            _npcHealthBuffer.Clear();
        }
            
        // ===========================
        // ======== PLAYERS ==========
        // ===========================
            
        if (_playerSpawnBuffer.Count > 0)
        {
            var packet = new PlayerSpawnPacket(_playerSpawnBuffer.ToArray());
            sender.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            _playerSpawnBuffer.Clear();
        }
        
        if (_playerStateBuffer.Count > 0)
        {
            var packet = new PlayerStatePacket(_playerStateBuffer.ToArray());
            sender.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            _playerStateBuffer.Clear();
        }
        
        if (_playerVitalsBuffer.Count > 0)
        {
            var packet = new PlayerVitalsPacket(_playerVitalsBuffer.ToArray());
            sender.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            _playerVitalsBuffer.Clear();
        }
    }
}