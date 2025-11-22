using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Logic;

public static class DeathLogic
{
    /// <summary>
    /// Verifica se uma entidade está morta com base em seus pontos de vida.
    /// </summary>
    public static bool CheckDeath(in Health health)
    {
        return health.Current <= 0;
    }
}