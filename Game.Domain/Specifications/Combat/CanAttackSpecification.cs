using Game.Domain.ValueObjects.Character;

namespace Game.Domain.Specifications.Combat;

/// <summary>
/// Especificação para validar se uma entidade pode atacar outra.
/// </summary>
public class CanAttackSpecification
{
    /// <summary>
    /// Verifica se o atacante pode atacar o alvo.
    /// </summary>
    /// <param name="attackerPosition">A posição do atacante.</param>
    /// <param name="targetPosition">A posição do alvo.</param>
    /// <param name="attackRange">O alcance de ataque.</param>
    /// <param name="attackerIsAlive">Indica se o atacante está vivo.</param>
    /// <param name="targetIsAlive">Indica se o alvo está vivo.</param>
    /// <returns>True se o atacante pode atacar o alvo, false caso contrário.</returns>
    public bool IsSatisfiedBy(
        Position attackerPosition,
        Position targetPosition,
        float attackRange,
        bool attackerIsAlive,
        bool targetIsAlive)
    {
        if (!attackerIsAlive || !targetIsAlive)
            return false;
        
        double distance = attackerPosition.EuclideanDistance(targetPosition);
        return distance <= attackRange;
    }

    /// <summary>
    /// Retorna a razão pela qual o ataque falhou, se aplicável.
    /// </summary>
    /// <param name="attackerPosition">A posição do atacante.</param>
    /// <param name="targetPosition">A posição do alvo.</param>
    /// <param name="attackRange">O alcance de ataque.</param>
    /// <param name="attackerIsAlive">Indica se o atacante está vivo.</param>
    /// <param name="targetIsAlive">Indica se o alvo está vivo.</param>
    /// <returns>Mensagem de erro se houver alguma restrição, ou string vazia se não houver problemas.</returns>
    public string GetFailureReason(
        Position attackerPosition,
        Position targetPosition,
        float attackRange,
        bool attackerIsAlive,
        bool targetIsAlive)
    {
        if (!attackerIsAlive)
            return "Attacker is dead";
        
        if (!targetIsAlive)
            return "Target is dead";
        
        double distance = attackerPosition.EuclideanDistance(targetPosition);
        if (distance > attackRange)
            return $"Target out of range (distance: {distance:F2}, range: {attackRange})";
        
        return string.Empty;
    }
}
