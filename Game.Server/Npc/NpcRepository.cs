using Game.Domain.Enums;
using Game.DTOs.Game.Npc;

namespace Game.Server.Npc;

public interface INpcRepository
{
    NpcTemplate GetTemplate(int templateId);
    NpcTemplate GetTemplate(string templateId);
    IEnumerable<NpcSpawnPoint> GetSpawnPoints(int mapId);
    void LoadTemplates(string jsonContent);
}

public class NpcRepository : INpcRepository
{
    private readonly Dictionary<int, NpcTemplate> _templates = new();
    private readonly Dictionary<string, NpcTemplate> _templatesByName = new();
    private readonly List<NpcSpawnPoint> _spawnPoints = [];

    private NpcTemplate[] LoadDefaultTemplates() =>
    [
        new()
        {
            Id = 1,
            Name = "Orc Warrior",
            MovementSpeed = 1.0f,
            AttackSpeed = 0.5f,
            PhysicalAttack = 25,
            MagicAttack = 5,
            PhysicalDefense = 10,
            MagicDefense = 5,
            CurrentHp = 200,
            MaxHp = 200,
            CurrentMp = 50,
            MaxMp = 50,
            HpRegen = 0.5f,
            MpRegen = 0.2f,
            BehaviorType = BehaviorType.Aggressive,
            VisionRange = 8f,
            AttackRange = 1.5f,
            LeashRange = 10f,
            PatrolRadius = 0f,
            IdleDurationMin = 3f,
            IdleDurationMax = 6f
        },
        new()
        {
            Id = 2,
            Name = "Goblin",
            MovementSpeed = 1.2f,
            AttackSpeed = 0.5f,
            PhysicalAttack = 15,
            MagicAttack = 0,
            PhysicalDefense = 5,
            MagicDefense = 2,
            CurrentHp = 100,
            MaxHp = 100,
            CurrentMp = 30,
            MaxMp = 30,
            HpRegen = 0.3f,
            MpRegen = 0.1f,
            BehaviorType = BehaviorType.Aggressive,
            VisionRange = 6f,
            AttackRange = 5f,
            LeashRange = 8f,
            PatrolRadius = 0f,
            IdleDurationMin = 2f,
            IdleDurationMax = 5f
        }
    ];

    private NpcSpawnPoint[] LoadDefaultSpawnPoints() =>
    [
        new(TemplateId: 1, MapId: 0, Floor: 0, X: 10, Y: 15),
        new(TemplateId: 2, MapId: 0, Floor: 0, X: 20, Y: 25)
    ];

    public NpcRepository()
    {
        foreach (var template in LoadDefaultTemplates())
        {
            _templates[template.Id] = template;
            _templatesByName[template.Name.ToLowerInvariant()] = template;
        }
        _spawnPoints.AddRange(LoadDefaultSpawnPoints());
    }

    public NpcTemplate GetTemplate(int templateId) =>
        _templates.TryGetValue(templateId, out var template)
            ? template
            : throw new KeyNotFoundException($"NpcTemplate with Id {templateId} not found.");

    public NpcTemplate GetTemplate(string templateId) =>
        _templatesByName.TryGetValue(templateId.ToLowerInvariant(), out var template)
            ? template
            : throw new KeyNotFoundException($"NpcTemplate with name '{templateId}' not found.");

    public IEnumerable<NpcSpawnPoint> GetSpawnPoints(int mapId) =>
        _spawnPoints.Where(s => s.MapId == mapId);

    public void LoadTemplates(string jsonContent)
    {
        // TODO: Implement JSON deserialization
    }

    public void AddTemplate(NpcTemplate template)
    {
        _templates[template.Id] = template;
        _templatesByName[template.Name.ToLowerInvariant()] = template;
    }

    public void AddSpawnPoint(NpcSpawnPoint spawnPoint) =>
        _spawnPoints.Add(spawnPoint);
}