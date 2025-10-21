using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Entities.Data;
using Game.ECS.Systems;
using Arch.Core;
using Arch.System;

namespace Game.ECS.Examples;

/// <summary>
/// Exemplo de uso do ECS como CLIENTE.
/// O cliente executa uma simulação local parcial: apenas movimento local, renderização e input.
/// Estado autorizado vem do servidor.
/// </summary>
public class ClientGameSimulation : GameSimulation
{
    private MovementSystem _movementSystem = null!;
    private InputSystem _inputSystem = null!;
    
    private Entity _localPlayer;
    private int _localNetworkId;
    
    public ClientGameSimulation() : base()
    {
        ConfigureSystems(World, Systems);
    }

    /// <summary>
    /// Configura sistemas apenas para o cliente.
    /// Ordem: Input → Movement (previsão local) → Sync (recebe correções do servidor)
    /// </summary>
    public override void ConfigureSystems(World world, Group<float> group)
    {
        // Sistemas de entrada do jogador
    _inputSystem = new InputSystem(world, EventSystem);
        group.Add(_inputSystem);
        
        // Sistemas de movimento (previsão local)
    _movementSystem = new MovementSystem(world, EventSystem);
        group.Add(_movementSystem);
    }

    public void SpawnLocalPlayer(int playerId, int networkId)
    {
        _localNetworkId = networkId;
        
        var playerData = new PlayerCharacter(
            PlayerId: playerId,
            NetworkId: networkId,
            Name: "You",
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
        
        _localPlayer = SpawnLocalPlayer(playerData);
        
        Console.WriteLine($"[CLIENT] Jogador local criado: {playerId} ({networkId})");
    }

    public void HandlePlayerInput(sbyte inputX, sbyte inputY, InputFlags flags)
    {
        if (World.IsAlive(_localPlayer))
        {
            _inputSystem.ApplyPlayerInput(_localPlayer, inputX, inputY, flags);
            Console.WriteLine($"[CLIENT] Input aplicado localmente: ({inputX}, {inputY}, {flags})");
        }
    }

    public void SpawnRemotePlayer(int networkId, int x, int y)
    {
        var playerData = new PlayerCharacter(
            PlayerId: networkId,
            NetworkId: networkId,
            Name: $"Player_{networkId}",
            Level: 1,
            ClassId: 0,
            SpawnX: x, SpawnY: y, SpawnZ: 0,
            FacingX: 0, FacingY: 1,
            Hp: 100, MaxHp: 100, HpRegen: 1f,
            Mp: 50, MaxMp: 50, MpRegen: 0.5f,
            MovementSpeed: 1f, AttackSpeed: 1f,
            PhysicalAttack: 10, MagicAttack: 5,
            PhysicalDefense: 2, MagicDefense: 1
        );
        
        SpawnRemotePlayer(playerData);
        Console.WriteLine($"[CLIENT] Jogador remoto apareceu: {networkId} em ({x}, {y})");
    }

    public void DespawnRemotePlayer(int networkId)
    {
        // Em uma implementação real, encontrar e remover a entidade
        Console.WriteLine($"[CLIENT] Jogador remoto desapareceu: {networkId}");
    }
}
