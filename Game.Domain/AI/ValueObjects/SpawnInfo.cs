namespace Game.Domain.AI.ValueObjects;

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