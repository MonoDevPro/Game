using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.Shared.Components;

namespace Simulation.Core.Server.Systems;

public sealed partial class PlayerIndexSystem(World world) : BaseSystem<World, float>(world)
{
    // O índice é privado e gerenciado inteiramente por este sistema
    private readonly Dictionary<int, Entity> _playersByCharId = new();

    [Query]
    [All<PlayerId>]
    [None<Index>]
    private void AddNewPlayers(in Entity entity, ref PlayerId playerId)
    {
        _playersByCharId[playerId.Value] = entity;
        Console.WriteLine($"Indexing player {playerId.Value} with entity {entity}");
        World.Add<Indexed>(entity); // Marca como indexado
    }

    public bool TryGetEntity(int charId, out Entity entity)
    {
        if (_playersByCharId.TryGetValue(charId, out entity))
        {
            // Verificação crucial: garante que não estamos retornando uma entidade "morta"
            // que já foi destruída mas ainda não foi removida do índice.
            if (World.IsAlive(entity))
            {
                return true;
            }
            
            // Auto-correção: remove a entidade morta do índice.
            _playersByCharId.Remove(charId);
        }
        entity = default;
        return false;
    }
}