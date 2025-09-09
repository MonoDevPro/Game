using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.Shared.Components;

namespace Simulation.Core.Server.Systems;

/// <summary>
/// Um sistema ECS cuja única responsabilidade é manter um índice de alta performance
/// que mapeia um MapId (int) para a sua Entity correspondente.
/// </summary>
public sealed partial class MapIndexSystem(World world) : BaseSystem<World, float>(world)
{
    // O índice privado que oferece busca O(1)
    private readonly Dictionary<int, Entity> _mapsByMapId = new();

    [Query]
    [All<MapId>]
    [None<MapIndexed>]
    private void AddNewMaps(in Entity entity, ref MapId mapId)
    {
        _mapsByMapId[mapId.Value] = entity;
        World.Add<MapIndexed>(entity); // Adiciona a tag para não processar novamente
    }

    /// <summary>
    /// Permite que outros sistemas obtenham a entidade de um mapa pelo seu ID de forma rápida.
    /// </summary>
    /// <param name="mapId">O ID do mapa a ser encontrado.</param>
    /// <param name="entity">A entidade encontrada.</param>
    /// <returns>True se a entidade foi encontrada e está viva, caso contrário, false.</returns>
    public bool TryGetEntity(int mapId, out Entity entity)
    {
        if (_mapsByMapId.TryGetValue(mapId, out entity))
        {
            // Verificação crucial: garante que não estamos retornando uma entidade "morta"
            // que já foi destruída mas ainda não foi removida do índice.
            if (World.IsAlive(entity))
            {
                return true;
            }
            
            // Auto-correção: remove a entidade morta do índice.
            _mapsByMapId.Remove(mapId);
        }

        entity = default;
        return false;
    }
}