using Game.ECS.Shared.Core.Entities;
using MemoryPack;

namespace Game.ECS.Shared.Data.Combat;

[MemoryPackable]
public readonly partial record struct AttackPacket(AttackData[] Attacks);

[MemoryPackable]
public readonly partial record struct VitalsPacket(VitalsData[] Vitals);