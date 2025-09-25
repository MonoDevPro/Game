using Arch.Core;
using Simulation.Core.Ports.ECS;

namespace Simulation.Core.ECS.Systems.Resources;

/// <summary>
/// Recurso que expõe o índice de jogadores como interface injetável.
/// É mantido exclusivamente pelo IndexSystem; os demais sistemas apenas consomem a interface.
/// </summary>
public sealed class PlayerIndexResource(World world) : IPlayerIndex
{
    private readonly Dictionary<int, Entity> _playersByCharId = new();

    // API pública consumida por outros sistemas
    public bool TryGetPlayerEntity(int playerId, out Entity entity)
    {
        if (_playersByCharId.TryGetValue(playerId, out entity))
        {
            if (world.IsAlive(entity))
                return true;

            // Auto-correção se a entidade morreu
            _playersByCharId.Remove(playerId);
        }

        entity = default;
        return false;
    }

    // Métodos internos usados apenas pelo IndexSystem
    internal void Index(int charId, in Entity entity)
    {
        _playersByCharId[charId] = entity;
    }

    internal void Unindex(int charId)
    {
        _playersByCharId.Remove(charId);
    }
}