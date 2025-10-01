namespace GameWeb.Domain.Constants;

public abstract class Policies
{
    public const string CanDeletePlayers = nameof(CanDeletePlayers);
    public const string CanReadPlayers = nameof(CanReadPlayers);
    public const string InternalOnly = nameof(InternalOnly);
}
