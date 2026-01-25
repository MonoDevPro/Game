using Game.Infrastructure.ArchECS.Commons.Components;

namespace Game.Infrastructure.ArchECS.Services.Navigation.Components;

/// <summary>
/// Tipo de movimento direcional.
/// </summary>
public enum DirectionalMovementType : byte
{
    /// <summary>
    /// Movimento único - move uma célula na direção e para.
    /// </summary>
    Single = 0,
    
    /// <summary>
    /// Movimento contínuo - continua movendo enquanto o componente existir.
    /// </summary>
    Continuous = 1
}

/// <summary>
/// Requisição de movimento direcional (manual).
/// Usado para movimento baseado em input do jogador (WASD, setas, etc).
/// </summary>
public struct NavDirectionalRequest
{
    /// <summary>
    /// Direção do movimento (-1, 0 ou 1 para X e Y).
    /// </summary>
    public Direction Direction;
    
    /// <summary>
    /// Tipo de movimento (único ou contínuo).
    /// </summary>
    public DirectionalMovementType MovementType;
    
    /// <summary>
    /// Flags opcionais para o movimento.
    /// </summary>
    public PathRequestFlags Flags;
}

/// <summary>
/// Tag indicando que entidade está em modo de movimento direcional.
/// Diferencia de movimento por pathfinding.
/// </summary>
public struct NavDirectionalMode;
