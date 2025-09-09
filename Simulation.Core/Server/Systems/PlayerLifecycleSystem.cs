using Arch.Bus;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Core.Server.Staging;
using Simulation.Core.Shared.Components;
using Simulation.Core.Shared.Templates;
using Simulation.Core.Shared.Utils.Map;

namespace Simulation.Core.Server.Systems;

// Classe refatorada para usar apenas PlayerData
public sealed class PlayerLifecycleSystem(
    World world,
    MapManagerService map,
    IPlayerStagingArea stagingArea,
    ILogger<PlayerLifecycleSystem> logger)
    : BaseSystem<World, float>(world)
{
    private readonly Dictionary<int, Entity> _playersByCharId = new();
    
    // Reuso de listas para minimizar alocações
    private readonly List<Entity> _entities = new();
    private readonly List<PlayerData> _players = new();
    

    private static readonly ComponentType[] ArchetypeComponents =
    [
        Component<PlayerId>.ComponentType,
        Component<MapId>.ComponentType,
        Component<PlayerData>.ComponentType,
        Component<Position>.ComponentType,
        Component<Direction>.ComponentType,
        Component<MoveStats>.ComponentType,
        Component<AttackStats>.ComponentType,
        Component<Health>.ComponentType,
        
    ];
    private static readonly QueryDescription CharQuery = new QueryDescription(all: ArchetypeComponents);
    
    public void ApplyTo(Entity e, PlayerData data)
    {
        World.Set(e,
            new PlayerId { Value = data.Id },
            new MapId { Value = data.MapId },
            data, // Armazena o DTO inteiro como um componente frio
            new Position { X = data.PosX, Y = data.PosY},
            new Direction { X = data.DirX, Y = data.DirY},
            new AttackStats { CastTime = data.AttackCastTime, Cooldown = data.AttackCooldown, Damage = data.AttackDamage, AttackRange = data.AttackRange },
            new MoveStats { Speed = data.MoveSpeed },
            new Health { Current = data.HealthCurrent, Max = data.HealthMax }
        );
    }
    
    public override void Update(in float dt)
    {
        // Processa jogadores entrando
        while (stagingArea.TryDequeueLogin(out var data) && data != null)
            ProcessJoin(data);
        
        // Processa jogadores saindo
        while (stagingArea.TryDequeueLeave(out var charId))
            ProcessLeave(charId);
    }
    
    public async void ProcessJoin(PlayerData data)
    {
        try
        {
            if (_playersByCharId.ContainsKey(data.Id))
            {
                logger.LogWarning("CharId {CharId} já está no jogo. Ignorando Join.", data.Id);
                return;
            }
            
            var others = _players;
            var mapEntitiesList = _entities;
            
            try
            {
                await map.LoadMapAsync(data.MapId);
            
                var entity = World.Create(ArchetypeComponents);
                ApplyTo(entity, data);
            
                _playersByCharId[data.Id] = entity;
            
                // Obtém outros players no mapa
                var othersEntities = GetEntitiesInMap(data.MapId, mapEntitiesList);
                foreach (var e in othersEntities)
                {
                    if (e.Id == entity.Id) continue;
                
                    others.Add(BuildPlayerData(e));
                }
            
                // TODO: Envia tudo para quem entrou
                // TODO: Notifica os demais do mapa
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao criar jogador para CharId {CharId}", data.Id);
            }
            finally
            {
                others.Clear();
                mapEntitiesList.Clear();
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Erro inesperado ao processar Join para CharId {CharId}", data.Id);
        }
    }
    
    public void ProcessLeave(int charId)
    {
        if (!_playersByCharId.TryGetValue(charId, out var playerEntity))
        {
            logger.LogWarning("CharId {CharId} não encontrado ao processar Leave.", charId);
            return;
        }
        
        try
        {
            // Enfileira o estado final do jogador para ser salvo no banco
            SavePlayerState(playerEntity);
            
            _playersByCharId.Remove(charId);
            
            // Constrói o DTO do jogador que saiu para notificar os outros
            var data = BuildPlayerData(playerEntity);
            
            // TODO: Notifica os outros jogadores no mesmo mapa
            
            if (World.IsAlive(playerEntity))
                World.Destroy(playerEntity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar saída do CharId {CharId}", charId);
        }
    }
    
    private void SavePlayerState(Entity playerEntity)
    {
        var data = BuildPlayerData(playerEntity);
        stagingArea.StageSave(data); // A StagingArea agora recebe PlayerData
    }
    
    private IEnumerable<Entity> GetEntitiesInMap(int mapId, List<Entity>? reuse = null)
    {
        reuse ??= [];

        World.Query(CharQuery, (ref Entity e, ref PlayerId cid, ref MapId mid) =>
        {
            if (mid.Value == mapId)
                reuse.Add(e);
        });

        return reuse;
    }

    public PlayerData BuildPlayerData(Entity e)
    {
        ref var playerId = ref World.Get<PlayerId>(e);
        ref var mapId = ref World.Get<MapId>(e);
        ref var position = ref World.Get<Position>(e);
        ref var direction = ref World.Get<Direction>(e);
        ref var attackStats = ref World.Get<AttackStats>(e);
        ref var moveStats = ref World.Get<MoveStats>(e);
        ref var health = ref World.Get<Health>(e);
        var pData = World.Get<PlayerData>(e); // Class ref
        
        pData.Id = playerId.Value;
        pData.MapId = mapId.Value;
        pData.PosX = position.X;
        pData.PosY = position.Y;
        pData.DirX = direction.X;
        pData.DirY = direction.Y;
        pData.AttackCastTime = attackStats.CastTime;
        pData.AttackCooldown = attackStats.Cooldown;
        pData.AttackDamage = attackStats.Damage;
        pData.AttackRange = attackStats.AttackRange;
        pData.MoveSpeed = moveStats.Speed;
        pData.HealthCurrent = health.Current;
        pData.HealthMax = health.Max;
        return pData;
    }
}