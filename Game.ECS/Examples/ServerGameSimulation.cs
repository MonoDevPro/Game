using Arch.Core;
using Arch.System;
using Game.ECS.Services;
using Game.ECS.Systems;

namespace Game.ECS.Examples;

/// <summary>
/// Exemplo de uso do ECS como SERVIDOR.
/// O servidor executa a simulação completa com todos os sistemas.
/// </summary>
public sealed class ServerGameSimulation : GameSimulation
{
    private InputSystem _inputSystem = null!;
    private MovementSystem _movementSystem = null!;
    private HealthSystem _healthSystem = null!;
    private CombatSystem _combatSystem = null!;
    private AISystem _aiSystem = null!;

    public ServerGameSimulation(GameEventSystem? gameEventSystem = null, IMapService? mapService = null) : base(gameEventSystem ??= new GameEventSystem(), mapService ??= new MapService())
    {
        // Configura os sistemas
        ConfigureSystems(World, GameEvents, Systems);
    }

    /// <summary>
    /// Configura todos os sistemas de servidor.
    /// Ordem importante: Input → Movement → Combat → Sync
    /// </summary>
    protected override void ConfigureSystems(World world, GameEventSystem eventSystem, Group<float> systems)
    {
        // Sistemas de entrada (input não vem do servidor, vem do cliente)
        // Mas o servidor valida e aplica
        _inputSystem = new InputSystem(world, eventSystem);
        systems.Add(_inputSystem);
        
        // Sistemas de movimento
        _movementSystem = new MovementSystem(world, MapService, eventSystem);
        systems.Add(_movementSystem);
        
        // Sistemas de saúde
        _healthSystem = new HealthSystem(world, eventSystem);
        systems.Add(_healthSystem);
        
        // Sistemas de combate
        _combatSystem = new CombatSystem(world, eventSystem);
        systems.Add(_combatSystem);
        
        // Sistemas de IA
        _aiSystem = new AISystem(world, MapService, _combatSystem, eventSystem);
        systems.Add(_aiSystem);
    }
}