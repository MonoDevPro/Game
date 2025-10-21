using Arch.Core;
using Game.ECS.Entities.Data;

namespace Game.ECS.Entities.Factories;

/// <summary>
/// Factory interface para criação de diferentes tipos de entidades no jogo.
/// Centraliza a lógica de inicialização de componentes.
/// </summary>
public interface IEntityFactory
{
    // ✅ Criação
    Entity CreatePlayer(in PlayerCharacter data);
    Entity CreateNPC(in NPCCharacter data);
    Entity CreateProjectile(in ProjectileData data);
    /// <summary>
    /// Cria um item solto no mapa.
    /// </summary>
    Entity CreateDroppedItem(in DroppedItemData data);
    
    // ✅ Destruição
    bool DestroyEntity(Entity entity);
}