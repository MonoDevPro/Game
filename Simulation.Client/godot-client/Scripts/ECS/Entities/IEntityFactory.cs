using Arch.Core;
using Godot;
using GodotClient.Simulation;

namespace GodotClient.ECS.Entities;

/// <summary>
/// Factory para criar entidades de jogador no ECS
/// Encapsula a complexidade de adicionar múltiplos componentes
/// </summary>
public interface IEntityFactory
{
    Entity CreatePlayer(int networkId, PlayerData data);
    Entity CreateRemotePlayer(int networkId, PlayerData snapshot, Node2D visual);
    Entity CreateLocalPlayer(int networkId, PlayerData snapshot, Node2D visual);
}