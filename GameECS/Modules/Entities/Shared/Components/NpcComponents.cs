using System.Runtime.CompilerServices;
using GameECS.Modules.Entities.Shared.Data;

namespace GameECS.Modules.Entities.Shared.Components;

/// <summary>
/// Comportamento de NPC.
/// </summary>
public struct NpcBehavior
{
    public NpcBehaviorType Type;
    public NpcSubType SubType;
    public int WanderRadius;
    public int AggroRange;
    public int LeashRange;
}

/// <summary>
/// Estado de IA do NPC.
/// </summary>
public struct NpcAI
{
    public NpcAIState State;
    public int TargetEntityId;
    public long StateChangeTick;
    public long NextActionTick;
}

/// <summary>
/// Tabela de aggro (threat).
/// </summary>
public unsafe struct AggroTable
{
    public const int MaxEntries = 16;

    private fixed int _entityIds[MaxEntries];
    private fixed int _threats[MaxEntries];
    public byte Count;

    public readonly bool HasThreat => Count > 0;

    public void AddThreat(int entityId, int amount)
    {
        // Procura existente
        for (int i = 0; i < Count; i++)
        {
            if (_entityIds[i] == entityId)
            {
                _threats[i] += amount;
                return;
            }
        }

        // Adiciona novo se tem espaço
        if (Count < MaxEntries)
        {
            _entityIds[Count] = entityId;
            _threats[Count] = amount;
            Count++;
        }
    }

    public void RemoveThreat(int entityId, int amount)
    {
        for (int i = 0; i < Count; i++)
        {
            if (_entityIds[i] == entityId)
            {
                _threats[i] = Math.Max(0, _threats[i] - amount);
                return;
            }
        }
    }

    public readonly int GetThreat(int entityId)
    {
        for (int i = 0; i < Count; i++)
        {
            if (_entityIds[i] == entityId)
                return _threats[i];
        }
        return 0;
    }

    public readonly bool TryGetHighestThreat(out int entityId, out int threat)
    {
        entityId = 0;
        threat = 0;

        for (int i = 0; i < Count; i++)
        {
            if (_threats[i] > threat)
            {
                entityId = _entityIds[i];
                threat = _threats[i];
            }
        }
        return threat > 0;
    }

    public void Clear() => Count = 0;

    public void DecayThreat(float percentage)
    {
        for (int i = 0; i < Count; i++)
        {
            _threats[i] = (int)(_threats[i] * (1f - percentage));
        }
    }
}

/// <summary>
/// Informação de spawn/respawn.
/// </summary>
public struct SpawnInfo
{
    public int SpawnX;
    public int SpawnY;
    public int RespawnDelayTicks;
    public long DeathTick;

    public readonly bool ShouldRespawn(long currentTick)
        => DeathTick > 0 && currentTick >= DeathTick + RespawnDelayTicks;
}

/// <summary>
/// Comportamento de Pet.
/// </summary>
public struct PetBehavior
{
    public PetMode Mode;
    public int FollowDistance;
    public int AttackRange;
}

/// <summary>
/// Estado do Pet.
/// </summary>
public struct PetState
{
    public bool IsChasing;
    public bool IsAttacking;
    public int TargetEntityId;
}
