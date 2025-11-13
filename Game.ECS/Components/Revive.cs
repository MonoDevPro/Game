namespace Game.ECS.Components;

/// <summary>
/// Componente que controla o processo de revive de uma entidade morta.
/// Quando uma entidade morre, este componente rastreia o tempo até o revive.
/// </summary>
public struct Revive
{
    /// <summary>
    /// Tempo restante até o revive (em segundos).
    /// </summary>
    public float TimeRemaining;
    
    /// <summary>
    /// Tempo total de espera para revive (em segundos).
    /// </summary>
    public float TotalTime;
    
    /// <summary>
    /// Posição de spawn para revive.
    /// </summary>
    public Position SpawnPosition;
}