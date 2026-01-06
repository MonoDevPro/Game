using Game.Domain.Data;

namespace Game.Persistence.Interfaces;

/// <summary>
/// Interface pública para serviço de persistência de dados de personagens.
/// Autor: MonoDevPro
/// Data: 2025-10-13 20:30:00
/// </summary>
public interface IPlayerPersistenceService
{
    /// <summary>
    /// Persiste dados completos do personagem ao desconectar (posição, direção, vitals).
    /// </summary>
    Task PersistDisconnectAsync(
        CharacterState dto, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste apenas os vitals (HP/MP) do personagem (operação rápida).
    /// </summary>
    Task PersistVitalsAsync(
        VitalsState dto, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste stats completos do personagem.
    /// </summary>
    Task PersistStatsAsync(
        StatsState dto, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste inventário completo do personagem (operação pesada).
    /// </summary>
    Task PersistInventoryAsync(
        InventoryState dto, 
        CancellationToken cancellationToken = default);
}