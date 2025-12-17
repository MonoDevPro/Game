using Arch.Core;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;

namespace Game.ECS.Services.Pathfinding.Systems;

/// <summary>
/// Sistema ECS que integra pathfinding com Arch ECS
/// </summary>
public sealed partial class EcsPathfindingSystem(
    World world, 
    PathfindingSystem pathfinder) : GameSystem(world)
{
    // Buffer thread-local para evitar alocação
    [ThreadStatic]
    private static int[]? _pathBuffer;
    
    [All<PathfindingRequest, Position>]
    private void ProcessPath(
        in Entity entity, 
        ref PathfindingRequest request, 
        ref Position pos)
    {
        _pathBuffer ??= new int[256];

        if (request.Status != PathfindingStatus.Pending)
            return;

        request.Status = PathfindingStatus.InProgress;

        var result = pathfinder.FindPath(ref request, _pathBuffer.AsSpan());

        if (result.IsValid)
        {
            // Adiciona componente de caminho à entidade
            // Usa buffer pooled ou copia para componente
            ProcessCompletedPath(entity, _pathBuffer, result.PathLength);
        }
    }

    private void ProcessCompletedPath(Entity entity, int[] pathBuffer, int length)
    {
        // Implementação depende de como você quer armazenar o caminho
        // Opção 1: Adicionar componente com waypoints
        // Opção 2: Iniciar sistema de movimento diretamente
        // Opção 3: Publicar evento para outros sistemas
    }
}