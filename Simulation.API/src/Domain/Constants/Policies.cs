namespace GameWeb.Domain.Constants;

public abstract class Policies
{
    public const string CanPurge = nameof(CanPurge);
    public const string CanManageUsers = nameof(CanManageUsers);
    public const string InternalOnly = nameof(InternalOnly);
}
