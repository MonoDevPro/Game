using Arch.Core;

namespace Game.ECS.Components;

public struct Damaged
{
    /// <summary>
    /// Quantidade de dano a ser aplicada.
    /// </summary>
    public int Amount;

    /// <summary>
    /// Indica se o dano é crítico.
    /// </summary>
    public bool IsCritical;

    /// <summary>
    /// Identificador da fonte do dano (pode ser entidade atacante ou habilidade).
    /// </summary>
    public Entity SourceEntity;
}