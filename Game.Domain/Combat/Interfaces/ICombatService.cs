using Game.Domain.Combat.Enums;
using Game.Domain.Combat.ValueObjects;

namespace Game.Domain.Combat.Interfaces;

/// <summary>
/// Interface para o serviço de combate.
/// Implementado na camada ECS.
/// </summary>
public interface ICombatService
{
    /// <summary>
    /// Valida se um ataque pode ser executado.
    /// </summary>
    AttackResult ValidateAttack(
        int attackerEntityId,
        int targetEntityId,
        int attackerX, int attackerY,
        int targetX, int targetY,
        long currentTick);

    /// <summary>
    /// Executa um ataque básico e retorna o resultado.
    /// </summary>
    (AttackResult result, DamageMessage? damage) ExecuteBasicAttack(
        int attackerEntityId,
        int targetEntityId,
        int attackerX, int attackerY,
        int targetX, int targetY,
        long currentTick);
}
