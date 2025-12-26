using Game.Domain.ValueObjects.Character;

namespace Game.Domain.Specifications.Combat;

/// <summary>
/// Especificação para verificar se um alvo está dentro do alcance de ataque.
/// </summary>
public class IsInAttackRangeSpecification
{
    /// <summary>
    /// Verifica se o alvo está dentro do alcance.
    /// </summary>
    /// <param name="attackerPosition">A posição do atacante.</param>
    /// <param name="targetPosition">A posição do alvo.</param>
    /// <param name="attackRange">O alcance de ataque.</param>
    /// <returns>True se o alvo está dentro do alcance, false caso contrário.</returns>
    public bool IsSatisfiedBy(Position attackerPosition, Position targetPosition, float attackRange)
    {
        double distance = attackerPosition.EuclideanDistance(targetPosition);
        return distance <= attackRange;
    }

    /// <summary>
    /// Calcula a distância até o alvo.
    /// </summary>
    /// <param name="attackerPosition">A posição do atacante.</param>
    /// <param name="targetPosition">A posição do alvo.</param>
    /// <returns>A distância euclidiana entre o atacante e o alvo.</returns>
    public double GetDistanceToTarget(Position attackerPosition, Position targetPosition)
    {
        return attackerPosition.EuclideanDistance(targetPosition);
    }

    /// <summary>
    /// Verifica se o atacante precisa se mover para alcançar o alvo.
    /// </summary>
    /// <param name="attackerPosition">A posição do atacante.</param>
    /// <param name="targetPosition">A posição do alvo.</param>
    /// <param name="attackRange">O alcance de ataque.</param>
    /// <returns>True se o atacante precisa se mover, false se o alvo já está no alcance.</returns>
    public bool NeedsToMove(Position attackerPosition, Position targetPosition, float attackRange)
    {
        return !IsSatisfiedBy(attackerPosition, targetPosition, attackRange);
    }
}
