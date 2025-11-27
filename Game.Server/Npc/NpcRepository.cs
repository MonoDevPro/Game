using Game.Domain.Templates;
using Game.Domain.Enums;

namespace Game.Server.Npc;

public interface INpcRepository
{
    NpcTemplate GetTemplate(string templateId);
    IEnumerable<NpcSpawnPoint> GetSpawnPoints(int mapId);
    void LoadTemplates(string jsonContent);
}

public class NpcRepository : INpcRepository
{
    private readonly Dictionary<string, NpcTemplate> _templates = new();
    private readonly List<NpcSpawnPoint> _spawnPoints = [];

    public NpcRepository()
    {
        // Temporary: Load default templates and spawn points
        var orcTemplate = new NpcTemplate
        {
            Id = "orc_warrior",
            Name = "Orc Warrior",
            Gender = (byte)Gender.Male,
            Vocation = (byte)VocationType.Warrior,
            BaseHp = 150,
            BaseMp = 50,
            Stats = new NpcStats
            {
                HpRegen = 0.5f,
                MpRegen = 0.2f,
                MovementSpeed = 1.0f,
                AttackSpeed = 1.0f,
                PhysicalAttack = 25,
                MagicAttack = 5,
                PhysicalDefense = 10,
                MagicDefense = 5
            },
            Behavior = new NpcBehaviorConfig
            {
                VisionRange = 8f,
                AttackRange = 1.5f,
                LeashRange = 10f,
                PatrolRadius = 0f
            }
        };
        AddTemplate(orcTemplate);
        AddSpawnPoint(new NpcSpawnPoint { TemplateId = "orc_warrior", MapId = 0, X = 10, Y = 10, Floor = 0 });

        var goblinTemplate = new NpcTemplate
        {
            Id = "goblin",
            Name = "Goblin",
            Gender = (byte)Gender.Male,
            Vocation = (byte)VocationType.Archer,
            BaseHp = 120,
            BaseMp = 80,
            Stats = new NpcStats
            {
                HpRegen = 0.3f,
                MpRegen = 0.4f,
                MovementSpeed = 1.2f,
                AttackSpeed = 1.1f,
                PhysicalAttack = 18,
                MagicAttack = 8,
                PhysicalDefense = 8,
                MagicDefense = 6
            },
            Behavior = new NpcBehaviorConfig
            {
                VisionRange = 6f,
                AttackRange = 5f,
                LeashRange = 12f,
                PatrolRadius = 0f
            }
        };
        AddTemplate(goblinTemplate);
        AddSpawnPoint(new NpcSpawnPoint { TemplateId = "goblin", MapId = 0, X = 20, Y = 20, Floor = 0 });
    }

    public NpcTemplate GetTemplate(string templateId)
    {
        if (_templates.TryGetValue(templateId, out var template))
        {
            return template;
        }
        throw new KeyNotFoundException($"NpcTemplate with Id {templateId} not found.");
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
