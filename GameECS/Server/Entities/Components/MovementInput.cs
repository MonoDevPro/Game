namespace GameECS.Server.Entities.Components;

/// <summary>
/// Input de movimento pendente para a entidade.
/// </summary>
public struct MovementInput
{
    public int TargetX;
    public int TargetY;
    public byte RetryCount;
    public bool HasInput;

    public void Set(int targetX, int targetY)
    {
        TargetX = targetX;
        TargetY = targetY;
        RetryCount = 0;
        HasInput = true;
    }

    public void Clear()
    {
        HasInput = false;
        RetryCount = 0;
    }
}
