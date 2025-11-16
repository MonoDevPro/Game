using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

/// <summary>
/// Sistema responsável por atualizar o estado de combate dos jogadores.
/// Mantém um timer desde o último hit e limpa o flag de combate
/// após um período configurado, permitindo voltar a regenerar HP/MP.
/// </summary>
public sealed partial class CombatSystem(World world) : GameSystem(world)
{
    [Query]
    [All<CombatState>]
    [None<Dead>] // morto não entra/sai de combate
    private void UpdateCombatState(ref CombatState combat, [Data] float deltaTime)
    {
        if (!combat.InCombat)
            return;

        combat.TimeSinceLastHit += deltaTime;

        if (combat.TimeSinceLastHit >= SimulationConfig.HealthRegenDelayAfterCombat)
        {
            combat.InCombat = false;
            combat.TimeSinceLastHit = SimulationConfig.HealthRegenDelayAfterCombat;
        }
    }
}