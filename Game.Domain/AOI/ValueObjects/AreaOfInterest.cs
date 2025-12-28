namespace Game.Domain.AOI.ValueObjects;

/// <summary>
/// Área de interesse da entidade.
/// </summary>
public struct AreaOfInterest(int viewRadius)
{
    public int ViewRadius = viewRadius;
    public long LastUpdateTick = 0;
}