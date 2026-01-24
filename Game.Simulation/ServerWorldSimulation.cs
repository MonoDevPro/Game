using Arch.Core;
using Arch.System;
using Game.Contracts;
using Game.Infrastructure.ArchECS;
using Game.Infrastructure.ArchECS.Commons.Components;
using Game.Infrastructure.ArchECS.Services.Map;
using Game.Infrastructure.ArchECS.Services.Navigation;
using Game.Infrastructure.ArchECS.Services.Navigation.Components;
using Game.Infrastructure.ArchECS.Services.Navigation.Core;
using Microsoft.Extensions.Logging;

namespace Game.Simulation;

/// <summary>
/// Implementação concreta da simulação do mundo para o servidor.
/// Herda de WorldSimulation (ECS) e gerencia entidades de jogadores.
/// Integra com o módulo de navegação para pathfinding e movimento.
/// </summary>
public sealed class ServerWorldSimulation : WorldSimulation, IWorldSimulation
{
    private readonly ILogger? _logger;
    private readonly Dictionary<int, Entity> _playerEntities = new();
    private readonly WorldMap? _worldMap;
    private readonly NavigationModule? _navigation;
    private long _currentTick;
    
    // Query para buscar todos os jogadores (entidades com CharacterId e Position)
    private static readonly QueryDescription PlayerQuery = new QueryDescription()
        .WithAll<CharacterId, AccountId, Position>();

    /// <summary>
    /// Cria uma simulação sem mapa (movimento simples por delta).
    /// </summary>
    public ServerWorldSimulation(ILogger? logger = null) : base(null)
    {
        _logger = logger;
        ConfigureSystems(World, Systems);
        Systems.Initialize();
    }

    /// <summary>
    /// Cria uma simulação com mapa e navegação completa.
    /// </summary>
    /// <param name="worldMap">Mapa do mundo para colisão e pathfinding.</param>
    /// <param name="navigationConfig">Configuração do sistema de navegação (opcional).</param>
    /// <param name="logger">Logger (opcional).</param>
    public ServerWorldSimulation(
        WorldMap worldMap,
        NavigationConfig? navigationConfig = null,
        ILogger? logger = null) : base(null)
    {
        _logger = logger;
        _worldMap = worldMap ?? throw new ArgumentNullException(nameof(worldMap));
        _navigation = new NavigationModule(World, worldMap, navigationConfig);
        
        ConfigureSystems(World, Systems);
        Systems.Initialize();
        
        _logger?.LogInformation(
            "ServerWorldSimulation inicializada com mapa {MapId} ({Width}x{Height})", 
            worldMap.Id, worldMap.Width, worldMap.Height);
    }

    /// <summary>
    /// Indica se a simulação tem suporte a navegação.
    /// </summary>
    public bool HasNavigation => _navigation is not null;

    /// <summary>
    /// Mapa do mundo (pode ser null se criado sem mapa).
    /// </summary>
    public WorldMap? Map => _worldMap;

    /// <summary>
    /// Tick atual da simulação.
    /// </summary>
    public long CurrentTick => _currentTick;

    protected override void ConfigureSystems(World world, Group<float> systems)
    {
        // Sistemas de navegação são gerenciados pelo NavigationModule
        // Outros sistemas podem ser adicionados aqui conforme necessidade
        _logger?.LogInformation("ServerWorldSimulation: Sistemas configurados");
    }

    /// <summary>
    /// Atualiza a simulação. Processa tick de navegação se disponível.
    /// </summary>
    public new void Update(in long deltaTime)
    {
        base.Update(in deltaTime);
        
        // Incrementa tick e atualiza navegação
        _currentTick++;
        _navigation?.Tick(_currentTick);
    }

    /// <summary>
    /// Adiciona ou atualiza um jogador no mundo ECS.
    /// Se navegação estiver habilitada, registra o jogador no mapa.
    /// </summary>
    /// <param name="characterId">ID do personagem.</param>
    /// <param name="name">Nome do jogador.</param>
    /// <param name="x">Posição X inicial.</param>
    /// <param name="y">Posição Y inicial.</param>
    /// <param name="floor">Andar (padrão 0).</param>
    /// <returns>A entidade criada ou atualizada.</returns>
    public Entity UpsertPlayer(int characterId, string name, int x, int y, int floor = 0)
    {
        if (_playerEntities.TryGetValue(characterId, out var existingEntity))
        {
            // Atualiza posição da entidade existente
            MovePlayerDirectly(existingEntity, x, y, floor);
            return existingEntity;
        }

        // Cria nova entidade com componentes básicos
        var entity = World.Create(
            new CharacterId { Value = characterId },
            new AccountId { Value = 0 },
            new Position { X = x, Y = y },
            new PlayerName { Name = name }
        );

        // Se tem navegação, adiciona componentes de navegação
        if (_navigation is not null)
        {
            _navigation.AddNavigationComponents(entity, new Position { X = x, Y = y }, floor);
        }
        else
        {
            // Adiciona componentes básicos de localização
            World.Add(entity, new FloorId { Value = floor });
        }

        _playerEntities[characterId] = entity;
        _logger?.LogDebug("Jogador criado: {CharacterId} ({Name}) em ({X}, {Y}, {Floor})", 
            characterId, name, x, y, floor);
        
        return entity;
    }

    /// <summary>
    /// Sobrecarga para compatibilidade com interface.
    /// </summary>
    public Entity UpsertPlayer(int characterId, string name, int x, int y)
        => UpsertPlayer(characterId, name, x, y, 0);

    /// <summary>
    /// Solicita movimento de um jogador para uma posição usando pathfinding.
    /// Requer que a simulação tenha sido criada com um mapa.
    /// </summary>
    /// <param name="characterId">ID do personagem.</param>
    /// <param name="targetX">Posição X de destino.</param>
    /// <param name="targetY">Posição Y de destino.</param>
    /// <param name="targetFloor">Andar de destino.</param>
    /// <param name="flags">Flags de pathfinding.</param>
    /// <returns>True se a requisição foi aceita.</returns>
    public bool RequestPlayerMove(int characterId, int targetX, int targetY, int targetFloor = 0, 
        PathRequestFlags flags = PathRequestFlags.None)
    {
        if (_navigation is null)
        {
            _logger?.LogWarning("Tentativa de movimento com pathfinding, mas navegação não está habilitada");
            return false;
        }

        if (!_playerEntities.TryGetValue(characterId, out var entity))
        {
            _logger?.LogWarning("Jogador {CharacterId} não encontrado para movimento", characterId);
            return false;
        }

        _navigation.RequestMove(entity, targetX, targetY, targetFloor, flags);
        _logger?.LogTrace("Movimento solicitado para {CharacterId}: ({X}, {Y}, {Floor})", 
            characterId, targetX, targetY, targetFloor);
        
        return true;
    }

    /// <summary>
    /// Para o movimento de um jogador.
    /// </summary>
    /// <param name="characterId">ID do personagem.</param>
    /// <returns>True se o movimento foi parado.</returns>
    public bool StopPlayerMove(int characterId)
    {
        if (_navigation is null)
            return false;

        if (!_playerEntities.TryGetValue(characterId, out var entity))
            return false;

        _navigation.StopMovement(entity);
        return true;
    }

    /// <summary>
    /// Move um jogador diretamente por delta (sem pathfinding).
    /// Útil para movimentos simples ou quando navegação não está habilitada.
    /// </summary>
    /// <param name="characterId">ID do personagem.</param>
    /// <param name="dx">Delta X.</param>
    /// <param name="dy">Delta Y.</param>
    /// <returns>True se o movimento foi aplicado.</returns>
    public bool MovePlayerByDelta(int characterId, int dx, int dy)
    {
        if (!_playerEntities.TryGetValue(characterId, out var entity))
            return false;

        ref var pos = ref World.Get<Position>(entity);
        int newX = pos.X + dx;
        int newY = pos.Y + dy;

        // Se tem mapa, verifica colisão
        if (_worldMap is not null)
        {
            var floor = World.Has<FloorId>(entity) ? World.Get<FloorId>(entity).Value : 0;
            
            if (_worldMap.IsBlocked(newX, newY, floor))
            {
                _logger?.LogTrace("Movimento bloqueado para {CharacterId}: ({X}, {Y}) está bloqueado", 
                    characterId, newX, newY);
                return false;
            }

            // Atualiza ocupação no mapa
            if (!_worldMap.TryMoveEntity(pos, floor, new Position { X = newX, Y = newY }, floor, entity))
            {
                _logger?.LogTrace("Movimento bloqueado para {CharacterId}: célula ocupada", characterId);
                return false;
            }
        }

        pos.X = newX;
        pos.Y = newY;
        return true;
    }

    /// <summary>
    /// Move um jogador diretamente para uma posição específica (teleporte).
    /// </summary>
    private void MovePlayerDirectly(Entity entity, int x, int y, int floor)
    {
        ref var pos = ref World.Get<Position>(entity);
        
        if (_worldMap is not null && World.Has<FloorId>(entity))
        {
            var currentFloor = World.Get<FloorId>(entity).Value;
            _worldMap.RemoveEntity(pos, currentFloor, entity);
            _worldMap.AddEntity(new Position { X = x, Y = y }, floor, entity);
        }

        pos.X = x;
        pos.Y = y;

        if (World.Has<FloorId>(entity))
        {
            ref var floorId = ref World.Get<FloorId>(entity);
            floorId.Value = floor;
        }
    }

    /// <summary>
    /// Obtém o estado de navegação de um jogador.
    /// </summary>
    /// <param name="characterId">ID do personagem.</param>
    /// <param name="status">Status de pathfinding.</param>
    /// <param name="isMoving">Se está em movimento.</param>
    /// <returns>True se conseguiu obter o estado.</returns>
    public bool TryGetPlayerNavigationState(int characterId, out PathStatus status, out bool isMoving)
    {
        status = PathStatus.None;
        isMoving = false;

        if (!_playerEntities.TryGetValue(characterId, out var entity))
            return false;

        if (!World.Has<NavPathState>(entity))
            return false;

        status = World.Get<NavPathState>(entity).Status;
        isMoving = World.Has<NavIsMoving>(entity);
        return true;
    }

    /// <summary>
    /// Remove um jogador do mundo.
    /// </summary>
    /// <param name="characterId">ID do personagem a remover.</param>
    /// <returns>True se o jogador foi removido com sucesso.</returns>
    public bool RemovePlayer(int characterId)
    {
        if (!_playerEntities.TryGetValue(characterId, out var entity))
            return false;

        // Remove do mapa se tiver navegação
        if (_navigation is not null)
        {
            _navigation.RemoveNavigationComponents(entity);
        }
        
        if (_worldMap is not null && World.Has<Position>(entity) && World.Has<FloorId>(entity))
        {
            var pos = World.Get<Position>(entity);
            var floor = World.Get<FloorId>(entity).Value;
            _worldMap.RemoveEntity(pos, floor, entity);
        }

        World.Destroy(entity);
        _playerEntities.Remove(characterId);
        _logger?.LogDebug("Jogador removido: {CharacterId}", characterId);
        
        return true;
    }

    /// <summary>
    /// Verifica se um jogador existe no mundo.
    /// </summary>
    public bool HasPlayer(int characterId) => _playerEntities.ContainsKey(characterId);

    /// <summary>
    /// Obtém a entidade de um jogador pelo ID.
    /// </summary>
    public bool TryGetPlayer(int characterId, out Entity entity) 
        => _playerEntities.TryGetValue(characterId, out entity);

    /// <summary>
    /// Constrói um snapshot do estado atual do mundo para sincronização com clientes.
    /// Inclui posição, direção, estado de movimento e dados de interpolação.
    /// </summary>
    public WorldSnapshot BuildSnapshot()
    {
        var players = new List<PlayerState>(_playerEntities.Count);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        foreach (var (characterId, entity) in _playerEntities)
        {
            if (!World.IsAlive(entity))
                continue;
                
            var pos = World.Get<Position>(entity);
            var name = GetPlayerName(characterId);
            var floor = World.Has<FloorId>(entity) ? World.Get<FloorId>(entity).Value : 0;
            
            // Dados de movimento e interpolação
            int dirX = 0, dirY = 0;
            int targetX = pos.X, targetY = pos.Y;
            bool isMoving = false;
            float moveProgress = 0f;
            
            if (World.Has<NavMovementState>(entity))
            {
                ref readonly var movement = ref World.Get<NavMovementState>(entity);
                if (movement.IsMoving)
                {
                    isMoving = true;
                    dirX = movement.MovementDirection.X;
                    dirY = movement.MovementDirection.Y;
                    targetX = movement.TargetCell.X;
                    targetY = movement.TargetCell.Y;
                    
                    // Calcula progresso do movimento (0.0 a 1.0)
                    if (movement.EndTick > movement.StartTick)
                    {
                        var totalDuration = movement.EndTick - movement.StartTick;
                        var elapsed = _currentTick - movement.StartTick;
                        moveProgress = Math.Clamp((float)elapsed / totalDuration, 0f, 1f);
                    }
                }
            }
            
            // Fallback: usa Direction component se existir
            if (!isMoving && World.Has<Direction>(entity))
            {
                var dir = World.Get<Direction>(entity);
                dirX = dir.X;
                dirY = dir.Y;
            }
            
            players.Add(new PlayerState(
                characterId, 
                name, 
                pos.X, 
                pos.Y, 
                floor, 
                dirX, 
                dirY, 
                isMoving,
                targetX,
                targetY,
                moveProgress));
        }

        return new WorldSnapshot(_currentTick, timestamp, players);
    }

    private string GetPlayerName(int characterId)
    {
        if (!_playerEntities.TryGetValue(characterId, out var entity))
            return "Unknown";

        if (!World.Has<PlayerName>(entity))
            return "Unknown";

        return World.Get<PlayerName>(entity).Name;
    }

    /// <summary>
    /// Wrapper para implementar a interface IWorldSimulation.
    /// </summary>
    void IWorldSimulation.Update(long deltaTimeMs)
    {
        Update(in deltaTimeMs);
    }

    public override void Dispose()
    {
        _navigation?.Dispose();
        _playerEntities.Clear();
        base.Dispose();
    }
}

/// <summary>
/// Componente para armazenar o nome do jogador.
/// </summary>
public struct PlayerName
{
    public string Name;
}
