using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Entities.Data;
using Game.ECS.Services;
using Game.ECS.Systems;
using Arch.Core;
using Arch.System;

namespace Game.ECS.Examples;

/// <summary>
/// Exemplo de uso do ECS como SERVIDOR.
/// O servidor executa a simulação completa com todos os sistemas.
/// </summary>
public class ServerGameSimulation : GameSimulation
{
    private MovementSystem _movementSystem = null!;
    private HealthSystem _healthSystem = null!;
    private CombatSystem _combatSystem = null!;
    private AISystem _aiSystem = null!;
    
    private IMapService _mapService = null!;
    
    public ServerGameSimulation() : base()
    {
        // Registra todos os serviços
        _mapService = new MapService();
        
        // Configura os sistemas
        ConfigureSystems(World, Systems);
    }

    /// <summary>
    /// Configura todos os sistemas de servidor.
    /// Ordem importante: Input → Movement → Combat → Sync
    /// </summary>
    public override void ConfigureSystems(World world, Group<float> group)
    {
        // Sistemas de entrada (input não vem do servidor, vem do cliente)
        // Mas o servidor valida e aplica
        
        // Sistemas de movimento
    _movementSystem = new MovementSystem(world, EventSystem);
        group.Add(_movementSystem);
        
        // Sistemas de saúde
    _healthSystem = new HealthSystem(world, EventSystem);
        group.Add(_healthSystem);
        
        // Sistemas de combate
    _combatSystem = new CombatSystem(world, EventSystem);
        group.Add(_combatSystem);
        
        // Sistemas de IA
    _aiSystem = new AISystem(world, EventSystem);
        group.Add(_aiSystem);
        
    }

    public void RegisterNewPlayer(int playerId, int networkId)
    {
        var playerData = new PlayerCharacter(
            PlayerId: playerId,
            NetworkId: networkId,
            Name: $"Player_{playerId}",
            Level: 1,
            ClassId: 0,
            SpawnX: 50, SpawnY: 50, SpawnZ: 0,
            FacingX: 0, FacingY: 1,
            Hp: 100, MaxHp: 100, HpRegen: 1f,
            Mp: 50, MaxMp: 50, MpRegen: 0.5f,
            MovementSpeed: 1f, AttackSpeed: 1f,
            PhysicalAttack: 10, MagicAttack: 5,
            PhysicalDefense: 2, MagicDefense: 1
        );
        
    var player = SpawnRemotePlayer(playerData);
        
    Console.WriteLine($"[SERVER] Jogador {playerId} ({networkId}) entrou no servidor");
    }

    public void RemovePlayer(int networkId)
    {
        // Encontra e remove o jogador
        // Em uma implementação real, manter um dicionário de networkId -> Entity
        // TODO: EventSystem.RaisePlayerLeft(networkId);
    }

    public void SpawnNPC(string name, int npcId, int x, int y)
    {
        var npcData = new NPCCharacter(
            NetworkId: npcId,
            Name: name,
            PositionX: x, PositionY: y, PositionZ: 0,
            Hp: 50, MaxHp: 50, HpRegen: 0.5f,
            PhysicalAttack: 8, MagicAttack: 2,
            PhysicalDefense: 1, MagicDefense: 0
        );
        
        var npc = SpawnNpc(npcData);
        
        // Insere no spatial grid
        var grid = _mapService.GetMapSpatial(0);
        grid.Insert(new Position { X = x, Y = y, Z = 0 }, npc);
    }

    public void ApplyPlayerInput(int networkId, sbyte inputX, sbyte inputY, InputFlags flags)
    {
        // Em uma implementação real, encontrar a entidade pelo networkId
        // player.Set(new PlayerInput { InputX = inputX, InputY = inputY, Flags = flags });
        Console.WriteLine($"[SERVER] Input recebido: {networkId} -> ({inputX}, {inputY}, {flags})");
    }
}
