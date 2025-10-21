using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Entities.Data;

namespace Game.ECS.Entities;

/// <summary>
/// Factory interface para criação de diferentes tipos de entidades no jogo.
/// Centraliza a lógica de inicialização de componentes.
/// </summary>
public interface IEntityFactory
{
    /// <summary>
    /// Cria um jogador genérico.
    /// </summary>
    Entity CreatePlayer(in PlayerCharacter data);

    /// <summary>
    /// Cria um jogador remoto (outro jogador conectado).
    /// </summary>
    Entity CreateRemotePlayer(in PlayerCharacter data);

    /// <summary>
    /// Cria um jogador local (controlado pela sessão atual).
    /// </summary>
    Entity CreateLocalPlayer(in PlayerCharacter data);

    /// <summary>
    /// Cria um NPC com IA.
    /// </summary>
    Entity CreateNPC(in NPCCharacter data);

    /// <summary>
    /// Cria um projétil.
    /// </summary>
    Entity CreateProjectile(in ProjectileData data);

    /// <summary>
    /// Cria um item solto no mapa.
    /// </summary>
    Entity CreateDroppedItem(in DroppedItemData data);
}