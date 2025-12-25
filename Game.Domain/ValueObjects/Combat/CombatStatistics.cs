using System.Runtime.InteropServices;

namespace Game.Domain.ValueObjects.Combat;

/// <summary>
/// Estatísticas de combate para tracking de performance.
/// Component ECS para armazenar estatísticas de combate de uma entidade.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CombatStatistics
{
    public int TotalDamageDealt;
    public int TotalDamageReceived;
    public int TotalKills;
    public int TotalDeaths;
    public int CriticalHits;
    public int AttacksMade;
    public int AttacksReceived;

    public readonly float AverageDamageDealt => AttacksMade > 0 ? (float)TotalDamageDealt / AttacksMade : 0;
    public readonly float AverageDamageReceived => AttacksReceived > 0 ? (float)TotalDamageReceived / AttacksReceived : 0;
    public readonly float CriticalHitRate => AttacksMade > 0 ? (float)CriticalHits / AttacksMade : 0;
    public readonly float KillDeathRatio => TotalDeaths > 0 ? (float)TotalKills / TotalDeaths : TotalKills;

    public void RecordDamageDealt(int damage, bool isCritical)
    {
        TotalDamageDealt += damage;
        AttacksMade++;
        if (isCritical) CriticalHits++;
    }

    public void RecordDamageReceived(int damage)
    {
        TotalDamageReceived += damage;
        AttacksReceived++;
    }

    public void RecordKill() => TotalKills++;
    public void RecordDeath() => TotalDeaths++;

    public static CombatStatistics Zero => default;
}
