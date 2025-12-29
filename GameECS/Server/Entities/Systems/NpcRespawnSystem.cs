using Arch.Core;
using Arch.LowLevel;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Domain.AI.Enums;
using Game.Domain.AI.ValueObjects;
using Game.Domain.Combat.ValueObjects;
using Game.Domain.Enums;
using Game.Domain.ValueObjects.Identitys;
using Game.Domain.ValueObjects.Map;
using Game.Domain.ValueObjects.Vitals;

namespace GameECS.Server.Entities.Systems;

/// <summary>
/// Sistema de respawn de NPCs.
/// </summary>
public sealed partial class NpcRespawnSystem(World world) : BaseSystem<World, long>(world)
{
    private readonly UnsafeStack<Entity> _toRespawnStack = new(16);

    public override void BeforeUpdate(in long t)
    {
        base.BeforeUpdate(t);

        _toRespawnStack.Clear();
    }

    [Query]
    [All<Identity, SpawnInfo>, None<Dead>]
    private void Update([Data] in long tick, in Entity entity, ref Identity identity, ref SpawnInfo spawn)
    {
        // Coleta entidades para respawn
        if (identity.Type == EntityType.Npc && spawn.ShouldRespawn(tick))
        {
            _toRespawnStack.Push(entity);
        }
    }

    public override void AfterUpdate(in long t)
    {
        base.AfterUpdate(t);

        for (int i = 0; i < _toRespawnStack.Count; i++)
        {
            var e = _toRespawnStack.Pop();

            ref var health = ref world.Get<Health>(e);
            ref var position = ref world.Get<GridPosition>(e);
            ref var ai = ref world.Get<NpcAI>(e);
            ref var spawn = ref world.Get<SpawnInfo>(e);

            health.Reset();
            position.X = spawn.SpawnX;
            position.Y = spawn.SpawnY;
            ai.State = NpcAIState.Idle;
            ai.TargetEntityId = 0;
            spawn.DeathTick = 0;

            world.Remove<Dead>(e);
        }
    }
}