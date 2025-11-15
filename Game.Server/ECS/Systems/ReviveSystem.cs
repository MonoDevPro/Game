using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS;
using Game.ECS.Components;
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
    // ========== CONSTANTS ==========

    private const float DefaultReviveTime = SimulationConfig.DefaultRespawnTime;
    private const float ReviveHealthPercent = SimulationConfig.ReviveHealthPercent;
    private const float ReviveManaPercent = SimulationConfig.ReviveManaPercent;
    
    // ========== CONSTRUCTOR ==========

    // ========== QUERIES ==========
    
    /// <summary>
    /// Processa jogadores mortos que estão em processo de revive.
    /// </summary>
    [Query]
    [All<PlayerControlled>]
    [All<Dead, Revive>]
    private void ProcessReviveTimer(
        in Entity entity,
        ref Revive revive,
        [Data] float deltaTime)
    {
        revive.TimeRemaining -= deltaTime;
        
        // ✅ Tempo de revive expirou - reviver jogador
        if (revive.TimeRemaining <= 0f)
            RevivePlayer(entity);
    }
    
    /// <summary>
    /// Detecta jogadores que acabaram de morrer e inicia o processo de revive.
    /// </summary>
    [Query]
    [All<Dead, PlayerControlled, Position, Health, Mana>]
    [None<Revive>]
    private void InitializeRevive(
        in Entity entity,
        ref Position position)
    {
        // ✅ Adiciona componente de revive ao jogador morto
        var revive = new Revive
        {
            TimeRemaining = DefaultReviveTime,
            TotalTime = DefaultReviveTime,
            SpawnPosition = GetSpawnPosition(position) // Pode ser customizado
        };
        
        World.Add(entity, revive);
        if (World.TryGet(entity, out NetworkId netId))
        {
            logger.LogInformation(
                "Player {NetworkId} died - Revive will occur in {Time}s",
                netId.Value,
                DefaultReviveTime);
        }
    }
    
    // ========== PRIVATE METHODS ==========
    
    /// <summary>
    /// Revive um jogador morto.
    /// </summary>
    private void RevivePlayer(in Entity entity)
    {
        // ✅ 1. Remove componente Dead
        World.Remove<Dead>(entity);
        
        // ✅ 3. Reseta estado de combate
        if (World.TryGet(entity, out CombatState combat))
        {
            combat.InCombat = false;
            combat.LastAttackTime = 0f;
            World.Set(entity, combat);
        }
        
        // ✅ 4. Teleporta para posição de spawn
        if (World.TryGet(entity, out Revive revive))
        {
            if (World.TryGet(entity, out Position position))
            {
                position = revive.SpawnPosition;
                World.Set(entity, position);
            }
            // ✅ 5. Remove componente Revive
            World.Remove<Revive>(entity);
        }
        
        // ✅ 6. Marca tudo como dirty para sincronizar com cliente
        if (World.TryGet(entity, out DirtyFlags dirty))
        {
            dirty.MarkDirty(DirtyComponentType.All);
            World.Set(entity, dirty);
        }
        
        if (World.TryGet(entity, out NetworkId netId))
        {
            logger.LogInformation(
                "Player {NetworkId} revived at position ({X}, {Y}, {Z})",
                netId.Value,
                revive.SpawnPosition.X,
                revive.SpawnPosition.Y,
                revive.SpawnPosition.Z);
        }
    }
    
    /// <summary>
    /// Obtém a posição de spawn para revive.
    /// Por padrão, usa posição inicial do mapa.
    /// Pode ser customizado para usar checkpoints, cidades, etc.
    /// </summary>
    private static Position GetSpawnPosition(in Position currentPosition)
    {
        // ✅ Por enquanto, usa spawn padrão do servidor
        // TODO: Implementar sistema de checkpoints/cidades
        return new Position
        {
            X = SimulationConfig.DefaultSpawnX,
            Y = SimulationConfig.DefaultSpawnY,
            Z = SimulationConfig.DefaultSpawnZ
        };
    }
    
    // ========== PUBLIC METHODS ==========
    
    /// <summary>
    /// Revive instantaneamente um jogador (sem timer).
    /// Útil para GMs, items de revive, etc.
    /// </summary>
    public void InstantRevive(in Entity entity, Position? spawnPosition = null)
    {
        if (!World.IsAlive(entity) || !World.Has<Dead>(entity))
            return;
            
        // ✅ Define posição de spawn customizada
        if (spawnPosition.HasValue)
        {
            var revive = new Revive
            {
                TimeRemaining = 0f,
                TotalTime = 0f,
                SpawnPosition = spawnPosition.Value
            };
            
            if (World.Has<Revive>(entity))
                World.Set(entity, revive);
            else
                World.Add(entity, revive);
        }
        
        // ✅ Revive imediatamente
        RevivePlayer(entity);
        
        if (World.TryGet(entity, out NetworkId netId))
        {
            logger.LogInformation(
                "Player {NetworkId} instantly revived",
                netId.Value);
        }
    }
}
