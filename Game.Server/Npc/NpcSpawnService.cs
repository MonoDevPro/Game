using Arch.Core;
using GameECS.Server;
using GameECS.Shared.Entities.Data;

namespace Game.Server.Npc;

/// <summary>
/// Interface para repositório de NPCs.
/// </summary>
public interface INpcRepository
{
    IEnumerable<NpcSpawnInfo> GetAllNpcs();
}

/// <summary>
/// Informações de spawn de NPC.
/// </summary>
public record NpcSpawnInfo(
    string TemplateId,
    string Name,
    int X,
    int Y,
    int Level = 1,
    int MaxHp = 100
);

/// <summary>
/// Implementação simples de repositório de NPCs.
/// </summary>
public sealed class NpcRepository : INpcRepository
{
    private readonly List<NpcSpawnInfo> _npcs = new()
    {
        new NpcSpawnInfo("merchant_01", "Merchant", 10, 10, 5, 500),
        new NpcSpawnInfo("guard_01", "Guard", 15, 15, 10, 1000),
    };

    public IEnumerable<NpcSpawnInfo> GetAllNpcs() => _npcs;
}

/// <summary>
/// Serviço para spawn de NPCs.
/// </summary>
public sealed class NpcSpawnService
{
    private readonly ServerGameSimulation _simulation;
    private readonly INpcRepository _npcRepository;
    private readonly ILogger<NpcSpawnService> _logger;
    private readonly Dictionary<int, Entity> _spawnedNpcs = new();
    private int _nextNpcNetworkId = 10000;

    public NpcSpawnService(
        ServerGameSimulation simulation,
        INpcRepository npcRepository,
        ILogger<NpcSpawnService> logger)
    {
        _simulation = simulation;
        _npcRepository = npcRepository;
        _logger = logger;
    }

    /// <summary>
    /// Spawna todos os NPCs iniciais.
    /// </summary>
    public void SpawnInitialNpcs()
    {
        foreach (var npcInfo in _npcRepository.GetAllNpcs())
        {
            SpawnNpc(npcInfo);
        }
    }

    /// <summary>
    /// Spawna um NPC específico.
    /// </summary>
    public Entity SpawnNpc(NpcSpawnInfo info)
    {
        try
        {
            var entity = _simulation.CreateNpcEntity(info.TemplateId, info.X, info.Y);
            var networkId = _nextNpcNetworkId++;
            
            _spawnedNpcs[networkId] = entity;
            
            _logger.LogInformation("Spawned NPC {Name} at ({X}, {Y})", info.Name, info.X, info.Y);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to spawn NPC {TemplateId} - template not found", info.TemplateId);
            return Entity.Null;
        }
    }

    /// <summary>
    /// Constrói snapshots de todos os NPCs spawnados.
    /// </summary>
    public IEnumerable<NpcData> BuildSnapshots()
    {
        var npcs = _npcRepository.GetAllNpcs().ToList();
        int i = 0;
        
        foreach (var kvp in _spawnedNpcs)
        {
            if (i >= npcs.Count) break;
            
            var npcInfo = npcs[i];
            var networkId = kvp.Key;
            
            yield return new NpcData(
                NetworkId: networkId,
                Name: npcInfo.Name,
                TemplateId: npcInfo.TemplateId,
                Type: 0,
                X: npcInfo.X,
                Y: npcInfo.Y,
                Z: 0,
                Direction: 0,
                Hp: npcInfo.MaxHp,
                MaxHp: npcInfo.MaxHp,
                Mp: 0,
                MaxMp: 0,
                MovementSpeed: 1.0f,
                AttackSpeed: 1.0f,
                Level: npcInfo.Level
            );
            
            i++;
        }
    }
}
