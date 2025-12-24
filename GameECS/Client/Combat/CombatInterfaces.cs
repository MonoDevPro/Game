using GameECS.Shared.Combat.Data;

namespace GameECS.Client.Combat;

/// <summary>
/// Interface para envio de comandos de combate para o servidor.
/// </summary>
public interface ICombatNetworkSender
{
    /// <summary>
    /// Envia requisição de ataque para o servidor.
    /// </summary>
    void SendAttackRequest(int targetEntityId);

    /// <summary>
    /// Envia requisição de seleção de alvo.
    /// </summary>
    void SendTargetSelection(int targetEntityId);

    /// <summary>
    /// Envia requisição de cancelamento de ataque.
    /// </summary>
    void SendCancelAttack();
}

/// <summary>
/// Interface para recebimento de eventos de combate do servidor.
/// </summary>
public interface ICombatNetworkReceiver
{
    /// <summary>
    /// Chamado quando recebe atualização de dano.
    /// </summary>
    void OnDamageReceived(DamageMessage message);

    /// <summary>
    /// Chamado quando recebe atualização de morte.
    /// </summary>
    void OnDeathReceived(DeathMessage message);

    /// <summary>
    /// Chamado quando recebe atualização de vida.
    /// </summary>
    void OnHealthUpdated(int entityId, int current, int max, long serverTick);

    /// <summary>
    /// Chamado quando recebe atualização de mana.
    /// </summary>
    void OnManaUpdated(int entityId, int current, int max, long serverTick);
}

/// <summary>
/// Interface para input de combate do jogador.
/// </summary>
public interface ICombatInputProvider
{
    /// <summary>
    /// Verifica se o botão de ataque foi pressionado.
    /// </summary>
    bool IsAttackPressed();

    /// <summary>
    /// Obtém o ID da entidade sob o cursor/toque, se houver.
    /// </summary>
    int? GetTargetUnderCursor();

    /// <summary>
    /// Verifica se o alvo foi selecionado (click/tap).
    /// </summary>
    bool IsTargetSelected();
}
