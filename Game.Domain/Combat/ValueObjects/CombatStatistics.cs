namespace Game.Domain.Combat.ValueObjects;

/// <summary>
/// Estatísticas de combate para o servidor.
/// </summary>
public struct CombatStatistics
{
    public int TotalDamageDealt;
    public int TotalDamageReceived;
    public int TotalKills;
    public int TotalDeaths;
    public int CriticalHits;
    public int AttacksMade;
    public int AttacksReceived;

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
}
