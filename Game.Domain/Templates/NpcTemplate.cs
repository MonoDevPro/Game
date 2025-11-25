namespace Game.Domain.Templates;

public class NpcTemplate
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int BaseHp { get; set; }
    public int BaseMp { get; set; }
    public int Gender { get; set; }
    public int Vocation { get; set; }
    
    public NpcStats Stats { get; set; }
    public NpcBehaviorConfig Behavior { get; set; }
}

public class NpcStats 
{
    public float MovementSpeed { get; set; }
    public float AttackSpeed { get; set; }
    public int PhysicalAttack { get; set; }
    public int MagicAttack { get; set; }
    public int PhysicalDefense { get; set; }
    public int MagicDefense { get; set; }
    public float HpRegen { get; set; }
    public float MpRegen { get; set; }
}

public class NpcBehaviorConfig
{
    public int Type { get; set; }
    public float VisionRange { get; set; }
    public float AttackRange { get; set; }
    public float LeashRange { get; set; }
    public float PatrolRadius { get; set; }
    public float IdleDurationMin { get; set; }
    public float IdleDurationMax { get; set; }
}

public class NpcSpawnPoint
{
    public string TemplateId { get; set; }
    public int MapId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Floor { get; set; }
}
