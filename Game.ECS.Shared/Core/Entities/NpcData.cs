using Game.ECS.Shared.Components.Navigation;
using MemoryPack;

namespace Game.ECS.Shared.Core.Entities;

[MemoryPackable]
public partial struct NpcData
{
    public int NetworkId { get; set; }
    public string Name { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
    public MovementDirection Direction { get; set; }
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int Mp { get; set; }
    public int MaxMp { get; set; }
    public float MovementSpeed { get; set; }
    public float AttackSpeed { get; set; }
    public int PhysicalAttack { get; set; }
    public int MagicAttack { get; set; }
    public int PhysicalDefense { get; set; }
    public int MagicDefense { get; set; }
}