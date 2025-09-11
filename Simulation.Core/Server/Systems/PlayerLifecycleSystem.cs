using System.Collections.Concurrent;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Core.Server.Staging;
using Simulation.Core.Shared.Components;
using Simulation.Core.Shared.Templates;
using Simulation.Core.Shared.Utils.Map;

namespace Simulation.Core.Server.Systems
{
    public sealed class PlayerLifecycleSystem(
        World world,
        MapManagerService map,
        PlayerIndexSystem playerIndex,
        IPlayerStagingArea stagingArea,
        ILogger<PlayerLifecycleSystem> logger)
        : BaseSystem<World, float>(world)
    {
        // Reuso de listas (apenas no thread principal)
        private readonly List<Entity> _entities = new();
        private readonly List<PlayerData> _players = new();

        // Queue para finalizar joins iniciados em background (apenas modificação do World no thread principal)
        private readonly ConcurrentQueue<PlayerData> _pendingJoinsFromIO = new();

        // Exemplo de composição de archetype (ajuste conforme sua API)
        private static readonly ComponentType[] ArchetypeComponents =
        [
            Component<PlayerId>.ComponentType,
            Component<MapId>.ComponentType,
            Component<PlayerData>.ComponentType, // se for classe, atenção ao aliasing (ver notas abaixo)
            Component<Position>.ComponentType,
            Component<Direction>.ComponentType,
            Component<MoveStats>.ComponentType,
            Component<AttackStats>.ComponentType,
            Component<Health>.ComponentType
        ];

        // Se sua API não aceita QueryDescription(all: ...), construa com WithAll<T>()
        private static readonly QueryDescription CharQuery = new QueryDescription(all: ArchetypeComponents);

        // Exemplo: este Update roda no thread principal do servidor
        public override void Update(in float dt)
        {
            // 1) processa itens pré-finalizados vindos do background (após LoadMapAsync)
            while (_pendingJoinsFromIO.TryDequeue(out var pending))
            {
                // garante ser executado no thread principal
                FinalizeJoinOnMainThread(pending);
            }

            // 2) processa entradas síncronas da stagingArea (se a stagingArea for concorrente, ela já provê fila seguro)
            while (stagingArea.TryDequeueLogin(out var data))
            {
                if (data == null) continue;
                // Inicia o carregamento em background; quando terminar, será enfileirado para finalizar no Update
                _ = ProcessJoinBackgroundAsync(data);
            }

            // 3) processa leaves (stagingArea pode ter fila separada)
            while (stagingArea.TryDequeueLeave(out var charId))
            {
                ProcessLeave(charId);
            }
        }

        // Faz IO / Load do mapa em background; não toca no World aqui.
        private async Task ProcessJoinBackgroundAsync(PlayerData data)
        {
            try
            {
                await map.LoadMapAsync(data.MapId).ConfigureAwait(false);
                // opcional: clone data para evitar alterações externas antes do enqueue
                var dataClone = Clone(playerData: data);
                _pendingJoinsFromIO.Enqueue(dataClone);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao carregar mapa para CharId {CharId}", data.Id);
            }
        }

        // Método que deve rodar somente no thread principal — cria entidade e aplica componentes
        private void FinalizeJoinOnMainThread(PlayerData data)
        {
            try
            {
                if (!playerIndex.TryGetEntity(data.Id, out var existingEntity) || !World.IsAlive(existingEntity))
                {
                    logger.LogWarning("CharId {CharId} já está no jogo. Ignorando Join final.", data.Id);
                    return;
                }

                // Cria a entidade (opte por criar vazia e Set/Add para maior compatibilidade)
                var entity = World.Create(ArchetypeComponents); // ou World.Create(ArchetypeComponents) se sua API suportar
                ApplyTo(entity, data);

                // Obtém outros players no mapa usando lista reutilizável
                var mapEntitiesList = _entities;
                mapEntitiesList.Clear();
                var others = _players;
                others.Clear();

                foreach (var e in GetEntitiesInMap(data.MapId, mapEntitiesList))
                {
                    if (e.Id == entity.Id) continue;
                    others.Add(BuildPlayerData(e)); // BuildPlayerData retorna cópia
                }

                // TODO: Enviar dados para o jogador que entrou (use objeto copiado)
                // TODO: Notificar demais jogadores
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao finalizar join para CharId {CharId}", data.Id);
            }
        }

        // Processo de saída
        public void ProcessLeave(int charId)
        {
            if (!playerIndex.TryGetEntity(charId, out var playerEntity) || !World.IsAlive(playerEntity))
            {
                logger.LogWarning("CharId {CharId} não encontrado ao processar Leave.", charId);
                return;
            }

            try
            {
                // Enfileira estado final para salvar (não bloqueante)
                SavePlayerState(playerEntity);

                var data = BuildPlayerData(playerEntity); // copy-safe
                // TODO: Notificar os outros jogadores no mesmo mapa

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
            stagingArea.StageSave(data); // opera com cópia DTO
        }

        private IEnumerable<Entity> GetEntitiesInMap(int mapId, List<Entity>? reuse = null)
        {
            reuse ??= new List<Entity>();

            // Ajuste da assinatura do callback conforme sua versão do Arch
            World.Query(CharQuery, (Entity e, ref PlayerId pid, ref MapId mid) =>
            {
                if (mid.Value == mapId)
                    reuse.Add(e);
            });

            return reuse;
        }

        // Constrói e RETORNA UMA CÓPIA de PlayerData para evitar aliasing com a instância armazenada como componente
        public PlayerData BuildPlayerData(Entity e)
        {
            // Se PlayerData for armazenado como componente class no World, evite retornar referência direta:
            // copie os campos para um novo PlayerData DTO
            ref var id = ref World.Get<PlayerId>(e);
            ref var mapId = ref World.Get<MapId>(e);
            ref var pos = ref World.Get<Position>(e);
            ref var dir = ref World.Get<Direction>(e);
            ref var attack = ref World.Get<AttackStats>(e);
            ref var move = ref World.Get<MoveStats>(e);
            ref var health = ref World.Get<Health>(e);
            // mantenha outros campos estáticos se necessário (Name, Gender, etc.)
            ref var name = ref World.Get<PlayerData>(e).Name;
            
            var p = new PlayerData
            {
                Id = id.Value,
                MapId = mapId.Value,
                PosX = pos.X,
                PosY = pos.Y,
                DirX = dir.X,
                DirY = dir.Y,
                AttackCastTime = attack.CastTime,
                AttackCooldown = attack.Cooldown,
                AttackDamage = attack.Damage,
                AttackRange = attack.AttackRange,
                MoveSpeed = move.Speed,
                HealthCurrent = health.Current,
                HealthMax = health.Max,
                // mantenha outros campos estáticos se necessário (Name, Gender, etc.)
                Name = name
            };

            return p;
        }

        // Aplica componentes na entidade criada — se preferir, converta PlayerData em struct para evitar aliasing
        public void ApplyTo(Entity e, PlayerData data)
        {
            // Exemplo seguro: sempre crie novos structs / objetos quando armazenar no World
            World.Set(e,
                new PlayerId { Value = data.Id },
                new MapId { Value = data.MapId },
                // Se você insiste em armazenar PlayerData (classe), armazene uma CÓPIA nova:
                new PlayerData
                {
                    Id = data.Id,
                    Name = data.Name,
                    Gender = data.Gender,
                    Vocation = data.Vocation,
                    HealthMax = data.HealthMax,
                    AttackDamage = data.AttackDamage,
                    AttackRange = data.AttackRange,
                    AttackCastTime = data.AttackCastTime,
                    AttackCooldown = data.AttackCooldown,
                    MoveSpeed = data.MoveSpeed,
                    MapId = data.MapId,
                    PosX = data.PosX,
                    PosY = data.PosY,
                    DirX = data.DirX,
                    DirY = data.DirY,
                    HealthCurrent = data.HealthCurrent
                },
                new Position { X = data.PosX, Y = data.PosY },
                new Direction { X = data.DirX, Y = data.DirY },
                new AttackStats { CastTime = data.AttackCastTime, Cooldown = data.AttackCooldown, Damage = data.AttackDamage, AttackRange = data.AttackRange },
                new MoveStats { Speed = data.MoveSpeed },
                new Health { Current = data.HealthCurrent, Max = data.HealthMax }
            );
        }

        // Clona PlayerData (utilitário)
        private static PlayerData Clone(PlayerData playerData)
        {
            if (playerData == null) return new PlayerData();
            return new PlayerData
            {
                Id = playerData.Id,
                Name = playerData.Name,
                Gender = playerData.Gender,
                Vocation = playerData.Vocation,
                HealthMax = playerData.HealthMax,
                AttackDamage = playerData.AttackDamage,
                AttackRange = playerData.AttackRange,
                AttackCastTime = playerData.AttackCastTime,
                AttackCooldown = playerData.AttackCooldown,
                MoveSpeed = playerData.MoveSpeed,
                MapId = playerData.MapId,
                PosX = playerData.PosX,
                PosY = playerData.PosY,
                DirX = playerData.DirX,
                DirY = playerData.DirY,
                HealthCurrent = playerData.HealthCurrent
            };
        }
    }
}