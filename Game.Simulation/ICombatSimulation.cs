using Game.Contracts;
using Game.Infrastructure.ArchECS.Services.Combat;

namespace Game.Simulation;

/// <summary>
/// Interface opcional para simulações que suportam combate.
/// Mantém o contrato isolado para plug-in gradual.
/// </summary>
public interface ICombatSimulation
{
    bool RequestBasicAttack(int characterId, int dirX, int dirY);
    bool TryDrainCombatEvents(out List<CombatEvent> events);
    bool TryDrainCombatVitals(out List<CombatVitalUpdate> updates);
}
