namespace Game.ECS.Navigation.Client.Contracts;

/// <summary>
/// Interface para provider de input (abstrai implementação específica).
/// </summary>
public interface IInputProvider
{
    bool IsClickPressed();
    (float X, float Y) GetClickWorldPosition();
    (float X, float Y) GetMovementAxis(); // Para movimento WASD
}