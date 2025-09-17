using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Indexes;
using Simulation.Core.ECS.Pipeline;

namespace Simulation.Core.ECS.Systems;

/// <summary>
/// Sistema central responsável por manter índices de alta performance
/// para as entidades principais do mundo, como Mapas e Jogadores.
/// </summary>
 [PipelineSystem(SystemStage.Logic, -50)] // cedo na fase lógica
public sealed partial class IndexSystem(World world) : 
    BaseSystem<World, float>(world), 
    IPlayerIndex
{
    // Índices privados que oferecem busca O(1)
    private readonly Dictionary<int, Entity> _playersByCharId = new();
    
    // --- Ciclo de Vida do Índice ---

    [Query]
    [All<PlayerId>] [None<Indexed>]
    private void IndexPlayers(in Entity entity, ref PlayerId playerId)
    {
        _playersByCharId[playerId.Value] = entity;
        World.Add<Indexed>(entity);
    }
    
    [Query]
    [All<PlayerId, Indexed, Unindexed>]
    private void UnindexPlayers(in Entity entity, ref PlayerId playerId)
    {
        World.Remove<Indexed>(entity);
        _playersByCharId.Remove(playerId.Value);
    }

    // --- API Pública do Índice ---

    /// <summary>
    /// Obtém a entidade e o serviço de um mapa pelo seu ID, se existir e estiver vivo.
    /// </summary>
    public bool TryGetPlayerEntity(int charId, out Entity entity)
    {
        if (_playersByCharId.TryGetValue(charId, out entity))
        {
            if (World.IsAlive(entity))
            {
                return true;
            }
            _playersByCharId.Remove(charId); // Auto-correção
        }
        entity = default;
        return false;
    }
}