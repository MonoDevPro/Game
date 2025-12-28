namespace Game.Domain.Navigation.ValueObjects;

/// <summary>
/// Tag: entidade bloqueada aguardando.
/// </summary>
public struct WaitingForPath
{
    public long StartTick;
    public int BlockerId;
}