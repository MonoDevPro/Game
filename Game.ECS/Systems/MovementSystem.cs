using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Abstractions;
using Game.Domain.VOs;
using Game.ECS.Components;
using Game.ECS.Extensions;
using Game.ECS.Systems.Common;

namespace Game.ECS.Systems;

public sealed partial class MovementSystem(World world, MapService map) : GameSystem(world)
{
    [Query]
    [All<GridPosition, Velocity>]
    private void MoveEntity(Entity e, ref GridPosition grid, ref Position pos, ref Velocity vel, [Data] float deltaTime)
    {
        // Se não há velocidade, não há o que fazer.
        if (vel.Value.MagnitudeSquared == 0f)
            return;
        
        // 1. Calcula a posição precisa de destino potencial
        var displacement = vel.Value * deltaTime;
        var nextPrecisePos = pos.Value + displacement;
        
        // 2. Converte para a próxima posição do grid
        var nextGridPos = new Coordinate((int)Math.Round(nextPrecisePos.X), (int)Math.Round(nextPrecisePos.Y));

        // 3. Se a posição no grid não mudou, apenas atualizamos a posição precisa e paramos.
        //    Isso permite que pequenos movimentos se acumulem sem verificar colisão desnecessariamente.
        if (grid.Value == nextGridPos)
        {
            pos.Value = nextPrecisePos;
            return;
        }
        
        // 4. VERIFICAÇÃO DE COLISÃO: A posição no grid vai mudar, então validamos o destino.
        //    Verifica se a nova posição está dentro dos limites do mapa e não está bloqueada.
        if (!map.InBounds(nextGridPos) || map.IsBlocked(nextGridPos))
        {
            // Posição inválida. Barramos o movimento.
            // Opcional: zerar a velocidade para parar a entidade completamente ao colidir.
            vel.Value = FCoordinate.Zero; 
            World.MarkNetworkDirty(e, SyncFlags.Velocity); // Marca para sincronizar a colisão e parada
            return; // Não atualiza a posição.
        }
        
        // 5. Movimento VÁLIDO: Atualiza a posição precisa e a posição do grid.
        pos.Value = nextPrecisePos;
        grid.Value = nextGridPos;
        
        Console.WriteLine($"Entity {e} moved to {pos.Value} (Grid: {grid.Value})");
        
        vel.Value = FCoordinate.Zero; // Zera a velocidade após o movimento (movimento por input)
        
        // Marca a entidade como "dirty" para sincronização de rede.
        World.MarkNetworkDirty(e, SyncFlags.Movement);
    }
}