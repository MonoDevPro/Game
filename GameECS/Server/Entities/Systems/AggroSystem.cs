using Arch.Core;
using Game.Domain.AI.Enums;
using Game.Domain.AI.ValueObjects;
using Game.Domain.ValueObjects.Combat;

namespace GameECS.Server.Entities.Systems;

/// <summary>
/// Sistema de aggro.
/// </summary>
public sealed class AggroSystem(World world) : IDisposable
{
    private readonly QueryDescription _aggroQuery = new QueryDescription()
        .WithAll<AggroTable, NpcAI>()
        .WithNone<Dead>();

    public void Update(long tick)
    {
        world.Query(in _aggroQuery, (ref AggroTable aggro, ref NpcAI ai) =>
        {
            // Decay de 1% por tick quando fora de combate
            if (ai.State == NpcAIState.Idle || ai.State == NpcAIState.Returning)
            {
                aggro.DecayThreat(0.01f);
            }
        });
    }

    public void Dispose() { }
}