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
    public bool IsSatisfiedBy(Position attackerPosition, Position targetPosition, float attackRange)
    {
        double distance = attackerPosition.EuclideanDistance(targetPosition);
        return distance <= attackRange;
    }

    /// <summary>
    /// Calcula a distância até o alvo.
    /// </summary>
    public double GetDistanceToTarget(Position attackerPosition, Position targetPosition)
    {
        return attackerPosition.EuclideanDistance(targetPosition);
    }

    /// <summary>
    /// Verifica se o atacante precisa se mover para alcançar o alvo.
    /// </summary>
    public bool NeedsToMove(Position attackerPosition, Position targetPosition, float attackRange)
    {
        return !IsSatisfiedBy(attackerPosition, targetPosition, attackRange);
    }
}
