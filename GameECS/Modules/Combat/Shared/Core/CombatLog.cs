using System.Collections.Concurrent;
using GameECS.Modules.Combat.Shared.Data;

namespace GameECS.Modules.Combat.Shared.Core;

/// <summary>
/// Registro de combate para tracking e estatísticas.
/// Thread-safe.
/// </summary>
public sealed class CombatLog
{
    private readonly ConcurrentQueue<CombatLogEntry> _entries;
    private readonly int _maxEntries;

    public CombatLog(int maxEntries = 1000)
    {
        _maxEntries = maxEntries;
        _entries = new ConcurrentQueue<CombatLogEntry>();
    }

    public void LogDamage(DamageInfo damage)
    {
        var entry = new CombatLogEntry
        {
            Type = CombatLogType.Damage,
            AttackerId = damage.AttackerId,
            TargetId = damage.TargetId,
            Value = damage.FinalDamage,
            DamageType = damage.Type,
            IsCritical = damage.IsCritical,
            Tick = damage.Tick
        };

        AddEntry(entry);
    }

    public void LogDeath(int entityId, int killerId, long tick)
    {
        var entry = new CombatLogEntry
        {
            Type = CombatLogType.Death,
            AttackerId = killerId,
            TargetId = entityId,
            Tick = tick
        };

        AddEntry(entry);
    }

    public void LogHeal(int healerId, int targetId, int amount, long tick)
    {
        var entry = new CombatLogEntry
        {
            Type = CombatLogType.Heal,
            AttackerId = healerId,
            TargetId = targetId,
            Value = amount,
            Tick = tick
        };

        AddEntry(entry);
    }

    private void AddEntry(CombatLogEntry entry)
    {
        _entries.Enqueue(entry);

        // Limpa entradas antigas se exceder limite
        while (_entries.Count > _maxEntries)
        {
            _entries.TryDequeue(out _);
        }
    }

    public IEnumerable<CombatLogEntry> GetRecentEntries(int count = 100)
    {
        return _entries.TakeLast(count);
    }

    public IEnumerable<CombatLogEntry> GetEntriesForEntity(int entityId)
    {
        return _entries.Where(e => e.AttackerId == entityId || e.TargetId == entityId);
    }

    public void Clear()
    {
        while (_entries.TryDequeue(out _)) { }
    }
}

public readonly struct CombatLogEntry
{
    public CombatLogType Type { get; init; }
    public int AttackerId { get; init; }
    public int TargetId { get; init; }
    public int Value { get; init; }
    public DamageType DamageType { get; init; }
    public bool IsCritical { get; init; }
    public long Tick { get; init; }
}

public enum CombatLogType : byte
{
    Damage,
    Death,
    Heal,
    Miss,
    Block,
    Evade
}
