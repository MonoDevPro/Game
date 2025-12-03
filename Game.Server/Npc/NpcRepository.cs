using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.ECS.Entities.Npc;
using Game.ECS.Schema.Components;
using Game.ECS.Schema.Templates;

namespace Game.Server.Npc;

public interface INpcRepository
{
    NpcTemplate GetTemplate(int templateId);
    IEnumerable<SpawnPoint> GetSpawnPoints(int mapId);
    void LoadTemplates(string jsonContent);
}

public class NpcRepository : INpcRepository
{
    
    private readonly Dictionary<int, NpcTemplate> _templates = new();
    private readonly List<NpcSpawnPoint> _spawnPoints = [];

    private NpcTemplate[] LoadDefaultTemplates() =>
    [
        new() // Orc Warrior Template
        {
            Id = 1,
            Name = "Orc Warrior",
            Gender = (byte)Gender.Male,
            Vocation = (byte)VocationType.Warrior,
            StatsTemplate = new Stats
            {
                MovementSpeed = 1.0f,
                AttackSpeed = 1.0f,
                PhysicalAttack = 25,
                MagicAttack = 5,
                PhysicalDefense = 10,
                MagicDefense = 5
            },
            VitalsTemplate = new Vitals
            {
                CurrentHp = 200,
                MaxHp = 200,
                CurrentMp = 50,
                MaxMp = 50,
                HpRegen = 0.5f,
                MpRegen = 0.2f
            },
            Behavior = new BehaviorConfig
            {
                VisionRange = 8f,
                AttackRange = 1.5f,
                LeashRange = 10f,
                PatrolRadius = 0f
            }
        },
        
        new() // Goblin Template
        {
            Id = 2,
            Name = "Goblin",
            Gender = (byte)Gender.Male,
            Vocation = (byte)VocationType.Archer,
            StatsTemplate = new Stats
            {
                MovementSpeed = 1.2f,
                AttackSpeed = 1.5f,
                PhysicalAttack = 15,
                MagicAttack = 0,
                PhysicalDefense = 5,
                MagicDefense = 2
            },
            VitalsTemplate = new Vitals
            {
                CurrentHp = 100,
                MaxHp = 100,
                CurrentMp = 30,
                MaxMp = 30,
                HpRegen = 0.3f,
                MpRegen = 0.1f
            },
            Behavior = new BehaviorConfig
            {
                VisionRange = 6f,
                AttackRange = 5f,
                LeashRange = 8f,
                PatrolRadius = 0f,
                IdleDurationMin = 2f,
                IdleDurationMax = 5f,
            }
        }
    ];
    private NpcSpawnPoint[] LoadDefaultSpawnPoints() =>
    [
        // Orc Warrior Spawn
        new NpcSpawnPoint(Id: 1, MapId: 1, Floor: 0, X: 10, Y: 15),
        // Goblin Spawn
        new NpcSpawnPoint(Id: 2, MapId: 1, Floor: 0, X: 20, Y: 25)
    ];
        

    public NpcRepository()
    {
        // Load default templates and spawn points
        foreach (var template in LoadDefaultTemplates())
            _templates[template.Id] = template;
        _spawnPoints.AddRange(LoadDefaultSpawnPoints());
    }

    public NpcTemplate GetTemplate(int templateId)
    {
        return _templates.TryGetValue(templateId, out var template) 
            ? template 
            : throw new KeyNotFoundException($"NpcTemplate with Id {templateId} not found.");
    }

    public IEnumerable<NpcSpawnPoint> GetSpawnPoints(int mapId)
    {
        return _spawnPoints.Where(s => s.MapId == mapId);
    }

    public void LoadTemplates(string jsonContent)
    {
        // TODO: Implement JSON deserialization
        // For now, we can manually populate for testing if needed, 
        // or just leave it as a placeholder for the actual implementation.
        // The instructions say: "Deserializar JSON e popular _templates"
    }
    
    // Helper to add templates manually (useful for migration from hardcoded)
    public void AddTemplate(NpcTemplate template)
    {
        _templates[template.Id] = template;
    }

    public void AddSpawnPoint(NpcSpawnPoint spawnPoint)
    {
        _spawnPoints.Add(spawnPoint);
    }
}