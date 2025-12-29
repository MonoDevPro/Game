using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Domain.AI.Enums;
using Game.Domain.AI.ValueObjects;
using Game.Domain.Combat.ValueObjects;

namespace GameECS.Systems;

/// <summary>
/// Sistema de aggro.
/// </summary>
public sealed partial class AggroSystem(World world) : BaseSystem<World,long>(world), IDisposable
{
    [Query]
    [All<AggroTable, NpcAI>, None<Dead>]
    private void Update([Data] in long tick, ref AggroTable aggro, ref NpcAI ai)
    {
            // Decay de 1% por tick quando fora de combate
        if (ai.State == NpcAIState.Idle || ai.State == NpcAIState.Returning)
        {
            aggro.DecayThreat(0.01f);
        }
    }
}