namespace Game.ECS.Components;

/// <summary>
/// Marca uma entidade para interpolação visual.
/// Funciona para jogadores remotos, NPCs, projéteis, etc.
/// </summary>
public struct Interpolated
{
    /// <summary>
    /// Velocidade de interpolação (lerp alpha). 
    /// Valores menores = mais suave, valores maiores = mais responsivo.
    /// Padrão: 0.15f
    /// </summary>
    public float LerpSpeed;
    
    /// <summary>
    /// Threshold em pixels para snap (evita "tremor" no destino).
    /// Quando a distância é menor que isso, snapa para o alvo.
    /// Padrão: 2f
    /// </summary>
    public float SnapThresholdPx;
    
    /// <summary>
    /// Fator de extrapolação para compensar latência (0 a 1).
    /// 0 = sem extrapolação, 0.5 = extrapola meio tile à frente.
    /// Padrão: 0.5f
    /// </summary>
    public float ExtrapolationFactor;
    
    /// <summary>
    /// Se true, usa Movement.Timer para suavização intra-célula.
    /// Se false, usa apenas interpolação entre posições.
    /// </summary>
    public bool UseMovementTimer;

    public static Interpolated Default => new()
    {
        LerpSpeed = 0.15f,
        SnapThresholdPx = 2f,
        ExtrapolationFactor = 0.5f,
        UseMovementTimer = false
    };

    public static Interpolated Smooth => new()
    {
        LerpSpeed = 0.1f,
        SnapThresholdPx = 1f,
        ExtrapolationFactor = 0.3f,
        UseMovementTimer = false
    };

    public static Interpolated Responsive => new()
    {
        LerpSpeed = 0.3f,
        SnapThresholdPx = 4f,
        ExtrapolationFactor = 0.7f,
        UseMovementTimer = false
    };
}