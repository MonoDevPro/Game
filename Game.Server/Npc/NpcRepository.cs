using Game.Domain.Enums;
using Game.ECS.Schema.Components;
using Game.ECS.Schema.Templates;

namespace Game.Server.Npc;

/// <summary>
/// NpcSpawnPoint define um ponto de spawn com referência ao template do NPC.
/// TemplateId refere-se ao Id do NpcTemplate, não ao NetworkId.
/// </summary>
public readonly record struct NpcSpawnPoint(
    int TemplateId,
    int MapId,
    sbyte Floor,
    int X,
    int Y
);

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
        new() // Orc Warrior Template
        {
            Id = 1,
            IdentityTemplate = new IdentityTemplate(
                NetworkId: 0, // será atribuído no spawn
                Name: "Orc Warrior",
                Gender: Gender.Male,
                Vocation: VocationType.Warrior
            ),
            DirectionTemplate = new DirectionTemplate(DirX: 0, DirY: 1),
            LocationTemplate = new LocationTemplate(MapId: 0, Floor: 0, X: 0, Y: 0),
            StatsTemplate = new StatsTemplate(
                MovementSpeed: 1.0f,
                AttackSpeed: 1.0f,
                PhysicalAttack: 25,
                MagicAttack: 5,
                PhysicalDefense: 10,
                MagicDefense: 5
            ),
            VitalsTemplate = new VitalsTemplate(
                CurrentHp: 200,
                MaxHp: 200,
                CurrentMp: 50,
                MaxMp: 50,
                HpRegen: 0.5f,
                MpRegen: 0.2f
            ),
            BehaviorTemplate = new BehaviorTemplate(
                BehaviorType: BehaviorType.Aggressive,
                VisionRange: 8f,
                AttackRange: 1.5f,
                LeashRange: 10f,
                PatrolRadius: 0f,
                IdleDurationMin: 3f,
                IdleDurationMax: 6f
            )
        },
        
        new() // Goblin Template
        {
            Id = 2,
            IdentityTemplate = new IdentityTemplate(
                NetworkId: 0,
                Name: "Goblin",
                Gender: Gender.Male,
                Vocation: VocationType.Archer
            ),
            DirectionTemplate = new DirectionTemplate(DirX: 0, DirY: 1),
            LocationTemplate = new LocationTemplate(MapId: 0, Floor: 0, X: 0, Y: 0),
            StatsTemplate = new StatsTemplate(
                MovementSpeed: 1.2f,
                AttackSpeed: 1.5f,
                PhysicalAttack: 15,
                MagicAttack: 0,
                PhysicalDefense: 5,
                MagicDefense: 2
            ),
            VitalsTemplate = new VitalsTemplate(
                CurrentHp: 100,
                MaxHp: 100,
                CurrentMp: 30,
                MaxMp: 30,
                HpRegen: 0.3f,
                MpRegen: 0.1f
            ),
            BehaviorTemplate = new BehaviorTemplate(
                BehaviorType: BehaviorType.Aggressive,
                VisionRange: 6f,
                AttackRange: 5f,
                LeashRange: 8f,
                PatrolRadius: 0f,
                IdleDurationMin: 2f,
                IdleDurationMax: 5f
            )
        }
    ];
    
    private NpcSpawnPoint[] LoadDefaultSpawnPoints() =>
    [
        // Orc Warrior Spawn
        new NpcSpawnPoint(TemplateId: 1, MapId: 1, Floor: 0, X: 10, Y: 15),
        // Goblin Spawn
        new NpcSpawnPoint(TemplateId: 2, MapId: 1, Floor: 0, X: 20, Y: 25)
    ];
        

    public NpcRepository()
    {
        // Load default templates and spawn points
        foreach (var template in LoadDefaultTemplates())
        {
            _templates[template.Id] = template;
            _templatesByName[template.IdentityTemplate.Name.ToLowerInvariant()] = template;
        }
        _spawnPoints.AddRange(LoadDefaultSpawnPoints());
    }

    public NpcTemplate GetTemplate(int templateId)
    {
        return _templates.TryGetValue(templateId, out var template) 
            ? template 
            : throw new KeyNotFoundException($"NpcTemplate with Id {templateId} not found.");
    }
    
    public NpcTemplate GetTemplate(string templateId)
    {
        return _templatesByName.TryGetValue(templateId.ToLowerInvariant(), out var template) 
            ? template 
            : throw new KeyNotFoundException($"NpcTemplate with name '{templateId}' not found.");
    }

    public IEnumerable<NpcSpawnPoint> GetSpawnPoints(int mapId)
    {
        return _spawnPoints.Where(s => s.MapId == mapId);
    }

    public void LoadTemplates(string jsonContent)
    {
        // TODO: Implement JSON deserialization
    }
    
    public void AddTemplate(NpcTemplate template)
    {
        _templates[template.Id] = template;
        _templatesByName[template.IdentityTemplate.Name.ToLowerInvariant()] = template;
    }

    public void AddSpawnPoint(NpcSpawnPoint spawnPoint)
    {
        _spawnPoints.Add(spawnPoint);
    }
}