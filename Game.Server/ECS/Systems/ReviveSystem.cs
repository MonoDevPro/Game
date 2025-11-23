using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Logic;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

/// <summary>
/// Sistema responsável por reviver jogadores mortos após um período de tempo.
/// Quando um jogador morre, inicia um timer de revive.
/// Após o tempo expirar, o jogador é revivido com HP/MP parcial na posição de spawn.
/// 
/// Autor: MonoDevPro
/// Data: 2025-01-12
/// </summary>
public sealed partial class ReviveSystem(World world, ILogger<ReviveSystem> logger) 
    : GameSystem(world)
{
    
    /// <summary>
    /// Processa jogadores mortos que estão em processo de revive.
    /// </summary>
    [Query]
    [All<Dead, Revive>]
    private void ProcessReviveTimer(
        in Entity entity,
        ref Revive revive,
        ref CombatState combat,
        ref Health health,
        ref Mana mana,
        ref DirtyFlags dirty,
        [Data] float deltaTime)
    {
        revive.TimeRemaining -= deltaTime;
        
        // ✅ Tempo de revive expirou - reviver jogador
        if (revive.TimeRemaining <= 0f)
        {
            // ✅ Usa a extensão para marcar mudança de posição
            World.SetPosition(entity, revive.SpawnPosition);
        
            combat.InCombat = false;
            combat.LastAttackTime = 0f;
        
            health.Current = (int)(health.Max * SimulationConfig.ReviveHealthPercent);
            mana.Current = (int)(mana.Max * SimulationConfig.ReviveManaPercent);
        
            // ✅ Marca State como dirty para o SpatialSyncSystem detectar
            dirty.MarkDirty(DirtyComponentType.All);
        
            World.Remove<Dead, Revive>(entity);
        }
    }
    
    /// <summary>
    /// Detecta jogadores que acabaram de morrer e inicia o processo de revive.
    /// </summary>
    [Query]
    [All<Dead>]
    [None<Revive>]
    private void InitializeRevive(
        in Entity entity)
    {
        // ✅ Adiciona componente de revive ao jogador morto
        World.Add<Revive>(entity, new Revive
        {
            TimeRemaining = SimulationConfig.DefaultRespawnTime,
            TotalTime = SimulationConfig.DefaultRespawnTime,
            SpawnPosition = new Position { X = SimulationConfig.DefaultSpawnX, Y = SimulationConfig.DefaultSpawnY },
            SpawnFloor = new Floor { Level = SimulationConfig.DefaultSpawnFloor }
        });
    }
}
